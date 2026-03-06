using System.Collections.Generic;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix.Calamity.Melee
{
    public static class ForbiddenOathField
    {
        public const byte Kind = 1;

        // player state
        public const byte DemonSwordKillMode = 2;
        public const byte KillModeCooldown = 3;

        // projectile ai
        public const byte Ai0 = 10;
        public const byte Ai1 = 11;

        // holdout internals
        public const byte AimVelX = 12;
        public const byte AimVelY = 13;

        public const byte Holding = 14;
        public const byte DoSwing = 15;
        public const byte PostSwing = 16;

        public const byte UseAnim = 17;
        public const byte SwingCount = 18;

        public const byte FinalFlip = 19;
        public const byte PlaySwingSound = 20;
        public const byte PostSwingCooldown = 21;

        public const byte WillDie = 22;
        public const byte HasLaunchedBlades = 23;

        // 0 none, 1 swing whoosh, 2 strong impact
        public const byte SoundEvent = 24;
    }
    
    public class ForbiddenOathbladeHoldoutSync : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private float _lastAi0 = float.MinValue;
        private float _lastAi1 = float.MinValue;

        private Vector2 _lastAimVel = new(float.MinValue, float.MinValue);

        private bool _lastHolding;
        private bool _lastDoSwing;
        private bool _lastPostSwing;
        private bool _lastFinalFlip;
        private bool _lastPlaySwingSound;
        private bool _lastWillDie;
        private bool _lastLaunched;

        private int _lastUseAnim = int.MinValue;
        private int _lastSwingCount = int.MinValue;
        private int _lastCooldown = int.MinValue;

        private byte _pendingSoundEvent;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.ModProjectile is ForbiddenOathbladeHoldout;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            if (projectile.numHits == 0)
                _pendingSoundEvent = 2;
        }

        public override void AI(Projectile projectile)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            if (projectile.ModProjectile is not ForbiddenOathbladeHoldout p)
                return;

            float animationProgress = p.Animation % p.useAnim;
            float time = animationProgress - (p.useAnim / 3f);
            float timeMax = p.useAnim - (p.useAnim / 3f);

            if (!p.willDie && time >= (int)(timeMax * 0.4f) && p.playSwingSound)
                _pendingSoundEvent = 1;

            bool changed =
                projectile.ai[0] != _lastAi0 ||
                projectile.ai[1] != _lastAi1 ||

                p.aimVel != _lastAimVel ||

                p.holding != _lastHolding ||
                p.doSwing != _lastDoSwing ||
                p.postSwing != _lastPostSwing ||

                p.useAnim != _lastUseAnim ||
                p.swingCount != _lastSwingCount ||

                p.finalFlip != _lastFinalFlip ||
                p.playSwingSound != _lastPlaySwingSound ||
                p.postSwingCooldown != _lastCooldown ||

                p.willDie != _lastWillDie ||
                p.hasLaunchedBlades != _lastLaunched ||

                _pendingSoundEvent != 0;

            if (!changed)
                return;

            var fields = new List<SyncField>
            {
                SyncField.U(ForbiddenOathField.Kind, 0),

                SyncField.I(ForbiddenOathField.Ai0, (int)projectile.ai[0]),
                SyncField.I(ForbiddenOathField.Ai1, (int)projectile.ai[1]),

                SyncField.F(ForbiddenOathField.AimVelX, p.aimVel.X),
                SyncField.F(ForbiddenOathField.AimVelY, p.aimVel.Y),

                SyncField.U(ForbiddenOathField.Holding, p.holding ? (byte)1 : (byte)0),
                SyncField.U(ForbiddenOathField.DoSwing, p.doSwing ? (byte)1 : (byte)0),
                SyncField.U(ForbiddenOathField.PostSwing, p.postSwing ? (byte)1 : (byte)0),

                SyncField.I(ForbiddenOathField.UseAnim, p.useAnim),
                SyncField.I(ForbiddenOathField.SwingCount, p.swingCount),

                SyncField.U(ForbiddenOathField.FinalFlip, p.finalFlip ? (byte)1 : (byte)0),
                SyncField.U(ForbiddenOathField.PlaySwingSound, p.playSwingSound ? (byte)1 : (byte)0),
                SyncField.I(ForbiddenOathField.PostSwingCooldown, p.postSwingCooldown),

                SyncField.U(ForbiddenOathField.WillDie, p.willDie ? (byte)1 : (byte)0),
                SyncField.U(ForbiddenOathField.HasLaunchedBlades, p.hasLaunchedBlades ? (byte)1 : (byte)0),
            };

            if (_pendingSoundEvent != 0)
                fields.Add(SyncField.U(ForbiddenOathField.SoundEvent, _pendingSoundEvent));

            CalamitySyncFix.SendVars("Calamity:ForbiddenOathblade", projectile, fields);

            _lastAi0 = projectile.ai[0];
            _lastAi1 = projectile.ai[1];

            _lastAimVel = p.aimVel;

            _lastHolding = p.holding;
            _lastDoSwing = p.doSwing;
            _lastPostSwing = p.postSwing;

            _lastUseAnim = p.useAnim;
            _lastSwingCount = p.swingCount;

            _lastFinalFlip = p.finalFlip;
            _lastPlaySwingSound = p.playSwingSound;
            _lastCooldown = p.postSwingCooldown;

            _lastWillDie = p.willDie;
            _lastLaunched = p.hasLaunchedBlades;

            _pendingSoundEvent = 0;
        }
    }
}