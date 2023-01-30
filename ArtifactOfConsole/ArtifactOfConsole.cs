using BepInEx.Configuration;
using RoR2;
using MonoMod.Cil;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using EntityStates.Croco;
using RoR2.CharacterAI;
using UnityEngine.Networking;
using RoR2.Projectile;
using Mono.Cecil.Cil;
using R2API;
using R2API.Utils;
using RoR2.ConVar;

namespace ArtifactOfConsole.Artifact
{
    internal class ArtifactOfConsole : ArtifactBase<ArtifactOfConsole>
    {
        public override string ArtifactName => "Artifact of Console";

        public override string ArtifactLangTokenName => "HIFU_ArtifactOfConsole";

        public override string ArtifactDescription => "Experience a bunch of Risk of Rain 2 Console Edition bugs.";

        public override Sprite ArtifactEnabledIcon => Main.artifactofconsole.LoadAsset<Sprite>("texArtifactOfConsoleOn");

        public override Sprite ArtifactDisabledIcon => Main.artifactofconsole.LoadAsset<Sprite>("texArtifactOfConsoleOff");

        public override void Init(ConfigFile config)
        {
            var shield = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion();

            oldCleansingPoolDropTable = ScriptableObject.CreateInstance<OldCleansingPoolDropTable>();
            oldPowerMode = ScriptableObject.CreateInstance<BuffDef>();
            oldPowerMode.isDebuff = false;
            oldPowerMode.isHidden = false;
            oldPowerMode.canStack = false;
            oldPowerMode.buffColor = new Color32(214, 201, 58, 255);
            oldPowerMode.iconSprite = Sprite.Create(shield, new Rect(0f, 0f, (float)shield.width, (float)shield.height), new Vector2(0f, 0f));
            CreateLang();
            CreateArtifact();
            Hooks();
        }

        public static OldCleansingPoolDropTable oldCleansingPoolDropTable;
        public static BuffDef oldPowerMode;

        public override void Hooks()
        {
            CharacterBody.onBodyStartGlobal += RemoveOSP;
            CharacterMaster.onStartGlobal += ClingyDronesAndInvincibleUmbrae;
            IL.EntityStates.Croco.Bite.OnMeleeHitAuthority += RemoveM2Healing;
            IL.EntityStates.Croco.Slash.OnMeleeHitAuthority += RemoveM1Healing;
            IL.RoR2.CharacterBody.RecalculateStats += ChangeTriTipChance;
            IL.RoR2.CharacterBody.UpdateFireTrail += IncreaseFireTrailDamage;
            IL.RoR2.GlobalEventManager.OnHitEnemy += RemoveBandsOnHitChangeDeathMarkDebuffsRequirement;
            IL.RoR2.HealthComponent.Heal += RemoveAegisAndRejuvRack;
            IL.RoR2.HealthComponent.TakeDamage += NoFocusCrystalColorNerfedCrowbarNoPlanula;
            On.EntityStates.Croco.BaseLeap.OnEnter += HighSpeedLeap;
            On.RoR2.DamageDisplay.DoUpdate += RemoveDamageNumbersLateGame;
            On.RoR2.DamageInfo.ModifyDamageInfo += RandomNoDamageOnHit;
            On.RoR2.GlobalEventManager.OnHitAll += OverloadingTwoOrbs;
            On.RoR2.HealthComponent.Awake += HealthComponent_Awake;
            On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += IncreasedKnockback;
            On.RoR2.HealthComponent.TakeDamageForce_Vector3_bool_bool += IncreasedKnockback2;
            On.RoR2.Items.NearbyDamageBonusBodyBehavior.OnEnable += NoFocusCrystalVFX;
            On.RoR2.Run.IsExpansionEnabled += DisableSOTV;
            On.RoR2.UI.HUD.Awake += HUD_Awake;
            RoR2Application.onFixedUpdate += RandomCrashFpsDropsFPSLimit;
            SceneDirector.onPrePopulateMonstersSceneServer += CommencementLunarWispSpam;
            Stage.onServerStageBegin += RandomStageStuff;
            On.RoR2.CombatDirector.Spawn += LunarChimeraeBugs;
            On.RoR2.GlobalEventManager.OnInteractionBegin += GlobalEventManager_OnInteractionBegin;
            IL.RoR2.CharacterBody.AddMultiKill += CharacterBody_AddMultiKill;
            On.RoR2.Projectile.ProjectileImpactExplosion.OnProjectileImpact += ProjectileImpactExplosion_OnProjectileImpact;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.EntityStates.Toolbot.ToolbotDualWieldBase.OnEnter += ToolbotDualWieldBase_OnEnter;
            On.EntityStates.Loader.PreGroundSlam.OnEnter += PreGroundSlam_OnEnter;
            On.EntityStates.Loader.GroundSlam.OnEnter += GroundSlam_OnEnter;
            On.RoR2.RagdollController.BeginRagdoll += RagdollController_BeginRagdoll;
            On.RoR2.HoldoutZoneController.Start += HoldoutZoneController_Start;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            On.EntityStates.GravekeeperBoss.SpawnState.OnEnter += SpawnState_OnEnter;
            Changes();
        }

