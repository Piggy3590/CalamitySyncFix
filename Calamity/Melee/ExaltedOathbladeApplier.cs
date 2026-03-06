using System.IO;
using System.Reflection;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamitySyncFix.Calamity.Melee
{
    public static class ExaltedOathField
        {
            public const byte Kind = 1;

            // player state
            public const byte DemonSwordKillMode = 2;
            public const byte KillModeCooldown = 3;

            // holdout state
            public const byte Holding = 10;
            public const byte DoSwing = 11;
            public const byte PostSwing = 12;
            public const byte UseAnim = 13;
            public const byte SwingCount = 14;
            public const byte FinalFlip = 15;
            public const byte PostSwingCooldown = 16;
            public const byte WillDie = 17;
            public const byte HasLaunchedBlades = 18;
            public const byte LastSwingId = 19;

            public const byte SoundEvent = 20;
        }
    
    public class ExaltedOathbladeApplier : ISyncApplier
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

            bool killMode = false;
            int killModeCooldown = 0;

            bool holding = false;
            bool doSwing = false;
            bool postSwing = false;
            bool finalFlip = false;
            bool willDie = false;
            bool launch = false;

            int useAnim = 0;
            int swing = 0;
            int cooldown = 0;
            int lastSwingId = 0;

            byte soundEvent = 0;

            for (int i = 0; i < count; i++)
            {
                byte field = r.ReadByte();
                SyncType type = (SyncType)r.ReadByte();

                switch (field)
                {
                    case ExaltedOathField.DemonSwordKillMode:
                        killMode = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.KillModeCooldown:
                        killModeCooldown = ReadInt(type, r);
                        break;

                    case ExaltedOathField.Holding:
                        holding = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.DoSwing:
                        doSwing = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.PostSwing:
                        postSwing = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.FinalFlip:
                        finalFlip = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.WillDie:
                        willDie = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.HasLaunchedBlades:
                        launch = ReadByte(type, r) != 0;
                        break;

                    case ExaltedOathField.UseAnim:
                        useAnim = ReadInt(type, r);
                        break;

                    case ExaltedOathField.SwingCount:
                        swing = ReadInt(type, r);
                        break;

                    case ExaltedOathField.PostSwingCooldown:
                        cooldown = ReadInt(type, r);
                        break;

                    case ExaltedOathField.LastSwingId:
                        lastSwingId = ReadInt(type, r);
                        break;

                    case ExaltedOathField.SoundEvent:
                        soundEvent = ReadByte(type, r);
                        break;

                    default:
                        Skip(type, r);
                        break;
                }
            }

            Player plr = Main.player[owner];
            plr.Calamity().demonSwordKillMode = killMode;
            plr.Calamity().killModeCooldown = killModeCooldown;

            if (proj != null && proj.ModProjectile?.GetType().Name == "ExaltedOathbladeHoldout")
            {
                dynamic p = proj.ModProjectile;

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
        }

        private static void PlaySoundEvent(byte soundEvent, dynamic p, Projectile proj)
        {
            if (soundEvent == 0)
                return;
            
            switch (soundEvent)
            {
                case 1:
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordSwing", 2) with
                    {
                        Volume = 0.85f,
                        Pitch = Main.rand.NextFloat(-0.5f, -0.4f)
                    }, proj.Center);
                    break;

                case 2:
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordFinalStrike") with
                    {
                        Volume = 1f
                    }, proj.Center);
                    break;

                case 3:
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordInsaneImpact") with
                    {
                        Volume = 0.8f,
                        Pitch = MathHelper.Clamp((float)p.swingCount * 0.05f, -0.25f, 0.5f)
                    }, proj.Center);
                    break;

                case 4:
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong") with
                    {
                        Volume = 0.9f,
                        Pitch = 0.3f
                    }, proj.Center);
                    break;
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