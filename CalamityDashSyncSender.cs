using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamitySyncFix
{
    public class CalamityDashSyncSender : ModPlayer
    {
        private int _cooldown;

        // Keep an early burst so dash start displacement is delivered immediately
        private const int DashWindow = 30;

        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            if (Player.whoAmI != Main.myPlayer)
                return;

            int t = Player.timeSinceLastDashStarted;

            // Init Dash~DashWindow
            bool dashing = t is >= 0 and <= DashWindow;
            
            if (!dashing)
            {
                _cooldown = 0;
                return;
            }

            // Vanilla-like movement sync burst while dash is active
            if (_cooldown-- > 0)
                return;

            _cooldown = 2;

            NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
            NetMessage.SendData(MessageID.SyncPlayer, number: Player.whoAmI);
        }
    }
}