        private void SpawnState_OnEnter(On.EntityStates.GravekeeperBoss.SpawnState.orig_OnEnter orig, EntityStates.GravekeeperBoss.SpawnState self)
        {
            EntityStates.GravekeeperBoss.SpawnState.duration = 10f;
            orig(self);
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (ArtifactEnabled)
            {
                RoR2.Networking.NetworkManagerSystem.cvNetTimeSmoothRate = new FloatConVar("net_time_smooth_rate", ConVarFlags.None, "0", "The smoothing rate for the network time.");
                // vanilla is 1.05;
                RoR2.Networking.NetworkManagerSystem.svTimeTransmitInterval = new FloatConVar("sv_time_transmit_interval", ConVarFlags.Cheat, (10f / 60f).ToString(), "How long it takes for the server to issue a time update to clients.");
                // vanilla is 1/60;
            }
            else
            {
                RoR2.Networking.NetworkManagerSystem.cvNetTimeSmoothRate = new FloatConVar("net_time_smooth_rate", ConVarFlags.None, "1.05", "The smoothing rate for the network time.");
                // vanilla is 1.05;
                RoR2.Networking.NetworkManagerSystem.svTimeTransmitInterval = new FloatConVar("sv_time_transmit_interval", ConVarFlags.Cheat, 0.016666668f.ToString(), "How long it takes for the server to issue a time update to clients.");
                // vanilla is 1/60;
            }
        }

        private void HoldoutZoneController_Start(On.RoR2.HoldoutZoneController.orig_Start orig, HoldoutZoneController self)
        {
            if (ArtifactEnabled && self.transform.root.name == "Moon2DropshipZone")
            {
                self.holdoutZoneShape = HoldoutZoneController.HoldoutZoneShape.Sphere;
            }
            orig(self);
        }

        private void RagdollController_BeginRagdoll(On.RoR2.RagdollController.orig_BeginRagdoll orig, RagdollController self, Vector3 force)
        {
            if (ArtifactEnabled)
            {
                force *= 5f;
            }
            orig(self, force);
        }

        private void GroundSlam_OnEnter(On.EntityStates.Loader.GroundSlam.orig_OnEnter orig, EntityStates.Loader.GroundSlam self)
        {
            if (ArtifactEnabled)
            {
                for (int i = 0; i < 50; i++)
                    Util.PlaySound("Play_loader_R_variant_whooshDown", self.gameObject);
            }
            orig(self);
        }

        private void PreGroundSlam_OnEnter(On.EntityStates.Loader.PreGroundSlam.orig_OnEnter orig, EntityStates.Loader.PreGroundSlam self)
        {
            if (ArtifactEnabled)
            {
                for (int i = 0; i < 50; i++)
                    Util.PlaySound("Play_loader_R_variant_activate", self.gameObject);
            }
            orig(self);
        }

