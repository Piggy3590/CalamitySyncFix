using System.IO;
using System.Reflection;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamitySyncFix.Calamity.Melee
{
    public static class DevilsDevField
    {
        public const byte Kind = 1;

        public const byte Holding = 2;
        public const byte DoSwing = 3;
        public const byte PostSwing = 4;

        public const byte UseAnim = 5;
        public const byte SwingCount = 6;

        public const byte FinalFlip = 7;
        public const byte PostSwingCooldown = 8;

        public const byte WillDie = 9;
        public const byte HasLaunchedBlades = 10;

        // 0 none, 1 swing whoosh, 2 final strike start, 3 hit impact, 4 kill finish
        public const byte SoundEvent = 11;
    }

    public class DevilsDevastationApplier : ISyncApplier
    {
        public void Apply(int owner, int identity, BinaryReader r, int count)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Consume(r, count);
                return;
            }

            if (Main.myPlayer == owner)
            {
                Consume(r, count);
                return;
            }

            Projectile proj = FindProj(owner, identity);
            if (proj == null || proj.ModProjectile is not DevilsDevastationHoldout p)
            {
                Consume(r, count);
                return;
            }

            bool holding = p.holding;
            bool doSwing = p.doSwing;
            bool postSwing = p.postSwing;
            bool finalFlip = p.finalFlip;
            bool willDie = p.willDie;
            bool launch = p.hasLaunchedBlades;

            int useAnim = p.useAnim;
            int swing = p.swingCount;
            int cooldown = p.postSwingCooldown;
            int lastSwingId = 0;

            byte soundEvent = 0;

            for (int i = 0; i < count; i++)
            {
                byte field = r.ReadByte();
                SyncType type = (SyncType)r.ReadByte();

                switch (field)
                {
                    case DevilsDevField.Holding:
                        holding = ReadByte(type, r) != 0;
                        break;

                    case DevilsDevField.DoSwing:
                        doSwing = ReadByte(type, r) != 0;
                        break;

                    case DevilsDevField.PostSwing:
                        postSwing = ReadByte(type, r) != 0;
                        break;

                    case DevilsDevField.FinalFlip:
                        finalFlip = ReadByte(type, r) != 0;
                        break;

                    case DevilsDevField.WillDie:
                        willDie = ReadByte(type, r) != 0;
                        break;

                    case DevilsDevField.HasLaunchedBlades:
                        launch = ReadByte(type, r) != 0;
                        break;

                    case DevilsDevField.UseAnim:
                        useAnim = ReadInt(type, r);
                        break;

                    case DevilsDevField.SwingCount:
                        swing = ReadInt(type, r);
                        break;

                    case DevilsDevField.PostSwingCooldown:
                        cooldown = ReadInt(type, r);
                        break;

                    case DevilsDevField.SoundEvent:
                        soundEvent = ReadByte(type, r);
                        break;

                    default:
                        Skip(type, r);
                        break;
                }
            }

            p.holding = holding;
            p.doSwing = doSwing;
            p.postSwing = postSwing;
            p.useAnim = useAnim;
            p.swingCount = swing;
            p.finalFlip = finalFlip;
            p.postSwingCooldown = cooldown;
            p.willDie = willDie;
            p.hasLaunchedBlades = launch;

            PlaySoundEvent(soundEvent, p, proj);

            proj.netUpdate = true;
        }

        private static void PlaySoundEvent(byte soundEvent, DevilsDevastationHoldout p, Projectile proj)
        {
            if (soundEvent == 0)
                return;

            switch (soundEvent)
            {
                case 1:
                {
                    SoundStyle swing = new("CalamityMod/Sounds/Item/DemonSwordSwing", 2);
                    SoundEngine.PlaySound(swing with
                    {
                        Volume = 0.85f,
                        Pitch = Main.rand.NextFloat(-0.5f, -0.4f)
                    }, proj.Center);

                    SoundStyle heavy = new("CalamityMod/Sounds/Item/HeavySwing");
                    SoundEngine.PlaySound(heavy with
                    {
                        Volume = 0.65f,
                        Pitch = Main.rand.NextFloat(0.2f, 0.3f)
                    }, proj.Center);
                    break;
                }

                case 2:
                {
                    SoundStyle finalStrike = new("CalamityMod/Sounds/Item/DemonSwordFinalStrike");
                    Vector2 pos = p.lastHitTarget != null ? p.lastHitTarget.Center : proj.Center;
                    SoundEngine.PlaySound(finalStrike with
                    {
                        Volume = 1f,
                        Pitch = 0f
                    }, pos);
                    break;
                }

                case 3:
                {
                    SoundStyle impact = new("CalamityMod/Sounds/Item/DemonSwordInsaneImpact");
                    SoundEngine.PlaySound(impact with
                    {
                        Volume = 0.8f,
                        Pitch = MathHelper.Clamp(p.swingCount * 0.05f, -0.25f, 0.5f)
                    }, proj.Center);

                    SoundStyle impact2 = new("CalamityMod/Sounds/Item/HellkiteBigHit1");
                    SoundEngine.PlaySound(impact2 with
                    {
                        Volume = 0.8f,
                        Pitch = MathHelper.Clamp(p.swingCount * 0.025f, 0.4f, 0.65f)
                    }, proj.Center);
                    break;
                }

                case 4:
                {
                    SoundStyle finish = new("CalamityMod/Sounds/Item/LanceofDestinyStrong");
                    Vector2 pos = p.lastHitTarget != null ? p.lastHitTarget.Center : proj.Center;
                    SoundEngine.PlaySound(finish with
                    {
                        Volume = 0.9f,
                        Pitch = 0.3f
                    }, pos);
                    break;
                }
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
                r.ReadByte();
                Skip((SyncType)r.ReadByte(), r);
            }
        }
    }
}