using System;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod.Projectiles.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix.Calamity.Melee
{
    public class DevilsDevastationHoldoutSync : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private bool _lastHolding;
        private bool _lastDoSwing;
        private bool _lastPostSwing;
        private bool _lastFinalFlip;
        private bool _lastWillDie;
        private bool _lastLaunch;

        private int _lastUseAnim = int.MinValue;
        private int _lastSwing = int.MinValue;
        private int _lastCooldown = int.MinValue;

        private byte _pendingSoundEvent;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.ModProjectile is DevilsDevastationHoldout;
        
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            if (projectile.ModProjectile is not DevilsDevastationHoldout)
                return;
            
            if (projectile.numHits == 0)
                _pendingSoundEvent = 3;
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            if (projectile.ModProjectile is not DevilsDevastationHoldout p)
                return;

            if (p.lastHitTarget != null && p.lastHitTarget.active && p.lastHitTarget.life > 0)
            {
                var fields = new List<SyncField>(2)
                {
                    SyncField.U(DevilsDevField.Kind, 0),
                    SyncField.U(DevilsDevField.SoundEvent, 4)
                };

                CalamitySyncFix.SendVars("Calamity:DevilsDevastation", projectile, fields);
            }
        }

        public override void AI(Projectile projectile)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            if (projectile.ModProjectile is not DevilsDevastationHoldout p)
                return;

            bool holding = p.holding;
            bool doSwing = p.doSwing;
            bool postSwing = p.postSwing;
            bool finalFlip = p.finalFlip;
            bool willDie = p.willDie;
            bool launch = p.hasLaunchedBlades;

            int useAnim = p.useAnim;
            int swing = p.swingCount;
            int cooldown = p.postSwingCooldown;

            float animationProgress = p.Animation % p.useAnim;
            float time = animationProgress - (useAnim / 3f);
            float timeMax = useAnim - (useAnim / 3f);

            if (!p.willDie && time >= (int)(timeMax * 0.4f) && p.playSwingSound)
                _pendingSoundEvent = 1;

            if (!_lastWillDie && p.willDie && p.lastHitTarget != null)
                _pendingSoundEvent = 2;

            bool changed =
                holding != _lastHolding ||
                doSwing != _lastDoSwing ||
                postSwing != _lastPostSwing ||
                finalFlip != _lastFinalFlip ||
                willDie != _lastWillDie ||
                launch != _lastLaunch ||
                useAnim != _lastUseAnim ||
                swing != _lastSwing ||
                cooldown != _lastCooldown ||
                _pendingSoundEvent != 0;

            if (!changed)
                return;

            var fields = new List<SyncField>(11)
            {
                SyncField.U(DevilsDevField.Kind, 0),

                SyncField.U(DevilsDevField.Holding, holding ? (byte)1 : (byte)0),
                SyncField.U(DevilsDevField.DoSwing, doSwing ? (byte)1 : (byte)0),
                SyncField.U(DevilsDevField.PostSwing, postSwing ? (byte)1 : (byte)0),

                SyncField.I(DevilsDevField.UseAnim, useAnim),
                SyncField.I(DevilsDevField.SwingCount, swing),

                SyncField.U(DevilsDevField.FinalFlip, finalFlip ? (byte)1 : (byte)0),
                SyncField.I(DevilsDevField.PostSwingCooldown, cooldown),

                SyncField.U(DevilsDevField.WillDie, willDie ? (byte)1 : (byte)0),
                SyncField.U(DevilsDevField.HasLaunchedBlades, launch ? (byte)1 : (byte)0),
            };

            if (_pendingSoundEvent != 0)
                fields.Add(SyncField.U(DevilsDevField.SoundEvent, _pendingSoundEvent));

            CalamitySyncFix.SendVars("Calamity:DevilsDevastation", projectile, fields);

            _lastHolding = holding;
            _lastDoSwing = doSwing;
            _lastPostSwing = postSwing;
            _lastFinalFlip = finalFlip;
            _lastWillDie = willDie;
            _lastLaunch = launch;

            _lastUseAnim = useAnim;
            _lastSwing = swing;
            _lastCooldown = cooldown;

            _pendingSoundEvent = 0;
        }
    }
}