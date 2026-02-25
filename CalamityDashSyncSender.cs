using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace CalamitySyncFix
{
    public class CalamityDashSyncSender : ModPlayer
    {
        private int _cooldown;

        // Dash Tick
        private const int DashWindow = 30;

        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            if (Player.whoAmI != Main.myPlayer)
                return;

            int t = Player.timeSinceLastDashStarted;

            // Init Dash~DashWindow
            bool dashing = (t >= 0 && t <= DashWindow);

            if (!dashing)
            {
                _cooldown = 0;
                return;
            }

            // two ticks
            if (_cooldown-- > 0)
                return;

            _cooldown = 2;

            var mod = ModContent.GetInstance<CalamitySyncFix>();
            ModPacket p = mod.GetPacket();
            p.Write((byte)CalamitySyncFix.PacketKind.SyncDash);
            p.Write((byte)Player.whoAmI);

            Vector2 c = Player.Center;
            Vector2 v = Player.velocity;

            p.Write(c.X); p.Write(c.Y);
            p.Write(v.X); p.Write(v.Y);

            p.Write((byte)t);

            p.Send(); // to server
        }
    }
}