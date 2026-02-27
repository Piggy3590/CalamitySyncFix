using System.IO;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Sounds;
using CalamitySyncFix.Calamity.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
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
        HammerSound = 2,
        ParrySound = 3,
    }
    public enum HammerSoundEvent : byte
    {
        Use = 1,     // returnhammer == 2 진입(던지기/사용)
        RedHam = 2,  // returnhammer == 3 진입(강공/폭발)
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
        }else if (kind == PacketKind.HammerSound)
        {
            HammerSoundEvent ev = (HammerSoundEvent)r.ReadByte();
            byte hammerKind = r.ReadByte();
            float x = r.ReadSingle();
            float y = r.ReadSingle();
            float pitch = r.ReadSingle();

            if (Main.dedServ)
                return;

            var pos = new Vector2(x, y);
            
            switch (hammerKind)
            {
                case 0: // Galaxy
                    if (ev == HammerSoundEvent.Use)
                    {
                        if (Main.zenithWorld)
                            SoundEngine.PlaySound(GalaxySmasherHammer.UseSoundFunny with { Pitch = pitch }, pos);
                        else
                            SoundEngine.PlaySound(GalaxySmasherHammer.UseSound with { Pitch = pitch }, pos);
                    }
                    else if (ev == HammerSoundEvent.RedHam)
                    {
                        SoundEngine.PlaySound(GalaxySmasherHammer.RedHamSound, pos);
                    }
                    break;

                case 1: // Stellar
                    if (ev == HammerSoundEvent.Use)
                    {
                        if (Main.zenithWorld)
                            SoundEngine.PlaySound(StellarContemptHammer.UseSoundFunny with { Pitch = pitch }, pos);
                        else
                            SoundEngine.PlaySound(StellarContemptHammer.UseSound with { Pitch = pitch }, pos);
                    }
                    else if (ev == HammerSoundEvent.RedHam)
                    {
                        SoundEngine.PlaySound(StellarContemptHammer.RedHamSound, pos);
                    }
                    break;

                case 2: // Fallen
                    if (ev == HammerSoundEvent.Use)
                    {
                        if (Main.zenithWorld)
                            SoundEngine.PlaySound(FallenPaladinsHammerProj.UseSoundFunny with { Pitch = pitch }, pos);
                        else
                            SoundEngine.PlaySound(FallenPaladinsHammerProj.UseSound with { Pitch = pitch }, pos);
                    }
                    else if (ev == HammerSoundEvent.RedHam)
                    {
                        SoundEngine.PlaySound(FallenPaladinsHammerProj.RedHamSound, pos);
                    }
                    break;
                
                case 3:
                    break;
            }
            return;
        }else if (kind == PacketKind.ParrySound)
        {
            byte parryType = r.ReadByte();

            float cx = r.ReadSingle();
            float cy = r.ReadSingle();

            //  Hitbox Received
            int hx = r.ReadInt32();
            int hy = r.ReadInt32();
            int hw = r.ReadInt32();
            int hh = r.ReadInt32();

            if (Main.dedServ)
                return;

            Vector2 pos = new Vector2(cx, cy);
            Rectangle hitbox = new Rectangle(hx, hy, hw, hh);
            CombatText.NewText(hitbox, new Color(111, 247, 200), CalamityUtils.GetTextValue("Misc.ArkParry"), true);

            // 필요하면 디버그
            // Main.NewText($"HB: {hitbox}");

            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact, pos);

            if (parryType == 1 || parryType == 2)
            {
                SoundEngine.PlaySound(
                    CommonCalamitySounds.ScissorGuillotineSnapSound with
                    {
                        Volume = CommonCalamitySounds.ScissorGuillotineSnapSound.Volume * 1.3f
                    }, pos);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item67, pos);
            }

            return;
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
