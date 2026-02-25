using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix.Calamity.Melee;

public class ArkApplier : ISyncApplier
{
    private static bool IsArkProjectile(Projectile p)
    {
        if (p.ModProjectile?.Mod?.Name != "CalamityMod")
            return false;

        string n = p.ModProjectile.GetType().Name;

        return n == "ArkoftheCosmosParryHoldout"
               || n == "ArkoftheElementsParryHoldout"
               || n == "ArkoftheCosmosSwungBlade"
               || n == "ArkoftheElementsSwungBlade"
               || n == "ArkoftheAncientsSwungBlade"
               || n == "TrueArkoftheAncientsSwungBlade"
               || n == "ArkoftheAncientsParryHoldout"
               || n == "TrueArkoftheAncientsParryHoldout"
               || n == "ArkoftheCosmosBlast";
    }
    
    public void Apply(int owner, int identity, BinaryReader r, int count)
    {
        // only cli
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Consume(r, count);
            return;
        }

        // owner authoritative, do not apply
        if (Main.myPlayer == owner)
        {
            Consume(r, count);
            return;
        }

        Projectile proj = FindProj(owner, identity);
        if (proj == null)
        {
            Consume(r, count);
            return;
        }

        // only ark projectile
        if (proj.ModProjectile?.Mod?.Name != "CalamityMod")
        {
            Consume(r, count);
            return;
        }

        string n = proj.ModProjectile.GetType().Name;
        if (!IsArkProjectile(proj))
        {
            Consume(r, count);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            byte fieldId = r.ReadByte();
            SyncType typeId = (SyncType)r.ReadByte();

            switch (fieldId)
            {
                case ArkField.AimAngle:
                    float ang = ReadFloat(typeId, r);

                    string tn = proj.ModProjectile?.GetType().Name ?? "";
                    if (tn == "ArkoftheCosmosParryHoldout" || tn == "ArkoftheElementsParryHoldout")
                    {
                        proj.velocity = ang.ToRotationVector2();
                        proj.rotation = ang;
                        proj.netUpdate = true;
                    }
                    break;
                
                default:
                    Skip(typeId, r);
                    break;
            }
        }

        // net update
        proj.netUpdate = true;
    }

    private static Projectile FindProj(int owner, int identity)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile p = Main.projectile[i];
            if (p.active && p.owner == owner && p.identity == identity)
                return p;
        }
        return null;
    }

    private static int ReadInt(SyncType t, BinaryReader r) =>
        t switch
        {
            SyncType.Byte => r.ReadByte(),
            SyncType.Bool => r.ReadBoolean() ? 1 : 0,
            SyncType.Float => (int)r.ReadSingle(),
            _ => 0
        };

    private static float ReadFloat(SyncType t, BinaryReader r) =>
        t switch
        {
            SyncType.Float => r.ReadSingle(),
            SyncType.Int32 => r.ReadInt32(),
            SyncType.Byte => r.ReadByte(),
            SyncType.Bool => r.ReadBoolean() ? 1f : 0f,
            _ => 0f
        };

    private static void Skip(SyncType t, BinaryReader r)
    {
        switch (t)
        {
            case SyncType.Int32: r.ReadInt32(); break;
            case SyncType.Float: r.ReadSingle(); break;
            case SyncType.Byte: r.ReadByte(); break;
            case SyncType.Bool: r.ReadBoolean(); break;
        }
    }

    private static void Consume(BinaryReader r, int count)
    {
        for (int i = 0; i < count; i++)
        {
            r.ReadByte();
            Skip((SyncType)r.ReadByte(), r);
        }
    }
}