using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamitySyncFix
{
    public static class DashSmoother
    {
        private struct Target
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public int LastRecvTick;
        }

        private static readonly Dictionary<int, Target> Targets = new();

        public static void SetTarget(int who, Vector2 pos, Vector2 vel)
        {
            Targets[who] = new Target
            {
                Pos = pos,
                Vel = vel,
                LastRecvTick = (int)Main.GameUpdateCount
            };
        }

        public static bool TryGet(int who, out Vector2 pos, out Vector2 vel, out int age)
        {
            if (Targets.TryGetValue(who, out var t))
            {
                pos = t.Pos;
                vel = t.Vel;
                age = (int)Main.GameUpdateCount - t.LastRecvTick;
                return true;
            }
            pos = default;
            vel = default;
            age = 9999;
            return false;
        }

        public static void Clear(int who) => Targets.Remove(who);
    }
}