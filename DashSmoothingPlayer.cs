using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix
{
    public class DashSmoothingPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            // 내 로컬은 건드리지 않음
            if (Player.whoAmI == Main.myPlayer)
                return;

            if (!DashSmoother.TryGet(Player.whoAmI, out Vector2 targetPos, out Vector2 targetVel, out int age))
                return;

            if (age > 20)
            {
                DashSmoother.Clear(Player.whoAmI);
                return;
            }
            Vector2 diff = targetPos - Player.Center;
            float dist2 = diff.LengthSquared();

            if (dist2 > 900f)
            {
                Player.Center = targetPos;
            }
            else
            {
                Player.Center = Vector2.Lerp(Player.Center, targetPos, 0.35f);
            }

            Player.velocity = Vector2.Lerp(Player.velocity, targetVel, 0.5f);
        }
    }
}