using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix.Calamity.Melee;

public class GalaxySmasherEchoFix : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        if (entity.ModProjectile?.Mod?.Name != "CalamityMod")
            return false;

        return entity.ModProjectile is GalaxySmasherEcho
               || entity.ModProjectile is FallenPaladinsHammerEcho
               || entity.ModProjectile is PwnagehammerEcho
               || entity.ModProjectile is StellarContemptEcho;
    }

    public override void AI(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;
        NPC targeted = Main.npc[(int)projectile.ai[1]];
        if (projectile.Hitbox.Intersects(targeted.Hitbox))
        {
            projectile.Kill();
            projectile.netUpdate = true;
        }
    }
}