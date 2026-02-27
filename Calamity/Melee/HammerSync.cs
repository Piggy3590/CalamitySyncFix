using System;
using System.Collections.Generic;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Projectiles.Melee;
using Terraria.Audio;

namespace CalamitySyncFix.Calamity.Melee
{
    // weaponKey: "Calamity:Hammers"
    public class HammerSync : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private int _lastState = int.MinValue;   // returnhammer
        private float _lastPrep = float.NaN;     // EchoHammerPrep
        private float _lastWait = float.NaN;     // WaitTimer
        private int _lastPulse = int.MinValue;   // InPulse
        private int _lastTime = int.MinValue;    // time
        private int _lastEmp = int.MinValue;     // player stack (GalaxyHammer / PHAThammer / Holyhammer)
        private int _lastTarget = int.MinValue;  // ai[1] npc index
        private int _sendDelayRemaining;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            if (entity.ModProjectile?.Mod?.Name != "CalamityMod")
                return false;

            return entity.ModProjectile is GalaxySmasherHammer
                || entity.ModProjectile is StellarContemptHammer
                || entity.ModProjectile is FallenPaladinsHammerProj
                || entity.ModProjectile is PwnagehammerProj;
        }

        private bool CanSendThisTick(int tickDelay)
        {
            if (tickDelay <= 0)
                return true;

            if (_sendDelayRemaining > 0)
            {
                _sendDelayRemaining--;
                return false;
            }

            _sendDelayRemaining = tickDelay;
            return true;
        }

        public override void AI(Projectile projectile)
        {
            // skip non-owner
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            Player owner = Main.player[projectile.owner];
            string projectileName = projectile.ModProjectile?.GetType().Name ?? string.Empty;

            if (!SyncConfigAccess.IsMeleeProjectileSyncEnabled(projectileName))
                return;

            // ---- GalaxySmasherHammer ----
            if (projectile.ModProjectile is GalaxySmasherHammer g)
            {
                int emp = owner.Calamity().GalaxyHammer;
                int targetNpc = (int)projectile.ai[1];

                bool changed =
                    g.returnhammer != _lastState ||
                    Math.Abs(g.EchoHammerPrep - _lastPrep) >= 0.25f ||
                    Math.Abs(g.WaitTimer - _lastWait) >= 0.25f ||
                    g.InPulse != _lastPulse ||
                    g.time != _lastTime ||
                    emp != _lastEmp ||
                    targetNpc != _lastTarget;

                if (!changed)
                    return;
                
                if (!CanSendThisTick(SyncConfigAccess.GetHammerTickDelay(projectileName)))
                    return;
                
                byte soundTrigger = 0;

                if (_lastState != g.returnhammer)
                {
                    if (g.returnhammer == 2)
                        soundTrigger = 1;
                    else if (g.returnhammer == 3)
                        soundTrigger = 2;
                }

                var fields = new List<SyncField>(8)
                {
                    SyncField.U(HammerField.Kind, 0),
                    SyncField.I(HammerField.ReturnHammer, g.returnhammer),
                    SyncField.I(HammerField.Time, g.time),
                    SyncField.F(HammerField.WaitTimer, g.WaitTimer),
                    SyncField.F(HammerField.EchoPrep, g.EchoHammerPrep),
                    SyncField.I(HammerField.InPulse, g.InPulse),
                    SyncField.I(HammerField.Empowered, emp),
                    SyncField.I(HammerField.TargetNpc, targetNpc),
                    SyncField.U(HammerField.SoundTrigger, soundTrigger),
                };

                _lastState = g.returnhammer;
                _lastPrep = g.EchoHammerPrep;
                _lastWait = g.WaitTimer;
                _lastPulse = g.InPulse;
                _lastTime = g.time;
                _lastEmp = emp;
                _lastTarget = targetNpc;

                CalamitySyncFix.SendVars("Calamity:Hammers", projectile, fields);
                return;
            }
            
            if (projectile.ModProjectile is StellarContemptHammer s)
            {
                int emp = owner.Calamity().StellarHammer;
                int targetNpc = (int)projectile.ai[1];
                
                bool changed =
                    s.returnhammer != _lastState ||
                    s.time != _lastTime ||
                    emp != _lastEmp ||
                    targetNpc != _lastTarget;

                if (!changed)
                    return;
                
                if (!CanSendThisTick(SyncConfigAccess.GetHammerTickDelay(projectileName)))
                    return;
                
                byte soundTrigger = 0;

                if (_lastState != s.returnhammer)
                {
                    if (s.returnhammer == 2)
                        soundTrigger = 1;
                    else if (s.returnhammer == 3)
                        soundTrigger = 2;
                }

                var fields = new List<SyncField>(5)
                {
                    SyncField.U(HammerField.Kind, 1),
                    SyncField.I(HammerField.ReturnHammer, s.returnhammer),
                    SyncField.I(HammerField.Time, s.time),
                    SyncField.I(HammerField.Empowered, emp),
                    SyncField.I(HammerField.TargetNpc, targetNpc),
                    SyncField.U(HammerField.SoundTrigger, soundTrigger),
                };

                _lastState = s.returnhammer;
                _lastTime = s.time;
                _lastEmp = emp;
                _lastTarget = targetNpc;

                CalamitySyncFix.SendVars("Calamity:Hammers", projectile, fields);
                return;
            }

            // ---- FallenPaladinsHammerProj ----
            if (projectile.ModProjectile is FallenPaladinsHammerProj f)
            {
                int emp = owner.Calamity().PHAThammer;
                int targetNpc = (int)projectile.ai[1];
                byte soundTrigger = 0;

                if (_lastState != f.returnhammer)
                {
                    if (f.returnhammer == 2)
                        soundTrigger = 1;
                }
                
                if (f.returnhammer == 2 && emp == 3 && projectile.Hitbox.Intersects(Main.player[projectile.owner].Hitbox))
                {
                    soundTrigger = 2;
                }

                bool changed =
                    f.returnhammer != _lastState ||
                    f.time != _lastTime ||
                    emp != _lastEmp ||
                    targetNpc != _lastTarget ||
                    soundTrigger != 0;

                if (!changed)
                    return;

                if (!CanSendThisTick(SyncConfigAccess.GetHammerTickDelay(projectileName)))
                    return;

                var fields = new List<SyncField>(5)
                {
                    SyncField.U(HammerField.Kind, 2),
                    SyncField.I(HammerField.ReturnHammer, f.returnhammer),
                    SyncField.I(HammerField.Time, f.time),
                    SyncField.I(HammerField.Empowered, emp),
                    SyncField.I(HammerField.TargetNpc, targetNpc),
                    SyncField.U(HammerField.SoundTrigger, soundTrigger),
                };

                _lastState = f.returnhammer;
                _lastTime = f.time;
                _lastEmp = emp;
                _lastTarget = targetNpc;

                CalamitySyncFix.SendVars("Calamity:Hammers", projectile, fields);
                return;
            }

            // ---- PwnagehammerProj ----
            if (projectile.ModProjectile is PwnagehammerProj p)
            {
                int emp = owner.Calamity().Holyhammer;
                int targetNpc = (int)projectile.ai[1];

                bool changed =
                    p.time != _lastTime ||
                    emp != _lastEmp ||
                    targetNpc != _lastTarget;

                if (!changed)
                    return;

                if (!CanSendThisTick(SyncConfigAccess.GetHammerTickDelay(projectileName)))
                    return;

                var fields = new List<SyncField>(4)
                {
                    SyncField.U(HammerField.Kind, 3),
                    SyncField.I(HammerField.Time, p.time),
                    SyncField.I(HammerField.Empowered, emp),
                    SyncField.I(HammerField.TargetNpc, targetNpc),
                };

                _lastTime = p.time;
                _lastEmp = emp;
                _lastTarget = targetNpc;

                CalamitySyncFix.SendVars("Calamity:Hammers", projectile, fields);
                return;
            }
        }
    }
}