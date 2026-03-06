using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using CalamityMod.Projectiles;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace CalamitySyncFix.Calamity.Ranged
{
    public class GrapeBeerSyncGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        
        public bool initialized;
        public bool sentInitialSync;
        public bool syncedGrapeBeer;
        public float syncedConditionalHomingRange;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            var cal = projectile.GetGlobalProjectile<CalamityGlobalProjectile>();

            syncedGrapeBeer = cal.grapeBeer;
            syncedConditionalHomingRange = (int)cal.conditionalHomingRange;
            initialized = true;

            if (Main.netMode != NetmodeID.SinglePlayer &&
                projectile.owner == Main.myPlayer &&
                !sentInitialSync)
            {
                sentInitialSync = true;
                projectile.netUpdate = true;
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(syncedGrapeBeer);
            binaryWriter.Write(syncedConditionalHomingRange);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            syncedGrapeBeer = bitReader.ReadBit();
            syncedConditionalHomingRange = binaryReader.ReadInt32();

            var cal = projectile.GetGlobalProjectile<CalamityGlobalProjectile>();
            cal.grapeBeer = syncedGrapeBeer;
            cal.conditionalHomingRange = syncedConditionalHomingRange;
        }

        public override void PostAI(Projectile projectile)
        {
            var cal = projectile.GetGlobalProjectile<CalamityGlobalProjectile>();

            bool changed =
                syncedGrapeBeer != cal.grapeBeer ||
                syncedConditionalHomingRange != cal.conditionalHomingRange;

            if (changed)
            {
                syncedGrapeBeer = cal.grapeBeer;
                syncedConditionalHomingRange = cal.conditionalHomingRange;

                if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer)
                    projectile.netUpdate = true;
            }
        }
    }
}