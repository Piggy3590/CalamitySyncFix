using CalamityMod.Items.Weapons.Melee;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Sounds;
using Terraria.Audio;
using Terraria.ID;

namespace CalamitySyncFix.Calamity.Melee
{
    public class ParryHoldoutHook : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.ModProjectile is ArkoftheCosmosParryHoldout
               || entity.ModProjectile is ArkoftheElementsParryHoldout
               || entity.ModProjectile is TrueArkoftheAncientsParryHoldout
               || entity.ModProjectile is ArkoftheAncientsParryHoldout;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.owner == Main.myPlayer)
                return;

            string projectileName = projectile.ModProjectile?.GetType().Name ?? string.Empty;
            if (!SyncConfigAccess.IsMeleeProjectileSyncEnabled(projectileName))
                return;

            byte parryType = 0;

            if (projectile.ModProjectile is ArkoftheCosmosParryHoldout cos)
            {
                if (cos.AlreadyParried > 0) return;
                parryType = 1;
            }
            else if (projectile.ModProjectile is ArkoftheElementsParryHoldout ele)
            {
                if (ele.AlreadyParried > 0) return;
                parryType = 2;
            }
            else if (projectile.ModProjectile is TrueArkoftheAncientsParryHoldout tru)
            {
                if (tru.AlreadyParried > 0) return;
                parryType = 3;
            }
            else if (projectile.ModProjectile is ArkoftheAncientsParryHoldout frac)
            {
                if (frac.AlreadyParried > 0) return;
                parryType = 4;
            }

            if (parryType == 0)
                return;

            var box = projectile.Hitbox;

            ModPacket p = ModContent.GetInstance<CalamitySyncFix>().GetPacket();
            p.Write((byte)CalamitySyncFix.PacketKind.ParrySound);
            p.Write(parryType);

            p.Write(projectile.Center.X);
            p.Write(projectile.Center.Y);

            p.Write(box.X);
            p.Write(box.Y);
            p.Write(box.Width);
            p.Write(box.Height);

            p.Send();
        }
    }
}