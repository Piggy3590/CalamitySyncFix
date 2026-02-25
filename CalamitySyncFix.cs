using System.IO;
using System.Collections.Generic;
using CalamitySyncFix.Calamity.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamitySyncFix;

public enum SyncType : byte
{
    Int32 = 1,
    Float = 2,
    Byte = 3,
    Bool = 4,
}

public class CalamitySyncFix : Mod
{
    internal static CalamitySyncFix Instance { get; private set; }
    private static readonly Dictionary<string, ISyncApplier> Appliers = new();
    
    public override void Load()
    {
        // Appliers
        Appliers["Calamity:Hammers"] = new HammerApplier();
        Appliers["Calamity:Ark"] = new ArkApplier();
    }
    
    public override void Unload() => Instance = null;
    
    public enum PacketKind : byte
    {
        SyncVars = 1,
        SyncDash = 2,
    }
    
    public override void HandlePacket(BinaryReader r, int whoAmI)
    {
        PacketKind kind = (PacketKind)r.ReadByte();
        if (kind == PacketKind.SyncVars)
        {
            string weaponKey = r.ReadString();
            int owner = r.ReadInt32();
            int identity = r.ReadInt32();

            // fields
            byte count = r.ReadByte();

            // relay: resend everyone
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket p = GetPacket();
                p.Write((byte)PacketKind.SyncVars);
                p.Write(weaponKey);
                p.Write(owner);
                p.Write(identity);
                p.Write(count);

                // copy remaining payload
                // BinaryReader cannot give length, need to read field, and use it
                for (int i = 0; i < count; i++)
                {
                    byte fieldId = r.ReadByte();
                    SyncType typeId = (SyncType)r.ReadByte();

                    p.Write(fieldId);
                    p.Write((byte)typeId);

                    switch (typeId)
                    {
                        case SyncType.Int32: p.Write(r.ReadInt32()); break;
                        case SyncType.Float: p.Write(r.ReadSingle()); break;
                        case SyncType.Byte:  p.Write(r.ReadByte()); break;
                        case SyncType.Bool:  p.Write(r.ReadBoolean()); break;
                        default: return;
                    }
                }

                p.Send(); // broadcast
                return;
            }

            // Apply clients
            if (!Appliers.TryGetValue(weaponKey, out var applier))
            {
                // If the weapon is unknown, ignore that field
                for (int i = 0; i < count; i++)
                {
                    r.ReadByte(); // fieldId
                    SyncType typeId = (SyncType)r.ReadByte();
                    switch (typeId)
                    {
                        case SyncType.Int32: r.ReadInt32(); break;
                        case SyncType.Float: r.ReadSingle(); break;
                        case SyncType.Byte:  r.ReadByte(); break;
                        case SyncType.Bool:  r.ReadBoolean(); break;
                    }
                }
                return;
            }
            applier.Apply(owner, identity, r, count);
        }else if (kind == PacketKind.SyncDash)
        {
            byte who = r.ReadByte();
            float cx = r.ReadSingle();
            float cy = r.ReadSingle();
            float vx = r.ReadSingle();
            float vy = r.ReadSingle();
            byte dashT = r.ReadByte();

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket p = GetPacket();
                p.Write((byte)PacketKind.SyncDash);
                p.Write(who);
                p.Write(cx); p.Write(cy);
                p.Write(vx); p.Write(vy);
                p.Write(dashT);
                p.Send();
                return;
            }

            if (Main.myPlayer == who)
                return;

            Player other = Main.player[who];
            if (Main.myPlayer != who)
            {
                DashSmoother.SetTarget(who,
                    new Vector2(cx, cy),
                    new Vector2(vx, vy));
            }
        }
    }

    // Client to Server (Called by Owner)
    public static void SendVars(string weaponKey, Projectile proj, List<SyncField> fields)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Mod mod = ModContent.GetInstance<CalamitySyncFix>();
        ModPacket p = mod.GetPacket();

        p.Write((byte)PacketKind.SyncVars);
        p.Write(weaponKey);
        p.Write(proj.owner);
        p.Write(proj.identity);

        if (fields.Count > byte.MaxValue) fields.RemoveRange(byte.MaxValue, fields.Count - byte.MaxValue);
        p.Write((byte)fields.Count);

        foreach (var f in fields)
        {
            p.Write(f.FieldId);
            p.Write((byte)f.TypeId);
            switch (f.TypeId)
            {
                case SyncType.Int32: p.Write(f.I32); break;
                case SyncType.Float: p.Write(f.F32); break;
                case SyncType.Byte:  p.Write(f.U8); break;
                case SyncType.Bool:  p.Write(f.B); break;
            }
        }

        p.Send(); // to server
    }
}

// ----------------- public structures -----------------

public interface ISyncApplier
{
    void Apply(int owner, int identity, BinaryReader r, int count);
}

public struct SyncField
{
    public byte FieldId;
    public SyncType TypeId;

    public int I32;
    public float F32;
    public byte U8;
    public bool B;

    public static SyncField I(byte id, int v) => new() { FieldId = id, TypeId = SyncType.Int32, I32 = v };
    public static SyncField F(byte id, float v) => new() { FieldId = id, TypeId = SyncType.Float, F32 = v };
    public static SyncField U(byte id, byte v) => new() { FieldId = id, TypeId = SyncType.Byte, U8 = v };
    public static SyncField Bool(byte id, bool v) => new() { FieldId = id, TypeId = SyncType.Bool, B = v };
}