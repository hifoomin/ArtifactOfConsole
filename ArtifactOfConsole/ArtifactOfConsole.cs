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

namespace ArtifactOfConsole.Artifact
{
    internal class ArtifactOfConsole : ArtifactBase<ArtifactOfConsole>
    {
        public Texture2D tempEnabled = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/LunarGolem/texBuffLunarShellIcon.tif").WaitForCompletion();
        public Texture2D tempDisabled = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Nullifier/texBuffNullifiedIcon.tif").WaitForCompletion();
        public override string ArtifactName => "Artifact of Console";

        public override string ArtifactLangTokenName => "HIFU_ArtifactOfConsole";

        public override string ArtifactDescription => "Experience a bunch of Risk of Rain 2 Console Edition bugs.";

        public override Sprite ArtifactEnabledIcon => Sprite.Create(tempEnabled, new Rect(0f, 0f, (float)tempEnabled.width, (float)tempEnabled.height), new Vector2(0f, 0f));

        public override Sprite ArtifactDisabledIcon => Sprite.Create(tempDisabled, new Rect(0f, 0f, (float)tempDisabled.width, (float)tempDisabled.height), new Vector2(0f, 0f));

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateArtifact();
            Hooks();
        }

        public override void Hooks()
        {
            On.RoR2.DamageInfo.ModifyDamageInfo += RandomNoDamageOnHit;
            IL.RoR2.HealthComponent.Heal += RemoveAegisAndRejuvRack;
            Stage.onServerStageBegin += RandomStageStuff;
            // RoR2Application.onFixedUpdate += RandomCrashRandomUpdateRate;
            On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += IncreasedKnockback;
            On.RoR2.HealthComponent.TakeDamageForce_Vector3_bool_bool += IncreasedKnockback2;
            IL.RoR2.CharacterBody.UpdateFireTrail += IncreaseFireTrailDamage;
            IL.EntityStates.Croco.Slash.OnMeleeHitAuthority += RemoveM1Healing;
            IL.EntityStates.Croco.Bite.OnMeleeHitAuthority += RemoveM2Healing;
            On.EntityStates.Croco.BaseLeap.OnEnter += HighSpeedLeap;
            CharacterBody.onBodyStartGlobal += RemoveOSP;
            On.RoR2.DamageDisplay.DoUpdate += RemoveDamageNumbersLateGame;
            SceneDirector.onPrePopulateMonstersSceneServer += CommencementLunarWispSpam;
            On.RoR2.CharacterBody.FixedUpdate += RandomFpsDrops;
            CharacterMaster.onStartGlobal += ClingyDrones;
            On.RoR2.GlobalEventManager.OnHitAll += OverloadingTwoOrbs;
            IL.RoR2.GlobalEventManager.OnHitEnemy += RemoveBandsOnHit;
            On.RoR2.HealthComponent.TakeDamage += AddBands;
            Changes();
        }

        private void AddBands(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            if (ArtifactEnabled)
            {
                var attacker = damageInfo.attacker;
                if (attacker)
                {
                    // attackerBody is me
                    // self is enemy

                    var attackerBody = attacker.GetComponent<CharacterBody>();
                    if (attackerBody)
                    {
                        var inventory = attackerBody.inventory;
                        if (!damageInfo.procChainMask.HasProc(ProcType.Rings) && damageInfo.damage / attackerBody.damage >= 4f)
                        {
                            Main.ACLogger.LogError("passed damage threshold check");
                            var aimOrigin = attackerBody.aimOrigin;

                            if (attackerBody.HasBuff(RoR2Content.Buffs.ElementalRingsReady) && inventory)
                            {
                                Main.ACLogger.LogError("body has elementalrings buff ready");
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
                                    Main.ACLogger.LogError("attackerBody has runald's band");
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
                                    self.TakeDamage(damageInfo2);
                                }
                                if (itemCount10 > 0)
                                {
                                    Main.ACLogger.LogError("attackerBody exists and self.body.inventory has KJARO band");
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
                            else if (self.body.HasBuff(DLC1Content.Buffs.ElementalRingVoidReady))
                            {
                                int itemCount11 = inventory.GetItemCount(DLC1Content.Items.ElementalRingVoid);
                                self.body.RemoveBuff(DLC1Content.Buffs.ElementalRingVoidReady);
                                int num35 = 1;
                                while ((float)num35 <= 20f)
                                {
                                    self.body.AddTimedBuff(DLC1Content.Buffs.ElementalRingVoidCooldown, (float)num35);
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

        private void RemoveBandsOnHit(ILContext il)
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

        private void ClingyDrones(CharacterMaster master)
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
                BaseLeap.upwardVelocity = 14f;
                BaseLeap.forwardVelocity = 6f;
            }
            else
            {
                orig(self);
            }
        }

        private void RandomFpsDrops(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            if (ArtifactEnabled)
            {
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
            orig(self);
        }

        private void CommencementLunarWispSpam(SceneDirector sceneDirector)
        {
            if (ArtifactEnabled && SceneManager.GetActiveScene().name == "moon2")
            {
                sceneDirector.monsterCredit *= 7;
                sceneDirector.spawnDistanceMultiplier *= 0.1f;
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

        private void RandomCrashRandomUpdateRate()
        {
            if (ArtifactEnabled)
            {
                if (Random.RandomRangeInt(0, 10000000) < 1)
                {
                    UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.AccessViolation);
                    // random crash at any time
                }

                Time.fixedDeltaTime = 1 / Run.instance.spawnRng.RangeFloat(30, 70);
                // lower non-input, non-render fps to random lmaoo
            }
            if (Time.fixedDeltaTime != 1 / 60 && ArtifactEnabled == false)
            {
                Time.fixedDeltaTime = 1 / 60;
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

                var lunarWisp = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/LunarWisp/cscLunarWisp.asset").WaitForCompletion();
                lunarWisp.eliteRules = SpawnCard.EliteRules.Default;
                lunarWisp.directorCreditCost = 80;

                var lunarExploder = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/LunarExploder/cscLunarExploder.asset").WaitForCompletion();
                lunarExploder.eliteRules = SpawnCard.EliteRules.Default;
                lunarExploder.directorCreditCost = 200;

                var lunarGolem = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/LunarGolem/cscLunarGolem.asset").WaitForCompletion();
                lunarGolem.eliteRules = SpawnCard.EliteRules.Default;
                // everything but perfected, more wisp spawns like console
            }
        }
    }
}