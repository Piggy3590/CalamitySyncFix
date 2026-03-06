using System.Collections.Generic;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix.Calamity.Melee
{
    public class ForbiddenOathbladePlayerSync : ModPlayer
    {
        private bool _lastKillMode;
        private int _lastCooldown = int.MinValue;

        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer)
                return;

            bool killMode = Player.Calamity().demonSwordKillMode;
            int cooldown = Player.Calamity().killModeCooldown;

            if (killMode == _lastKillMode && cooldown == _lastCooldown)
                return;

            int holdoutIndex = FindHoldout(Player.whoAmI);
            if (holdoutIndex == -1)
                return;

            Projectile proj = Main.projectile[holdoutIndex];

            var fields = new List<SyncField>
            {
                SyncField.U(ForbiddenOathField.Kind, 0),
                SyncField.U(ForbiddenOathField.DemonSwordKillMode, killMode ? (byte)1 : (byte)0),
                SyncField.I(ForbiddenOathField.KillModeCooldown, cooldown)
            };

            CalamitySyncFix.SendVars("Calamity:ForbiddenOathblade", proj, fields);

            _lastKillMode = killMode;
            _lastCooldown = cooldown;
        }

        private static int FindHoldout(int owner)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != owner || p.ModProjectile == null)
                    continue;

                if (p.ModProjectile.GetType().Name == "ForbiddenOathbladeHoldout")
                    return i;
            }
            return -1;
        }
    }
}