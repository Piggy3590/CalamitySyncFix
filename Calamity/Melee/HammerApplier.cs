using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Terraria;
using CalamityMod.Projectiles.Melee;
using Terraria.Audio;
using Terraria.ID;

namespace CalamitySyncFix.Calamity.Melee;

public static class HammerField
{
    public const byte Kind = 1;          // byte: 0=Galaxy,1=Fallen,2=Pwnage
    public const byte ReturnHammer = 2;  // int
    public const byte Time = 3;          // int
    public const byte WaitTimer = 4;     // float (Galaxy Smasher)
    public const byte EchoPrep = 5;      // float (Galaxy Smasher)
    public const byte InPulse = 6;       // int (Galaxy Smasher)
    public const byte Empowered = 7;     // int (stack)
    public const byte TargetNpc = 8;     // int (ai[1] as npc whoAmI)
}

public class HammerApplier : ISyncApplier
{
    private static readonly Dictionary<long, int> LastReturnState = new();
    private static long Key(int owner, int identity) => ((long)owner << 32) | (uint)identity;
    
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

        // collect payload
        byte kind = 255;
        int returnhammer = 0;
        int time = 0;
        int inPulse = 0;
        int emp = 0;
        int targetNpc = -1;
        float wait = 0f;
        float prep = 0f;

        for (int i = 0; i < count; i++)
        {
            byte fieldId = r.ReadByte();
            SyncType typeId = (SyncType)r.ReadByte();

            switch (fieldId)
            {
                case HammerField.Kind:
                    kind = ReadByte(typeId, r);
                    break;

                case HammerField.ReturnHammer:
                    returnhammer = ReadInt(typeId, r);
                    break;

                case HammerField.Time:
                    time = ReadInt(typeId, r);
                    break;

                case HammerField.WaitTimer:
                    wait = ReadFloat(typeId, r);
                    break;

                case HammerField.EchoPrep:
                    prep = ReadFloat(typeId, r);
                    break;

                case HammerField.InPulse:
                    inPulse = ReadInt(typeId, r);
                    break;

                case HammerField.Empowered:
                    emp = ReadInt(typeId, r);
                    break;

                case HammerField.TargetNpc:
                    targetNpc = ReadInt(typeId, r);
                    break;

                default:
                    Skip(typeId, r);
                    break;
            }
        }

        Player plr = Main.player[owner];

        // Hammer types
        if (kind == 0 && proj.ModProjectile is GalaxySmasherHammer g)
        {
            if (Main.myPlayer != owner && returnhammer != g.returnhammer)
            {
                if (g.returnhammer == 2)
                {
                    if (Main.zenithWorld)
                        SoundEngine.PlaySound(GalaxySmasherHammer.UseSoundFunny with { Pitch = emp * 0.05f - 0.05f }, proj.Center);
                    else
                        SoundEngine.PlaySound(GalaxySmasherHammer.UseSound with { Pitch = emp * 0.05f - 0.05f }, proj.Center);
                }
                else if (g.returnhammer == 3)
                {
                    SoundEngine.PlaySound(GalaxySmasherHammer.RedHamSound, proj.Center);
                }
            }
            
            g.returnhammer = returnhammer;
            g.time = time;
            g.WaitTimer = wait;
            g.EchoHammerPrep = prep;
            g.InPulse = inPulse;

            proj.ai[1] = targetNpc;

            // stack ModPlayer
            plr.Calamity().GalaxyHammer = emp;

            proj.netUpdate = true;
            return;
        }

        if (kind == 1 && proj.ModProjectile is StellarContemptHammer s)
        {
            if (Main.myPlayer != owner && returnhammer != s.returnhammer)
            {
                if (s.returnhammer == 2)
                {
                    if (Main.zenithWorld)
                        SoundEngine.PlaySound(StellarContemptHammer.UseSoundFunny with { Pitch = emp * 0.1f - 0.1f }, proj.Center);
                    else
                        SoundEngine.PlaySound(StellarContemptHammer.UseSound with { Pitch = emp * 0.1f - 0.1f }, proj.Center);
                }
                else if (s.returnhammer == 3)
                {
                    SoundEngine.PlaySound(StellarContemptHammer.RedHamSound, proj.Center);
                }
            }
            
            s.returnhammer = returnhammer;
            s.time = time;

            proj.ai[1] = targetNpc;

            // stack ModPlayer
            plr.Calamity().StellarHammer = emp;

            proj.netUpdate = true;
            return;
        }

        if (kind == 1 && proj.ModProjectile is FallenPaladinsHammerProj f)
        {
            if (Main.myPlayer != owner && returnhammer != f.returnhammer)
            {
                if (f.returnhammer == 2)
                {
                    if (Main.zenithWorld)
                        SoundEngine.PlaySound(FallenPaladinsHammerProj.UseSoundFunny with { Pitch = emp * 0.2f - 0.4f }, proj.Center);
                    else
                        SoundEngine.PlaySound(FallenPaladinsHammerProj.UseSound with { Pitch = emp * 0.2f - 0.4f }, proj.Center);
                }
                else if (f.returnhammer == 3)
                {
                    SoundEngine.PlaySound(FallenPaladinsHammerProj.RedHamSound, proj.Center);
                }
            }
            f.returnhammer = returnhammer;
            f.time = time;

            proj.ai[1] = targetNpc;

            plr.Calamity().PHAThammer = emp;

            proj.netUpdate = true;
            return;
        }

        if (kind == 2 && proj.ModProjectile is PwnagehammerProj p)
        {
            p.time = time;

            proj.ai[1] = targetNpc;

            plr.Calamity().Holyhammer = emp;

            proj.netUpdate = true;
            return;
        }
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

    private static int ReadInt(SyncType t, BinaryReader r)
    {
        return t switch
        {
            SyncType.Int32 => r.ReadInt32(),
            SyncType.Byte => r.ReadByte(),
            SyncType.Bool => r.ReadBoolean() ? 1 : 0,
            SyncType.Float => (int)r.ReadSingle(),
            _ => 0
        };
    }

    private static float ReadFloat(SyncType t, BinaryReader r)
    {
        return t switch
        {
            SyncType.Float => r.ReadSingle(),
            SyncType.Int32 => r.ReadInt32(),
            SyncType.Byte => r.ReadByte(),
            SyncType.Bool => r.ReadBoolean() ? 1f : 0f,
            _ => 0f
        };
    }

    private static byte ReadByte(SyncType t, BinaryReader r)
    {
        return t switch
        {
            SyncType.Byte => r.ReadByte(),
            SyncType.Bool => (byte)(r.ReadBoolean() ? 1 : 0),
            SyncType.Int32 => (byte)r.ReadInt32(),
            SyncType.Float => (byte)r.ReadSingle(),
            _ => 0
        };
    }

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
            r.ReadByte(); // fieldId
            Skip((SyncType)r.ReadByte(), r); // typeId + value
        }
    }
}