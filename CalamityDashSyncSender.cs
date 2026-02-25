using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace CalamitySyncFix
{
    public class CalamityDashSyncSender : ModPlayer
    {
        private int _cooldown;

        // 대시 구간(틱). 필요하면 30으로 올려도 됨.
        private const int DashWindow = 24;

        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            if (Player.whoAmI != Main.myPlayer)
                return;

            int t = Player.timeSinceLastDashStarted;

            // 대시 시작 직후~DashWindow까지를 "대시 중"으로 간주
            bool dashing = (t >= 0 && t <= DashWindow);

            if (!dashing)
            {
                _cooldown = 0;
                return;
            }

            // 2틱마다 전송(네트워크 부담 vs 정확도 타협점)
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

            // (선택) timeSinceLastDashStarted도 같이 보내면 수신측이 보간/판정에 쓸 수 있음
            p.Write((byte)t);

            p.Send(); // to server
        }
    }
}