        private void ToolbotDualWieldBase_OnEnter(On.EntityStates.Toolbot.ToolbotDualWieldBase.orig_OnEnter orig, EntityStates.Toolbot.ToolbotDualWieldBase self)
        {
            if (ArtifactEnabled)
            {
                EntityStates.Toolbot.ToolbotDualWieldBase.bonusBuff = oldPowerMode;
            }
            orig(self);
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(oldPowerMode))
            {
                args.armorAdd += 200f;
            }
        }

        private void ProjectileImpactExplosion_OnProjectileImpact(On.RoR2.Projectile.ProjectileImpactExplosion.orig_OnProjectileImpact orig, ProjectileImpactExplosion self, ProjectileImpactInfo impactInfo)
        {
            if (ArtifactEnabled && self.gameObject.name.Contains("ImpVoidspikeProjectile"))
            {
                self.destroyOnEnemy = false;
                self.blastRadius = 0f;
                self.blastDamageCoefficient = 0f;
                self.blastProcCoefficient = 0f;
                self.projectileDamage.damageType = DamageType.Generic;
            }
            orig(self, impactInfo);
        }

        private void CharacterBody_AddMultiKill(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.multiKillCount))),
                    x => x.MatchLdcI4(4)))
            {
                c.Index += 2;
                c.EmitDelegate<Func<int, int>>((self) =>
                {
                    return ArtifactEnabled ? 2 : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Berzerker's Pauldron Buff Kill Requirement hook");
            }
        }

        private void GlobalEventManager_OnInteractionBegin(On.RoR2.GlobalEventManager.orig_OnInteractionBegin orig, GlobalEventManager self, Interactor interactor, IInteractable interactable, GameObject interactableObject)
        {
            if (ArtifactEnabled && interactableObject.name.Contains("ShrineCleanse"))
            {
                var shopTerminalBehavior = interactableObject.GetComponent<ShopTerminalBehavior>();
                shopTerminalBehavior.dropTable = oldCleansingPoolDropTable;
            }
            orig(self, interactor, interactable, interactableObject);
        }

        private bool LunarChimeraeBugs(On.RoR2.CombatDirector.orig_Spawn orig, CombatDirector self, SpawnCard spawnCard, EliteDef eliteDef, Transform spawnTarget, DirectorCore.MonsterSpawnDistance spawnDistance, bool preventOverhead, float valueMultiplier, DirectorPlacementRule.PlacementMode placementMode)
        {
            if (ArtifactEnabled && spawnCard.eliteRules == SpawnCard.EliteRules.Lunar)
            {
                spawnCard.eliteRules = SpawnCard.EliteRules.Default;
                spawnCard.directorCreditCost /= 2;
                self.eliteBias *= 5000;
                self.maxSpawnDistance *= 0.1f;
            }
            return orig(self, spawnCard, eliteDef, spawnTarget, spawnDistance, preventOverhead, valueMultiplier, placementMode);
        }

        private void ChangeTriTipChance(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcR4(10f),
                    x => x.MatchLdloc(out _),
                    x => x.MatchConvR4()))
            {
                c.Index += 2;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 9f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Tri-tip Dagger Chance hook");
            }
        }

        private bool DisableSOTV(On.RoR2.Run.orig_IsExpansionEnabled orig, Run self, RoR2.ExpansionManagement.ExpansionDef expansionDef)
        {
            if (ArtifactEnabled && expansionDef.nameToken == "DLC1_NAME")
            {
                return false;
            }
            return orig(self, expansionDef);
        }

        private void NoFocusCrystalColorNerfedCrowbarNoPlanula(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(10),
                x => x.MatchStfld(typeof(DamageInfo), nameof(DamageInfo.damageColorIndex))))
            {
                c.Index += 1;
                c.EmitDelegate<Func<int, int>>((self) =>
                {
                    return ArtifactEnabled ? 0 : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Focus Crystal Damage Color hook");
            }

            c.Index = 0;

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(1f),
                x => x.MatchLdcR4(0.75f)))
            {
                c.Index += 2;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 0.5f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Crowbar Damage hook");
            }

            c.Index = 0;

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(15f),
                x => x.MatchMul()))
            {
                c.Index += 1;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 0f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Crowbar Damage hook");
            }
        }

        private void NoFocusCrystalVFX(On.RoR2.Items.NearbyDamageBonusBodyBehavior.orig_OnEnable orig, RoR2.Items.NearbyDamageBonusBodyBehavior self)
        {
            if (ArtifactEnabled)
            {
                self.indicatorEnabled = false;
            }

            orig(self);
        }

        private void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            if (ArtifactEnabled)
            {
                /*
                var mainUIArea = self.mainUIPanel.transform;
                var springCanvas = mainUIArea.GetChild(0);

                for (int i = 0; i < springCanvas.childCount; i++)
                {
                    springCanvas.GetChild(i).localRotation = Quaternion.identity;
                }

                var healthBar = self.healthBar.transform;
                var shrunkenRoot = healthBar.GetChild(0);
                shrunkenRoot.transform.localScale = new Vector3(1, 1.5f, 1);

                var text = healthBar.GetChild(1);
                text.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                */
            }
            orig(self);
        }

        private void HealthComponent_Awake(On.RoR2.HealthComponent.orig_Awake orig, HealthComponent self)
        {
            if (ArtifactEnabled) self.gameObject.AddComponent<StupidBandController>();
            orig(self);
        }

        private void RemoveBandsOnHitChangeDeathMarkDebuffsRequirement(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(4f),
                x => x.MatchBltUn(out _)))
            {
                c.Index += 1;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? float.MaxValue : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Bands Damage Bug hook");
            }

            c.Index = 0;

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(16),
                    x => x.MatchLdcI4(4),
                    x => x.MatchBlt(out ILLabel IL_1180)))
            {
                c.Index += 2;
                c.EmitDelegate<Func<int, int>>((self) =>
                {
                    return ArtifactEnabled ? 5 : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Death Mark Minimum Debuffs hook");
            }
        }

        private void OverloadingTwoOrbs(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            if (damageInfo.procCoefficient > 0f && !damageInfo.rejected)
            {
                bool active = NetworkServer.active;
                if (damageInfo.attacker)
                {
                    CharacterBody component = damageInfo.attacker.GetComponent<CharacterBody>();
                    if (component)
                    {
                        CharacterMaster master = component.master;
                        if (master)
                        {
                            Inventory inventory = master.inventory;
                            if (master.inventory)
                            {
                                if ((component.HasBuff(RoR2Content.Buffs.AffixBlue) ? 1 : 0) > 0)
                                {
                                    float num4 = 0.5f;
                                    float num5 = Util.OnHitProcDamage(damageInfo.damage, component.damage, num4);
                                    float num6 = 0f;
                                    Vector3 position = damageInfo.position;
                                    ProjectileManager.instance.FireProjectile(LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LightningStake"), position, Quaternion.identity, damageInfo.attacker, num5, num6, damageInfo.crit, DamageColorIndex.Item, null, -1f);
                                }
                            }
                        }
                    }
                }
            }

            orig(self, damageInfo, hitObject);
        }

        private void ClingyDronesAndInvincibleUmbrae(CharacterMaster master)
        {
            if (ArtifactEnabled)
            {
                switch (master.name)
                {
                    case "Drone1Master(Clone)":
                        AISkillDriver idle = (from x in master.GetComponents<AISkillDriver>()
                                              where x.customName == "IdleNearLeaderWhenNoEnemies"
                                              select x).First();
                        idle.maxDistance = Mathf.Infinity;
                        idle.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver softLeash = (from x in master.GetComponents<AISkillDriver>()
                                                   where x.customName == "SoftLeashToLeader"
                                                   select x).First();
                        softLeash.minDistance = 0f;

                        AISkillDriver hardLeash = (from x in master.GetComponents<AISkillDriver>()
                                                   where x.customName == "HardLeashToLeader"
                                                   select x).First();
                        hardLeash.minDistance = 0f;
                        break;

                    case "Drone2Master(Clone)":
                        AISkillDriver hardLeash2 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "HardLeashToLeader"
                                                    select x).First();
                        hardLeash2.minDistance = 0f;
                        break;

                    case "EmergencyDroneMaster(Clone)":
                        AISkillDriver idle2 = (from x in master.GetComponents<AISkillDriver>()
                                               where x.customName == "IdleNearLeaderWhenNoEnemies"
                                               select x).First();
                        idle2.maxDistance = Mathf.Infinity;
                        idle2.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver hardLeash3 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "HardLeashToLeader"
                                                    select x).First();
                        hardLeash3.minDistance = 0f;
                        break;

                    case "EquipmentDroneMaster(Clone)":
                        AISkillDriver idle3 = (from x in master.GetComponents<AISkillDriver>()
                                               where x.customName == "IdleNearLeaderWhenNoEnemies"
                                               select x).First();
                        idle3.maxDistance = Mathf.Infinity;
                        idle3.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver softLeash2 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "SoftLeashToLeader"
                                                    select x).First();
                        softLeash2.minDistance = 0f;

                        AISkillDriver hardLeash4 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "HardLeashToLeader"
                                                    select x).First();
                        hardLeash4.minDistance = 0f;
                        break;

                    case "FlameDroneMaster(Clone)":
                        AISkillDriver idle4 = (from x in master.GetComponents<AISkillDriver>()
                                               where x.customName == "IdleNearLeaderWhenNoEnemies"
                                               select x).First();
                        idle4.maxDistance = Mathf.Infinity;
                        idle4.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver hardLeash5 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "HardLeashToLeader"
                                                    select x).First();
                        hardLeash5.minDistance = 0f;
                        break;

                    case "MegaDroneMaster(Clone)":
                        AISkillDriver idle5 = (from x in master.GetComponents<AISkillDriver>()
                                               where x.customName == "IdleNearLeaderWhenNoEnemies"
                                               select x).First();
                        idle5.maxDistance = Mathf.Infinity;
                        idle5.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver hardLeash6 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "LeashLeaderHard"
                                                    select x).First();
                        hardLeash6.minDistance = 0f;
                        break;

                    case "DroneMissileMaster(Clone)":
                        AISkillDriver idle6 = (from x in master.GetComponents<AISkillDriver>()
                                               where x.customName == "IdleNearLeaderWhenNoEnemies"
                                               select x).First();
                        idle6.maxDistance = Mathf.Infinity;
                        idle6.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver softLeash3 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "SoftLeashToLeader"
                                                    select x).First();
                        softLeash3.minDistance = 0f;

                        AISkillDriver hardLeash7 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "HardLeashToLeader"
                                                    select x).First();
                        hardLeash7.minDistance = 0f;
                        break;

                    case "DroneBackupMaster(Clone)":
                        AISkillDriver idle7 = (from x in master.GetComponents<AISkillDriver>()
                                               where x.customName == "IdleNearLeaderWhenNoEnemies"
                                               select x).First();
                        idle7.maxDistance = Mathf.Infinity;
                        idle7.movementType = AISkillDriver.MovementType.ChaseMoveTarget;

                        AISkillDriver softLeash4 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "SoftLeashToLeader"
                                                    select x).First();
                        softLeash4.minDistance = 0f;

                        AISkillDriver hardLeash8 = (from x in master.GetComponents<AISkillDriver>()
                                                    where x.customName == "HardLeashToLeader"
                                                    select x).First();
                        hardLeash8.minDistance = 0f;
                        break;
                }
                var body = master.GetBody();
                if (body)
                {
                    bool isUmbra = master.teamIndex == TeamIndex.Monster && body.name.Contains("MonsterMaster");
                    if (isUmbra)
                    {
                        body.healthComponent.godMode = true;
                    }
                }
                // invincible umbrae
            }
        }

        private void RemoveDamageNumbersLateGame(On.RoR2.DamageDisplay.orig_DoUpdate orig, DamageDisplay self)
        {
            if (ArtifactEnabled && Run.instance.stageClearCount >= 11)
            {
                self.maxLife = 0f;
            }
            orig(self);
        }

        private void HighSpeedLeap(On.EntityStates.Croco.BaseLeap.orig_OnEnter orig, BaseLeap self)
        {
            if (ArtifactEnabled)
            {
                BaseLeap.upwardVelocity = 21f;
                BaseLeap.forwardVelocity = 9f;
            }
            orig(self);
        }

        private void CommencementLunarWispSpam(SceneDirector sceneDirector)
        {
            if (ArtifactEnabled && SceneManager.GetActiveScene().name == "moon2")
            {
                sceneDirector.monsterCredit *= 50;
                sceneDirector.spawnDistanceMultiplier *= 0.1f;
                sceneDirector.eliteBias *= 100f;
            }
        }

        private void RemoveOSP(CharacterBody body)
        {
            if (ArtifactEnabled)
                body.oneShotProtectionFraction = 0f;
        }

        private void RemoveM2Healing(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(0.5f)))
            {
                c.Index += 1;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 0f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Acrid M2 Regenerative hook");
            }
        }

        private void RemoveM1Healing(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(0.5f)))
            {
                c.Index += 1;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 0f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Acrid M1 Regenerative hook");
            }
        }

        private void IncreaseFireTrailDamage(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(1.5f)))
            {
                c.Index += 1;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 6f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Blazing Elite Fire Trail Damage hook");
            }
        }

        private void IncreasedKnockback(On.RoR2.HealthComponent.orig_TakeDamageForce_DamageInfo_bool_bool orig, HealthComponent self, DamageInfo damageInfo, bool alwaysApply, bool disableAirControlUntilCollision)
        {
            if (ArtifactEnabled)
                damageInfo.force *= 3f;
            orig(self, damageInfo, alwaysApply, disableAirControlUntilCollision);
        }

        private void IncreasedKnockback2(On.RoR2.HealthComponent.orig_TakeDamageForce_Vector3_bool_bool orig, HealthComponent self, Vector3 force, bool alwaysApply, bool disableAirControlUntilCollision)
        {
            if (ArtifactEnabled)
                force *= 3f;
            orig(self, force, alwaysApply, disableAirControlUntilCollision);
        }

        private void RandomCrashFpsDropsFPSLimit()
        {
            if (ArtifactEnabled)
            {
                if (Random.RandomRangeInt(0, 10000000) < 1)
                {
                    UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.AccessViolation);
                    // random crash at any time
                }
                Application.targetFrameRate = 60;
                if (Run.instance.stageRng.RangeInt(0, 1000) < 3)
                {
                    Application.targetFrameRate = 10;
                }
            }
            else
            {
                Application.targetFrameRate = int.MaxValue;
            }
        }

        private void RandomStageStuff(Stage stage)
        {
            if (ArtifactEnabled)
            {
                if (Run.instance.stageRng.RangeInt(0, 100) < 1)
                {
                    UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.FatalError);
                    // random crash on stage start
                }

                if (Run.instance.stageRng.RangeInt(0, 100) < 3)
                {
                    On.RoR2.DamageInfo.ModifyDamageInfo += RandomNoDamageForStage;
                }
                else
                {
                    On.RoR2.DamageInfo.ModifyDamageInfo -= RandomNoDamageForStage;
                }

                if (Run.instance.stageRng.RangeInt(0, 100) < 2)
                {
                    On.RoR2.DamageInfo.ModifyDamageInfo += RandomNoProcsForStage;
                }
                else
                {
                    On.RoR2.DamageInfo.ModifyDamageInfo -= RandomNoProcsForStage;
                }

                if (Run.instance.stageRng.RangeInt(0, 100) < 4)
                {
                    On.RoR2.DamageInfo.ModifyDamageInfo += RandomInfiniteChainsForStage;
                }
            }
        }

        private void RandomInfiniteChainsForStage(On.RoR2.DamageInfo.orig_ModifyDamageInfo orig, DamageInfo self, HurtBox.DamageModifier damageModifier)
        {
            self.procChainMask = default;
            self.procCoefficient = 100f;
            orig(self, damageModifier);
        }

        private void RandomNoProcsForStage(On.RoR2.DamageInfo.orig_ModifyDamageInfo orig, DamageInfo self, HurtBox.DamageModifier damageModifier)
        {
            self.procCoefficient = 0f;
            orig(self, damageModifier);
        }

        private void RandomNoDamageForStage(On.RoR2.DamageInfo.orig_ModifyDamageInfo orig, DamageInfo self, HurtBox.DamageModifier damageModifier)
        {
            self.damage = 0;
            orig(self, damageModifier);
        }

        private void RandomNoDamageOnHit(On.RoR2.DamageInfo.orig_ModifyDamageInfo orig, DamageInfo self, HurtBox.DamageModifier damageModifier)
        {
            if (Run.instance.stageRng.RangeInt(0, 100) < 2)
            {
                self.damage = 0;
            }
            if (Run.instance.stageRng.RangeInt(0, 100) < 3)
            {
                self.procCoefficient = 0;
            }
            orig(self, damageModifier);
        }

        private void RemoveAegisAndRejuvRack(ILContext il)
        {
            ILCursor c = new(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(1f)))
            {
                c.Index += 1;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 0f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Rejuvenation Rack Healing hook");
            }

            c.Index = 0;

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "barrierOnOverHeal"),
                    x => x.MatchConvR4(),
                    x => x.MatchLdcR4(0.5f)))
            {
                c.Index += 3;
                c.EmitDelegate<Func<float, float>>((self) =>
                {
                    return ArtifactEnabled ? 0f : self;
                });
            }
            else
            {
                Main.ACLogger.LogError("Failed to apply Aegis Overheal hook");
            }
        }

        private void Changes()
        {
            if (ArtifactEnabled)
            {
                var brassContraption = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bell/BellBody.png").WaitForCompletion();
                var mdlBell = brassContraption.transform.GetChild(0).GetChild(0);
                var hurtBoxGroup = mdlBell.GetComponent<HurtBoxGroup>();
                for (int i = 0; i < hurtBoxGroup.hurtBoxes.Length; i++)
                {
                    var boxCollider = hurtBoxGroup.hurtBoxes[i].GetComponent<BoxCollider>();
                    if (boxCollider)
                    {
                        boxCollider.size *= 0.6f;
                    }
                    var sphereCollider = hurtBoxGroup.hurtBoxes[i].GetComponent<SphereCollider>();
                    if (sphereCollider)
                    {
                        sphereCollider.radius *= 0.6f;
                    }
                }
                // smaller hitbox
            }
        }
    }

    public class StupidBandController : MonoBehaviour, IOnTakeDamageServerReceiver
    {
        public void OnTakeDamageServer(DamageReport damageReport)
        {
            var damageInfo = damageReport.damageInfo;
            var attacker = damageInfo.attacker;
            if (attacker)
            {
                var attackerBody = attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    var inventory = attackerBody.inventory;

                    if (!damageInfo.procChainMask.HasProc(ProcType.Rings) && damageReport.damageDealt / attackerBody.damage >= 4f)
                    {
                        var aimOrigin = attackerBody.aimOrigin;

                        if (attackerBody.HasBuff(RoR2Content.Buffs.ElementalRingsReady) && inventory)
                        {
                            int itemCount9 = inventory.GetItemCount(RoR2Content.Items.IceRing);
                            int itemCount10 = inventory.GetItemCount(RoR2Content.Items.FireRing);
                            attackerBody.RemoveBuff(RoR2Content.Buffs.ElementalRingsReady);
                            int num29 = 1;
                            while (num29 <= 10f)
                            {
                                attackerBody.AddTimedBuff(RoR2Content.Buffs.ElementalRingsCooldown, (float)num29);
                                num29++;
                            }
                            ProcChainMask procChainMask5 = damageInfo.procChainMask;
                            procChainMask5.AddProc(ProcType.Rings);
                            Vector3 position2 = damageInfo.position;
                            if (itemCount9 > 0 && attackerBody)
                            {
                                float num30 = 2.5f * itemCount9;
                                float num31 = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, num30);
                                DamageInfo damageInfo2 = new()
                                {
                                    damage = num31,
                                    damageColorIndex = DamageColorIndex.Item,
                                    damageType = DamageType.Generic,
                                    attacker = damageInfo.attacker,
                                    crit = damageInfo.crit,
                                    force = Vector3.zero,
                                    inflictor = null,
                                    position = position2,
                                    procChainMask = procChainMask5,
                                    procCoefficient = 1f
                                };
                                EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/IceRingExplosion"), position2, Vector3.up, true);
                                attackerBody.AddTimedBuff(RoR2Content.Buffs.Slow80, 3f * (float)itemCount9);
                                damageReport.victim.TakeDamage(damageInfo2);
                            }
                            if (itemCount10 > 0)
                            {
                                GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireTornado");
                                float resetInterval = gameObject.GetComponent<ProjectileOverlapAttack>().resetInterval;
                                float lifetime = gameObject.GetComponent<ProjectileSimple>().lifetime;
                                float num32 = 3f * (float)itemCount10;
                                float num33 = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, num32) / lifetime * resetInterval;
                                float num34 = 0f;
                                Quaternion quaternion2 = Quaternion.identity;
                                Vector3 vector2 = position2 - aimOrigin;
                                vector2.y = 0f;
                                if (vector2 != Vector3.zero)
                                {
                                    num34 = -1f;
                                    quaternion2 = Util.QuaternionSafeLookRotation(vector2, Vector3.up);
                                }
                                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                                {
                                    damage = num33,
                                    crit = damageInfo.crit,
                                    damageColorIndex = DamageColorIndex.Item,
                                    position = position2,
                                    procChainMask = procChainMask5,
                                    force = 0f,
                                    owner = damageInfo.attacker,
                                    projectilePrefab = gameObject,
                                    rotation = quaternion2,
                                    speedOverride = num34,
                                    target = null
                                });
                            }
                        }
                        else if (attackerBody.HasBuff(DLC1Content.Buffs.ElementalRingVoidReady))
                        {
                            int itemCount11 = inventory.GetItemCount(DLC1Content.Items.ElementalRingVoid);
                            attackerBody.RemoveBuff(DLC1Content.Buffs.ElementalRingVoidReady);
                            int num35 = 1;
                            while ((float)num35 <= 20f)
                            {
                                attackerBody.AddTimedBuff(DLC1Content.Buffs.ElementalRingVoidCooldown, (float)num35);
                                num35++;
                            }
                            ProcChainMask procChainMask6 = damageInfo.procChainMask;
                            procChainMask6.AddProc(ProcType.Rings);
                            if (itemCount11 > 0)
                            {
                                GameObject gameObject2 = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ElementalRingVoidBlackHole");
                                float num36 = 1f * (float)itemCount11;
                                float num37 = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, num36);
                                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                                {
                                    damage = num37,
                                    crit = damageInfo.crit,
                                    damageColorIndex = DamageColorIndex.Void,
                                    position = damageInfo.position,
                                    procChainMask = procChainMask6,
                                    force = 6000f,
                                    owner = damageInfo.attacker,
                                    projectilePrefab = gameObject2,
                                    rotation = Quaternion.identity,
                                    target = null
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    public class OldCleansingPoolDropTable : PickupDropTable
    {
        public WeightedSelection<PickupIndex> weighted = new();

        public override int GetPickupCount()
        {
            return weighted.Count;
        }

        public void GenerateWeightedSelection()
        {
            weighted.Clear();
            weighted.AddChoice(PickupCatalog.FindPickupIndex(RoR2Content.Items.Pearl.itemIndex), 96f);
            weighted.AddChoice(PickupCatalog.FindPickupIndex(RoR2Content.Items.ShinyPearl.itemIndex), 4f);
        }

        public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
        {
            GenerateWeightedSelection();
            return GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, weighted);
        }

        public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
        {
            GenerateWeightedSelection();
            Debug.Log(GenerateDropFromWeightedSelection(rng, weighted));
            return GenerateDropFromWeightedSelection(rng, weighted);
        }
    }
}