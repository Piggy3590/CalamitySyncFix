namespace CalamitySyncFix.Calamity.Melee;

using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

public static class ArkField
{
    public const byte AimAngle = 10; // float radians
}

public class ArkAttacksFix : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    private bool fixedTimeLeft;

    // sync ParryHoldout direction, sent by owner, once
    private bool sentAimOnce;
    private int aimSendCooldown = 2; // wait 1~2 tick, after instantiated

    // Target projectile "class name" keys (Calamity registers by class name)
    private const string AncientsSwungBlade = "ArkoftheAncientsSwungBlade";
    private const string AncientsParryHoldout = "ArkoftheAncientsParryHoldout";
    private const string TrueAncientsSwungBlade = "TrueArkoftheAncientsSwungBlade";
    private const string TrueAncientsParryHoldout = "TrueArkoftheAncientsParryHoldout";

    private const string CosmosSwungBlade = "ArkoftheCosmosSwungBlade";
    private const string CosmosBlast = "ArkoftheCosmosBlast";
    private const string CosmosParryHoldout = "ArkoftheCosmosParryHoldout";

    private const string ElementsSwungBlade = "ArkoftheElementsSwungBlade";
    private const string ElementsParryHoldout = "ArkoftheElementsParryHoldout";

    // if timeLeft is way larger than any expected max, treat it as "init skipped"
    private const int BadTimeLeftThreshold = 200;

    // weaponKey: ark melees(?)
    private const string WeaponKey = "Calamity:Ark";

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        if (entity.ModProjectile == null) return false;
        if (entity.ModProjectile.Mod?.Name != "CalamityMod") return false;

        string n = entity.ModProjectile.GetType().Name;

        return n == AncientsSwungBlade ||
               n == AncientsParryHoldout ||
               n == TrueAncientsSwungBlade ||
               n == TrueAncientsParryHoldout ||
               n == CosmosSwungBlade ||
               n == CosmosBlast ||
               n == CosmosParryHoldout ||
               n == ElementsSwungBlade ||
               n == ElementsParryHoldout;
    }

    public override void AI(Projectile projectile)
    {
        // Ignore server
        if (Main.netMode == NetmodeID.Server)
            return;

        string n = projectile.ModProjectile?.GetType().Name ?? "";

        bool isParryHoldout = (n == CosmosParryHoldout || n == ElementsParryHoldout);

        // -------------------------
        // Owner: Send ParryHoldout direction OONLY once
        // -------------------------
        if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer == projectile.owner)
        {
            if (isParryHoldout && !sentAimOnce)
            {
                aimSendCooldown--;
                if (aimSendCooldown <= 0)
                {
                    float ang = projectile.velocity.LengthSquared() > 0.0001f
                        ? projectile.velocity.ToRotation()
                        : projectile.rotation;

                    var fields = new List<SyncField>(1)
                    {
                        SyncField.F(ArkField.AimAngle, ang)
                    };

                    CalamitySyncFix.SendVars(WeaponKey, projectile, fields);

                    sentAimOnce = true;
                }
            }

            return; // owner authoritative
        }

        // -------------------------
        // 2) non-owner timeLeft init-skipped clamp one time
        // -------------------------
        if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer != projectile.owner)
        {
            if (fixedTimeLeft)
                return;

            if (projectile.timeLeft > BadTimeLeftThreshold)
            {
                int expected = GetExpectedTimeLeft(projectile);
                if (expected > 0)
                {
                    projectile.timeLeft = expected;
                    fixedTimeLeft = true;
                }
            }
        }
    }

    private static int GetExpectedTimeLeft(Projectile p)
    {
        string n = p.ModProjectile?.GetType().Name ?? "";

        // === Fractured ===
        if (n == AncientsSwungBlade) return 35;
        if (n == AncientsParryHoldout) return 340;

        // === True ===
        if (n == TrueAncientsSwungBlade) return 40;
        if (n == TrueAncientsParryHoldout) return 340;

        // === Cosmos ===
        if (n == CosmosSwungBlade)
        {
            float combo = p.ai[0];
            bool thrown = combo == 2f || combo == 3f;
            if (thrown) return 140;

            bool swirl = combo == 1f;
            return swirl ? 55 : 35;
        }

        if (n == CosmosBlast) return 70;
        if (n == CosmosParryHoldout) return 340;

        // === Elements ===
        if (n == ElementsSwungBlade)
        {
            float combo = p.ai[0];
            bool thrown = combo == 2f || combo == 3f;
            return thrown ? 80 : 35;
        }

        if (n == ElementsParryHoldout) return 340;

        return -1;
    }
}