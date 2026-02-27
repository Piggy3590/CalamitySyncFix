using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Projectiles.Melee;

/*
namespace CalamitySyncFix.Calamity.Melee
{
    public class HammerSoundServerHook : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private int _lastState = int.MinValue;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.ModProjectile is GalaxySmasherHammer
               || entity.ModProjectile is StellarContemptHammer
               || entity.ModProjectile is FallenPaladinsHammerProj;

        public override void AI(Projectile projectile)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.myPlayer != projectile.owner)
                return;

            int state = 0;
            byte kind = 255;
            float pitch = 0f;

            Player owner = Main.player[projectile.owner];

            if (projectile.ModProjectile is GalaxySmasherHammer g)
            {
                kind = 0;
                state = g.returnhammer;
                int emp = owner.Calamity().GalaxyHammer;
                pitch = emp * 0.05f - 0.05f;
            }
            else if (projectile.ModProjectile is StellarContemptHammer s)
            {
                kind = 1;
                state = s.returnhammer;
                int emp = owner.Calamity().StellarHammer;
                pitch = emp * 0.1f - 0.1f;
            }
            else if (projectile.ModProjectile is FallenPaladinsHammerProj f)
            {
                kind = 2;
                state = f.returnhammer;
                int emp = owner.Calamity().PHAThammer;
                pitch = emp * 0.2f - 0.4f;
            }
            else
            {
                return;
            }

            if (state == _lastState)
                return;
            if (state == 2)
                CalamitySyncFix.BroadcastHammerSound(CalamitySyncFix.HammerSoundEvent.Use, kind, projectile.Center, pitch);
            else if (state == 3)
                CalamitySyncFix.BroadcastHammerSound(CalamitySyncFix.HammerSoundEvent.RedHam, kind, projectile.Center, 0f);

            _lastState = state;
        }
    }
}
*/