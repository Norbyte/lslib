using LSLib.LS.Enums;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace LSLib.LS.Stats
{
    public class StatEnumeration
    {
        public readonly string Name;
        public readonly List<string> Values;
        public readonly Dictionary<string, int> ValueToIndexMap;

        public StatEnumeration(string name)
        {
            Name = name;
            Values = new List<string>();
            ValueToIndexMap = new Dictionary<string, int>();
        }

        public void AddItem(int index, string value)
        {
            if (Values.Count != index)
            {
                throw new Exception("Enumeration items must be added in order.");
            }

            Values.Add(value);

            // Some vanilla enums are bogus and contain names multiple times
            if (!ValueToIndexMap.ContainsKey(value))
            {
                ValueToIndexMap.Add(value, index);
            }
        }

        public void AddItem(string value)
        {
            AddItem(Values.Count, value);
        }
    }

    public class StatField
    {
        public string Name;
        public string Type;
        public StatEnumeration EnumType;
        public List<StatReferenceConstraint> ReferenceTypes;

        private IStatValueParser parser;

        public IStatValueParser GetParser(StatValueParserFactory factory, StatDefinitionRepository definitions)
        {
            if (parser == null)
            {
                parser = factory.CreateParser(this, definitions);
            }

            return parser;
        }
    }

    public class StatEntryType
    {
        public readonly string Name;
        public readonly string NameProperty;
        public readonly string BasedOnProperty;
        public readonly Dictionary<string, StatField> Fields;

        public StatEntryType(string name, string nameProperty, string basedOnProperty)
        {
            Name = name;
            NameProperty = nameProperty;
            BasedOnProperty = basedOnProperty;
            Fields = new Dictionary<string, StatField>();
        }
    }

    public class StatFunctorArgumentType
    {
        public string Name;
        public string Type;
    }

    public class StatFunctorType
    {
        public string Name;
        public int RequiredArgs;
        public List<StatFunctorArgumentType> Args;
    }

    public class StatDefinitionRepository
    {
        // Version of modified Enumerations.xml and StatObjectDefinitions.sod we expect
        public const string CustomizationsVersion = "1";

        public readonly Dictionary<string, StatEnumeration> Enumerations = new Dictionary<string, StatEnumeration>();
        public readonly Dictionary<string, StatEntryType> Types = new Dictionary<string, StatEntryType>();
        public readonly Dictionary<string, StatFunctorType> Functors = new Dictionary<string, StatFunctorType>();
        public readonly Dictionary<string, StatFunctorType> Boosts = new Dictionary<string, StatFunctorType>();
        public readonly Dictionary<string, StatFunctorType> DescriptionParams = new Dictionary<string, StatFunctorType>();

        private StatField AddField(StatEntryType defn, string name, string typeName)
        {
            var field = new StatField
            {
                Name = name,
                Type = typeName
            };

            if (Enumerations.TryGetValue(typeName, out StatEnumeration enumType) && enumType.Values.Count > 0)
            {
                field.EnumType = enumType;
            }

            defn.Fields.Add(name, field);
            return field;
        }

        private void AddEnumeration(string name, List<string> labels)
        {
            var enumType = new StatEnumeration(name);
            foreach (var label in labels)
            {
                enumType.AddItem(label);
            }
            Enumerations.Add(name, enumType);
        }

        private StatFunctorArgumentType MakeFunctorArg(string name, string type)
        {
            return new StatFunctorArgumentType
            {
                Name = name,
                Type = type
            };
        }

        public void AddBoost(string name, int requiredArgs, List<string> args)
        {
            AddFunctor(Boosts, name, requiredArgs, args);
        }

        public void AddFunctor(string name, int requiredArgs, List<string> args)
        {
            AddFunctor(Functors, name, requiredArgs, args);
        }

        public void AddDescriptionParams(string name, int requiredArgs, List<string> args)
        {
            AddFunctor(DescriptionParams, name, requiredArgs, args);
        }

        public void AddFunctor(Dictionary<string, StatFunctorType> dict, string name, int requiredArgs, List<string> argDescs)
        {
            var args = new List<StatFunctorArgumentType>();
            for (int i = 0; i < argDescs.Count; i += 2)
            {
                args.Add(MakeFunctorArg(argDescs[i], argDescs[i + 1]));
            }

            AddFunctor(dict, name, requiredArgs, args);
        }

        public void AddFunctor(Dictionary<string, StatFunctorType> dict, string name, int requiredArgs, IEnumerable<StatFunctorArgumentType> args)
        {
            var functor = new StatFunctorType
            {
                Name = name,
                RequiredArgs = requiredArgs,
                Args = args.ToList()
            };

            dict.Add(name, functor);
        }

        public void LoadDefinitions(Stream stream)
        {
            StatEntryType defn = null;
            string line;

            using (var reader = new StreamReader(stream))
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    if (trimmed.StartsWith("modifier type "))
                    {
                        var name = trimmed.Substring(15, trimmed.Length - 16);
                        defn = new StatEntryType(name, "Name", "Using");
                        Types.Add(defn.Name, defn);
                        AddField(defn, "Name", "FixedString");
                        var usingRef = AddField(defn, "Using", "StatReference");
                        usingRef.ReferenceTypes = new List<StatReferenceConstraint>
                        {
                            new StatReferenceConstraint
                            {
                                StatType = name
                            }
                        };
                    }
                    else if (trimmed.StartsWith("modifier \""))
                    {
                        var nameEnd = trimmed.IndexOf('"', 10);
                        var name = trimmed.Substring(10, nameEnd - 10);
                        var typeName = trimmed.Substring(nameEnd + 3, trimmed.Length - nameEnd - 4);
                        AddField(defn, name, typeName);
                    }
                }
            }

            // Add builtins
            var itemColor = new StatEntryType("ItemColor", "ItemColorName", null);
            Types.Add(itemColor.Name, itemColor);
            AddField(itemColor, "ItemColorName", "FixedString");
            AddField(itemColor, "Primary Color", "FixedString");
            AddField(itemColor, "Secondary Color", "FixedString");
            AddField(itemColor, "Tertiary Color", "FixedString");

            var itemProgressionName = new StatEntryType("ItemProgressionNames", "Name", null);
            Types.Add(itemProgressionName.Name, itemProgressionName);
            AddField(itemProgressionName, "Name", "FixedString");
            AddField(itemProgressionName, "Names", "Passthrough");

            var itemProgressionVisual = new StatEntryType("ItemProgressionVisuals", "Name", null);
            Types.Add(itemProgressionVisual.Name, itemProgressionVisual);
            AddField(itemProgressionVisual, "Name", "FixedString");
            // FIXME
            AddField(itemProgressionVisual, "LevelGroups", "Passthrough");
            AddField(itemProgressionVisual, "NameGroups", "Passthrough");
            AddField(itemProgressionVisual, "RootGroups", "Passthrough");

            var dataType = new StatEntryType("Data", "Key", null);
            Types.Add(dataType.Name, dataType);
            AddField(dataType, "Key", "FixedString");
            AddField(dataType, "Value", "FixedString");

            AddEnumeration("ResurrectType", new List<string>
            {
                "Living",
                "Guaranteed",
                "Construct",
                "Undead"
            });

            AddEnumeration("SetStatusDurationType", new List<string>
            {
                "SetMinimum",
                "ForceSet",
                "Add",
                "Multiply"
            });

            AddEnumeration("ExecuteWeaponFunctorsType", new List<string>
            {
                "MainHand",
                "OffHand",
                "BothHands"
            });

            AddEnumeration("SpellCooldownType", new List<string>
            {
                "Default",
                "OncePerTurn",
                "OncePerCombat",
                "UntilRest",
                "OncePerTurnNoRealtime",
                "UntilShortRest",
                "UntilPerRestPerItem",
                "OncePerShortRestPerItem"
            });

            AddEnumeration("SummonDuration", new List<string>
            {
                "UntilLongRest",
                "Permanent"
            });

            AddEnumeration("ForceFunctorOrigin", new List<string>
            {
                "OriginToEntity",
                "OriginToTarget",
                "TargetToEntity"
            });

            AddEnumeration("ForceFunctorAggression", new List<string>
            {
                "Aggressive",
                "Friendly",
                "Neutral"
            });

            AddEnumeration("StatItemSlot", new List<string>
            {
                "Helmet",
                "Breast",
                "Cloak",
                "MeleeMainHand",
                "MeleeOffHand",
                "RangedMainHand",
                "RangedOffHand",
                "Ring",
                "Underwear",
                "Boots",
                "Gloves",
                "Amulet",
                "Ring2",
                "Wings",
                "Horns",
                "Overhead",
                "MusicalInstrument",
                "VanityBody",
                "VanityBoots",
                "MainHand",
                "OffHand"
            });

            AddEnumeration("Magical", new List<string>
            {
                "Magical",
                "Nonmagical"
            });

            AddEnumeration("Nonlethal", new List<string>
            {
                "Lethal",
                "Nonlethal"
            });

            AddEnumeration("AllEnum", new List<string>
            {
                "All"
            });

            AddEnumeration("ZoneShape", new List<string>
            {
                "Cone",
                "Square",
            });

            AddEnumeration("SurfaceLayer", new List<string>
            {
                "Ground",
                "Cloud",
            });

            AddEnumeration("RollAdjustmentType", new List<string>
            {
                "All",
                "Distribute",
            });

            AddEnumeration("StatsRollType", new List<string>
            {
                "Attack",
                "MeleeWeaponAttack",
                "RangedWeaponAttack",
                "MeleeSpellAttack",
                "RangedSpellAttack",
                "MeleeUnarmedAttack",
                "RangedUnarmedAttack",
                "SkillCheck",
                "SavingThrow",
                "RawAbility",
                "Damage",
                "MeleeOffHandWeaponAttack",
                "RangedOffHandWeaponAttack",
                "DeathSavingThrow",
                "MeleeWeaponDamage",
                "RangedWeaponDamage",
                "MeleeSpellDamage",
                "RangedSpellDamage",
                "MeleeUnarmedDamage",
                "RangedUnarmedDamage",
            });

            AddEnumeration("AdvantageType", new List<string>
            {
                "AttackRoll",
                "AttackTarget",
                "SavingThrow",
                "AllSavingThrows",
                "Ability",
                "AllAbilities",
                "Skill",
                "AllSkills",
                "SourceDialogue",
                "DeathSavingThrow",
                "Concentration",
            });

            AddEnumeration("SkillType", new List<string>
            {
                "Deception",
                "Intimidation",
                "Performance",
                "Persuasion",
                "Acrobatics",
                "SleightOfHand",
                "Stealth",
                "Arcana",
                "History",
                "Investigation",
                "Nature",
                "Religion",
                "Athletics",
                "AnimalHandling",
                "Insight",
                "Medicine",
                "Perception",
                "Survival",
            });

            AddEnumeration("CriticalHitType", new List<string>
            {
                "AttackTarget",
                "AttackRoll"
            });

            AddEnumeration("Result", new List<string>
            {
                "Success",
                "Failure"
            });

            AddEnumeration("CriticalHitResult", new List<string>
            {
                "Success",
                "Failure"
            });

            AddEnumeration("CriticalHitWhen", new List<string>
            {
                "Never",
                "Always",
                "ForcedAlways"
            });

            AddEnumeration("MovementSpeedType", new List<string>
            {
                "Stroll",
                "Walk",
                "Run",
                "Sprint",
            });

            AddEnumeration("DamageReductionType", new List<string>
            {
                "Half",
                "Flat",
                "Threshold"
            });

            AddEnumeration("AttackRollAbility", new List<string>
            {
                "SpellCastingAbility",
                "UnarmedMeleeAbility",
                "AttackAbility"
            });

            AddEnumeration("HealingDirection", new List<string>
            {
                "Incoming",
                "Outgoing"
            });

            AddEnumeration("ResistanceBoostFlags", new List<string>
            {
                "None",
                "Resistant",
                "Immune",
                "Vulnerable",
                "BelowDamageThreshold",
                "ResistantToMagical",
                "ImmuneToMagical",
                "VulnerableToMagical",
                "ResistantToNonMagical",
                "ImmuneToNonMagical",
                "VulnerableToNonMagical",
            });

            AddEnumeration("UnlockSpellType", new List<string>
            {
                "Singular", 
                "AddChildren", 
                "MostPowerful"
            });

            AddEnumeration("ProficiencyBonusBoostType", new List<string>
            {
                "AttackRoll",
                "AttackTarget",
                "SavingThrow",
                "AllSavingThrows",
                "Ability",
                "AllAbilities",
                "Skill",
                "AllSkills",
                "SourceDialogue",
                "WeaponActionDC"
            });

            AddEnumeration("ResourceReplenishType", new List<string>
            {
                "Never",
                "Default",
                "Combat",
                "Rest",
                "ShortRest",
                "FullRest",
                "ExhaustedRest"
            });

            AddEnumeration("AttackType", new List<string>
            {
                "DirectHit",
                "MeleeWeaponAttack",
                "RangedWeaponAttack",
                "MeleeOffHandWeaponAttack",
                "RangedOffHandWeaponAttack",
                "MeleeSpellAttack",
                "RangedSpellAttack",
                "MeleeUnarmedAttack",
                "RangedUnarmedAttack"
            });

            AddEnumeration("DealDamageWeaponDamageType", new List<string>
            {
                "MainWeaponDamageType",
                "OffhandWeaponDamageType",
                "MainMeleeWeaponDamageType",
                "OffhandMeleeWeaponDamageType",
                "MainRangedWeaponDamageType",
                "OffhandRangedWeaponDamageType",
                "SourceWeaponDamageType",
                "ThrownWeaponDamageType",
            });

            AddEnumeration("EngineStatusType", new List<string>
            {
                "DYING",
                "HEAL",
                "KNOCKED_DOWN",
                "TELEPORT_FALLING",
                "BOOST",
                "REACTION",
                "STORY_FROZEN",
                "SNEAKING",
                "UNLOCK",
                "FEAR",
                "SMELLY",
                "INVISIBLE",
                "ROTATE",
                "MATERIAL",
                "CLIMBING",
                "INCAPACITATED",
                "INSURFACE",
                "POLYMORPHED",
                "EFFECT",
                "DEACTIVATED",
                "DOWNED",
            });


            // Add functors
            AddFunctor("ApplyStatus", 1, new List<string> {
                "StatusId", "StatusId",
                "Chance", "Int",
                "Duration", "Lua",
                "StatusSpecificParam1", "String",
                "StatusSpecificParam2", "Int",
                "StatusSpecificParam3", "Int",
                "StatsConditions", "Conditions",
                "RequiresConcentration", "Boolean"
            });
            AddFunctor("SurfaceChange", 1, new List<string> {
                "SurfaceChange", "Surface Change",
                "Chance", "Float",
                "Arg3", "Float",
                "Arg4", "Float",
                "Arg5", "Float"
            });
            AddFunctor("Resurrect", 0, new List<string> {
                "Chance", "Float",
                "HealthPercentage", "Float",
                "Type", "ResurrectType"
            });
            AddFunctor("Sabotage", 0, new List<string> {
                "Amount", "Int"
            });
            AddFunctor("Summon", 1, new List<string> {
                "Template", "Guid", // Root template GUID
                "Duration", "SummonDurationOrInt",
                "AIHelper", "SpellId",
                "Arg4", "Boolean",
                "StackId", "String",
                "StatusToApply1", "StatusId",
                "StatusToApply2", "StatusId",
                "StatusToApply3", "StatusId",
                "StatusToApply4", "StatusId",
                "Arg10", "Boolean",
            });
            AddFunctor("Force", 1, new List<string> {
                "Distance", "Lua",
                "Origin", "ForceFunctorOrigin",
                "Aggression", "ForceFunctorAggression",
                "Arg4", "Boolean",
                "Arg5", "Boolean",
            });
            AddFunctor("Douse", 0, new List<string> {
                "Arg1", "Float",
                "Arg2", "Float"
            });
            AddFunctor("SwapPlaces", 0, new List<string> {
                "Animation", "String",
                "Arg2", "Boolean",
                "Arg3", "Boolean"
            });
            AddFunctor("Pickup", 0, new List<string> {
                "Arg1", "String"
            });
            AddFunctor("CreateSurface", 3, new List<string> {
                "Radius", "Float",
                "Duration", "Float",
                "SurfaceType", "Surface Type",
                "IsControlledByConcentration", "Boolean",
                "Arg5", "Float",
                "Arg6", "Boolean"
            });
            AddFunctor("CreateConeSurface", 3, new List<string> {
                "Radius", "Float",
                "Duration", "Float",
                "SurfaceType", "Surface Type",
                "IsControlledByConcentration", "Boolean",
                "Arg5", "Float",
                "Arg6", "Boolean"
            });
            AddFunctor("RemoveStatus", 1, new List<string> {
                "StatusId", "StatusIdOrGroup"
            });
            AddFunctor("DealDamage", 1, new List<string> {
                "Damage", "Lua",
                "DamageType", "DamageTypeOrDealDamageWeaponDamageType",
                "Magical", "Magical",
                "Nonlethal", "Nonlethal",
                "CoinMultiplier", "Int",
                "Tooltip", "Guid",
                "Arg7", "Boolean",
                "Arg8", "Boolean",
                "Arg9", "Boolean",
                "Arg10", "Boolean",
            });
            AddFunctor("ExecuteWeaponFunctors", 0, new List<string> {
                "WeaponType", "ExecuteWeaponFunctorsType"
            });
            AddFunctor("RegainHitPoints", 1, new List<string> {
                "HitPoints", "Lua",
                "Type", "ResurrectType"
            });
            AddFunctor("TeleportSource", 0, new List<string> {
                "Arg1", "Boolean",
                "Arg2", "Boolean",
            });
            AddFunctor("SetStatusDuration", 2, new List<string> {
                "StatusId", "StatusId",
                "Duration", "Float",
                "ChangeType", "SetStatusDurationType",
            });
            AddFunctor("UseSpell", 1, new List<string> {
                "SpellId", "SpellId",
                "IgnoreHasSpell", "Boolean",
                "IgnoreChecks", "Boolean",
                "Arg4", "Boolean",
                "SpellCastGuid", "Guid",
            });
            AddFunctor("UseActionResource", 1, new List<string> {
                "ActionResource", "String", // Action resource name
                "Amount", "String", // Float or percentage
                "Level", "Int",
                "Arg4", "Boolean"
            });
            AddFunctor("UseAttack", 0, new List<string> {
                "IgnoreChecks", "Boolean"
            });
            AddFunctor("CreateExplosion", 0, new List<string> {
                "SpellId", "SpellId"
            });
            AddFunctor("BreakConcentration", 0, new List<string> {});
            AddFunctor("ApplyEquipmentStatus", 2, new List<string> {
                "ItemSlot", "StatItemSlot",
                "StatusId", "StatusId",
                "Chance", "Int",
                "Duration", "Lua",
                "StatusSpecificParam1", "String",
                "StatusSpecificParam2", "Int",
                "StatusSpecificParam3", "Int",
                "StatsConditions", "Conditions",
                "RequiresConcentration", "Boolean"
            });
            AddFunctor("RestoreResource", 2, new List<string> {
                "ActionResource", "String", // Action resource name
                "Amount", "Lua", // or percentage?
                "Level", "Int"
            });
            AddFunctor("Spawn", 1, new List<string> {
                "TemplateId", "Guid", // Root template Guid
                "AiHelper", "String", // Should be SpellId, but seemingly defunct?
                "StatusToApply1", "StatusId",
                "StatusToApply2", "StatusId",
                "StatusToApply3", "StatusId",
                "StatusToApply4", "StatusId",
                "Arg7", "Boolean"
            });
            AddFunctor("Stabilize", 0, new List<string>{});
            AddFunctor("Unlock", 0, new List<string>{});
            AddFunctor("ResetCombatTurn", 0, new List<string>{});
            AddFunctor("RemoveAuraByChildStatus", 1, new List<string> {
                "StatusId", "StatusId"
            });
            AddFunctor("SummonInInventory", 1, new List<string> {
                "TemplateId", "Guid", // Root template Guid
                "Duration", "SummonDurationOrInt",
                "Arg3", "Int",
                "Arg4", "Boolean",
                "Arg5", "Boolean",
                "Arg6", "Boolean",
                "Arg7", "Boolean",
                "Arg8", "String",
                "Arg9", "String",
                "Arg10", "String",
                "Arg11", "String", // etc.
            });
            AddFunctor("SpawnInInventory", 1, new List<string> {
                "TemplateId", "Guid", // Root template Guid
                "Arg2", "Int",
                "Arg3", "Boolean",
                "Arg4", "Boolean",
                "Arg5", "Boolean",
                "Arg6", "String",
                "Arg7", "String",
                "Arg8", "String", // etc.
            });
            AddFunctor("RemoveUniqueStatus", 1, new List<string> {
                "StatusId", "StatusId"
            });
            AddFunctor("DisarmWeapon", 0, new List<string> { });
            AddFunctor("DisarmAndStealWeapon", 0, new List<string> { });
            AddFunctor("SwitchDeathType", 1, new List<string> {
                "DeathType", "Death Type"
            });
            AddFunctor("TriggerRandomCast", 2, new List<string> {
                "Arg1", "Int",
                "Arg2", "Float",
                "Arg3", "String", // RandomCastOutcomesID resource
                "Arg4", "String", // RandomCastOutcomesID resource
                "Arg5", "String", // RandomCastOutcomesID resource
                "Arg6", "String", // RandomCastOutcomesID resource
            });
            AddFunctor("GainTemporaryHitPoints", 1, new List<string> {
                "Amount", "Lua"
            });
            AddFunctor("FireProjectile", 1, new List<string> {
                "Arg1", "String"
            });
            AddFunctor("ShortRest", 0, new List<string> {});
            AddFunctor("CreateZone", 0, new List<string> {
                "Shape", "ZoneShape",
                "Arg2", "Float",
                "Duration", "Float",
                "Arg4", "String",
                "Arg5", "Boolean",
            });
            AddFunctor("DoTeleport", 0, new List<string> {
                "Arg1", "Float"
            });
            AddFunctor("RegainTemporaryHitPoints", 1, new List<string> {
                "Amount", "Lua"
            });
            AddFunctor("RemoveStatusByLevel", 1, new List<string> {
                "StatusId", "StatusIdOrGroup",
                "Arg2", "Int",
                "Arg3", "Ability"
            });
            AddFunctor("SurfaceClearLayer", 0, new List<string> {
                "Layer1", "SurfaceLayer",
                "Layer2", "SurfaceLayer",
            });
            AddFunctor("Unsummon", 0, new List<string> { });
            AddFunctor("CreateWall", 0, new List<string> { });
            AddFunctor("Counterspell", 0, new List<string> { });
            AddFunctor("AdjustRoll", 1, new List<string> {
                "Amount", "Lua",
                "Type", "RollAdjustmentType",
                "DamageType", "Damage Type",
            });
            AddFunctor("SpawnExtraProjectiles", 0, new List<string> {
                "Arg1", "String", // ProjectileTypeId
            });
            AddFunctor("Kill", 0, new List<string> { });
            AddFunctor("TutorialEvent", 0, new List<string> {
                "Event", "Guid",
            });
            AddFunctor("Drop", 0, new List<string> {
                "Arg1", "String",
            });
            AddFunctor("ResetCooldowns", 1, new List<string> {
                "Type", "SpellCooldownType",
            });
            AddFunctor("SetRoll", 1, new List<string> {
                "Roll", "Int",
                "DistributionOrDamageType", "RollAdjustmentTypeOrDamageType"
            });
            AddFunctor("SetDamageResistance", 1, new List<string> {
                "DamageType", "Damage Type",
            });
            AddFunctor("SetReroll", 0, new List<string> {
                "Roll", "Int",
                "Arg2", "Boolean"
            });
            AddFunctor("SetAdvantage", 0, new List<string> { });
            AddFunctor("SetDisadvantage", 0, new List<string> { });
            AddFunctor("MaximizeRoll", 1, new List<string> {
                "DamageType", "Damage Type"
            });
            AddFunctor("CameraWait", 0, new List<string> {
                "Arg1", "Float"
            });



            AddDescriptionParams("DealDamage", 1, new List<string> {
                "Damage", "Lua",
                "DamageType", "DamageTypeOrDealDamageWeaponDamageType",
                "Magical", "Magical",
                "Nonlethal", "Nonlethal",
                "Arg5", "Int",
                "Tooltip", "Guid",
            });
            AddDescriptionParams("RegainHitPoints", 1, new List<string> {
                "HitPoints", "Lua",
                "Tooltip", "Guid",
            });
            AddDescriptionParams("Distance", 1, new List<string> {
                "Distance", "Float"
            });
            AddDescriptionParams("GainTemporaryHitPoints", 1, new List<string> {
                "Amount", "Lua"
            });
            AddDescriptionParams("LevelMapValue", 1, new List<string> {
                "LevelMap", "String"
            });
            AddDescriptionParams("ApplyStatus", 1, new List<string> {
                "StatusId", "StatusId",
                "Chance", "Int",
                "Duration", "Lua",
                "StatusSpecificParam1", "String",
                "StatusSpecificParam2", "Int",
                "StatusSpecificParam3", "Int",
                "StatsConditions", "Conditions",
                "RequiresConcentration", "Boolean"
            });



            AddBoost("AC", 1, new List<string> {
	            "AC", "Int"
            });
            AddBoost("Ability", 2, new List<string> {
	            "Ability", "Ability",
	            "Amount", "Int",
	            "Arg3", "Int",
            });
            AddBoost("RollBonus", 2, new List<string> {
	            "RollType", "StatsRollType",
	            "Bonus", "Lua",
	            "Arg3", "String",
            });
            AddBoost("Advantage", 1, new List<string> {
	            "Type", "AdvantageType",
	            "Arg2", "String", // Depends on type
	            "Tag1", "String", // TagManager resource
	            "Tag2", "String", // TagManager resource
	            "Tag3", "String", // TagManager resource
            });
            AddBoost("Disadvantage", 1, new List<string> {
	            "Type", "AdvantageType",
	            "Arg2", "String", // Depends on type
	            "Tag1", "String", // TagManager resource
	            "Tag2", "String", // TagManager resource
	            "Tag3", "String", // TagManager resource
            });
            AddBoost("ActionResource", 2, new List<string> {
	            "Resource", "String", // Action resource name
	            "Amount", "Float",
	            "Level", "Int",
                "DieType", "DieType",
            });
            AddBoost("CriticalHit", 3, new List<string> {
	            "Type", "CriticalHitType",
	            "Result", "CriticalHitResult",
	            "When", "CriticalHitWhen",
	            "Arg4", "Float",
            });
            AddBoost("AbilityFailedSavingThrow", 1, new List<string> {
	            "Ability", "Ability"
            });
            AddBoost("Resistance", 2, new List<string> {
                "DamageType", "AllOrDamageType",
                "ResistanceBoostFlags", "ResistanceBoostFlags"
            });
            AddBoost("WeaponDamageResistance", 1, new List<string> {
                "DamageType1", "Damage Type",
                "DamageType2", "Damage Type",
                "DamageType3", "Damage Type",
            });
            AddBoost("ProficiencyBonusOverride", 1, new List<string> {
	            "Bonus", "Lua"
            });
            AddBoost("ActionResourceOverride", 2, new List<string> {
                "Resource", "String", // Action resource name
	            "Amount", "Float",
                "Level", "Int",
                "DieType", "DieType",
            });
            AddBoost("AddProficiencyToAC", 0, new List<string> {});
            AddBoost("JumpMaxDistanceMultiplier", 1, new List<string> {
	            "Multiplier", "Float"
            });
            AddBoost("AddProficiencyToDamage", 0, new List<string> {});
            AddBoost("ActionResourceConsumeMultiplier", 3, new List<string> {
                "Resource", "String", // Action resource name
	            "Multiplier", "Float",
                "Level", "Int",
            });
            AddBoost("BlockVerbalComponent", 0, new List<string> {});
            AddBoost("BlockSomaticComponent", 0, new List<string> {});
            AddBoost("HalveWeaponDamage", 1, new List<string> {
	            "Ability", "Ability"
            });
            AddBoost("UnlockSpell", 1, new List<string> {
	            "SpellId", "SpellId",
                "Type", "UnlockSpellType",
                "SpellGuid", "String", // "None" or GUID or ""
                "Cooldown", "SpellCooldownType",
                "Ability", "Ability"
            });
            AddBoost("SourceAdvantageOnAttack", 0, new List<string> {
	            "Arg1", "Float"
            });
            AddBoost("ProficiencyBonus", 1, new List<string> {
	            "Type", "ProficiencyBonusBoostType",
                "Arg2", "String"
            });
            AddBoost("BlockSpellCast", 0, new List<string> {
	            "Arg1", "Float"
            });
            AddBoost("Proficiency", 1, new List<string> {
	            "Arg1", "ProficiencyGroupFlags",
	            "Arg2", "ProficiencyGroupFlags",
	            "Arg3", "ProficiencyGroupFlags",
            });
            AddBoost("SourceAllyAdvantageOnAttack", 0, new List<string> {});
            AddBoost("IncreaseMaxHP", 1, new List<string> {
	            "Amount", "String" // Lua or %
            });
            AddBoost("ActionResourceBlock", 1, new List<string> {
                "Resource", "String", // Action resource name
                "Level", "Int",
            });
            AddBoost("StatusImmunity", 1, new List<string> {
	            "StatusId", "StatusIdOrGroup",
	            "Tag1", "String", // Tag resource name
	            "Tag2", "String", // Tag resource name
	            "Tag3", "String", // Tag resource name
	            "Tag4", "String", // Tag resource name
	            "Tag5", "String", // Tag resource name
            });
            AddBoost("UseBoosts", 1, new List<string> {
	            "Arg1", "StatsFunctors"
            });
            AddBoost("CannotHarmCauseEntity", 1, new List<string> {
	            "Arg1", "String"
            });
            AddBoost("TemporaryHP", 1, new List<string> {
	            "Amount", "Lua"
            });
            AddBoost("Weight", 1, new List<string> {
	            "Weight", "Float"
            });
            AddBoost("WeightCategory", 1, new List<string> {
	            "Category", "Int"
            });
            AddBoost("FactionOverride", 1, new List<string> {
	            "Faction", "String" // Faction resource GUID or "Source"
            });
            AddBoost("ActionResourceMultiplier", 2, new List<string> {
                "Resource", "String", // Action resource name
	            "Multiplier", "Int",
                "Level", "Int",
            });
            AddBoost("BlockRegainHP", 0, new List<string> {
	            "Type", "ResurrectTypes"
            });
            AddBoost("Initiative", 1, new List<string> {
	            "Initiative", "Int"
            });
            AddBoost("DarkvisionRange", 1, new List<string> {
	            "Range", "Float"
            });
            AddBoost("DarkvisionRangeMin", 1, new List<string> {
                "Range", "Float"
            });
            AddBoost("DarkvisionRangeOverride", 1, new List<string> {
                "Range", "Float"
            });
            AddBoost("Tag", 1, new List<string> {
	            "Arg1", "String" // Tag resource name
            });
            AddBoost("IgnoreDamageThreshold", 2, new List<string> {
	            "DamageType", "AllOrDamageType",
                "Threshold", "Int"
            });
            AddBoost("Skill", 2, new List<string> {
	            "Skill", "SkillType",
                "Amount", "Lua"
            });
            AddBoost("WeaponDamage", 2, new List<string> {
	            "Amount", "Lua",
                "DamageType", "Damage Type",
                "Arg3", "Boolean"
            });
            AddBoost("NullifyAbilityScore", 1, new List<string> {
                "Ability", "Ability"
            });
            AddBoost("IgnoreFallDamage", 0, new List<string> {});
            AddBoost("Reroll", 3, new List<string> {
	            "RollType", "StatsRollType",
                "RollBelow", "Int",
                "Arg3", "Boolean"
            });
            AddBoost("DownedStatus", 1, new List<string> {
	            "StatusId", "StatusId",
                "Arg2", "Int"
            });
            AddBoost("Invulnerable", 0, new List<string> {});
            AddBoost("WeaponEnchantment", 1, new List<string> {
	            "Enchantment", "Int"
            });
            AddBoost("GuaranteedChanceRollOutcome", 1, new List<string> {
	            "Arg1", "Boolean"
            });
            AddBoost("Attribute", 1, new List<string> {
	            "Flags", "AttributeFlags"
            });
            AddBoost("IgnoreLeaveAttackRange", 0, new List<string> {});
            AddBoost("GameplayLight", 2, new List<string> {
	            "Arg1", "Float",
	            "Arg2", "Boolean",
	            "Arg3", "Float",
	            "Arg4", "Boolean"
            });
            AddBoost("DialogueBlock", 0, new List<string> {});
            AddBoost("DualWielding", 1, new List<string> {
	            "DW", "Boolean"
            });
            AddBoost("Savant", 1, new List<string> {
	            "SpellSchool", "SpellSchool"
            });
            AddBoost("MinimumRollResult", 2, new List<string> {
	            "RollType", "StatsRollType",
                "MinResult", "Int"
            });
            AddBoost("Lootable", 0, new List<string> {});
            AddBoost("CharacterWeaponDamage", 1, new List<string> {
	            "Amount", "Lua",
                "DamageType", "Damage Type"
            });
            AddBoost("ProjectileDeflect", 0, new List<string> {
	            "Type1", "String",
	            "Type2", "String",
            });
            AddBoost("AbilityOverrideMinimum", 2, new List<string> {
	            "Ability", "Ability",
                "Minimum", "Int"
            });
            AddBoost("ACOverrideFormula", 2, new List<string> {
	            "AC", "Int",
                "Arg2", "Boolean",
                "Ability1", "Ability",
                "Ability2", "Ability",
                "Ability3", "Ability",
            });
            AddBoost("FallDamageMultiplier", 1, new List<string> {
	            "Multiplier", "Float"
            });
            AddBoost("ActiveCharacterLight", 1, new List<string> {
	            "Light", "String"
            });
            AddBoost("Invisibility", 0, new List<string> {});
            AddBoost("TwoWeaponFighting", 0, new List<string> {});
            AddBoost("WeaponAttackTypeOverride", 1, new List<string> {
	            "Type", "AttackType"
            });
            AddBoost("WeaponDamageDieOverride", 1, new List<string> {
	            "DamageDie", "String", // die, eg. 1d10
            });
            AddBoost("CarryCapacityMultiplier", 1, new List<string> {
	            "Multiplier", "Float"
            });
            AddBoost("WeaponProperty", 1, new List<string> {
	            "Flags1", "WeaponFlags"
            });
            AddBoost("WeaponAttackRollAbilityOverride", 1, new List<string> {
	            "Ability", "AbilityOrAttackRollAbility"
            });
            AddBoost("BlockTravel", 0, new List<string> {});
            AddBoost("BlockGatherAtCamp", 0, new List<string> {});
            AddBoost("BlockAbilityModifierDamageBonus", 0, new List<string> {});
            AddBoost("VoicebarkBlock", 0, new List<string> {});
            AddBoost("HiddenDuringCinematic", 0, new List<string> {});
            AddBoost("SightRangeAdditive", 1, new List<string> {
	            "Range", "Float"
            });
            AddBoost("SightRangeMinimum", 1, new List<string> {
                "Range", "Float"
            });
            AddBoost("SightRangeMaximum", 1, new List<string> {
                "Range", "Float"
            });
            AddBoost("SightRangeOverride", 1, new List<string> {
                "Range", "Float"
            });
            AddBoost("CannotBeDisarmed", 0, new List<string> {});
            AddBoost("MovementSpeedLimit", 1, new List<string> {
	            "Type", "MovementSpeedType"
            });
            AddBoost("NonLethal", 0, new List<string> {});
            AddBoost("UnlockSpellVariant", 1, new List<string> {
	            "Modification1", "Lua", // TODO - add Modification parser?
	            "Modification2", "Lua",
	            "Modification3", "Lua",
	            "Modification4", "Lua",
	            "Modification5", "Lua",
	            "Modification6", "Lua",
	            "Modification7", "Lua",
	            "Modification8", "Lua",
	            "Modification9", "Lua",
	            "Modification10", "Lua",
	            "Modification11", "Lua",
	            "Modification12", "Lua",
	            "Modification13", "Lua",
	            "Modification14", "Lua",
	            "Modification15", "Lua"
            });
            AddBoost("DetectDisturbancesBlock", 1, new List<string> {
	            "Arg1", "Boolean"
            });
            AddBoost("BlockAbilityModifierFromAC", 1, new List<string> {
	            "Ability", "Ability"
            });
            AddBoost("ScaleMultiplier", 0, new List<string> {
	            "Multiplier", "Float"
            });
            AddBoost("CriticalDamageOnHit", 0, new List<string> {});
            AddBoost("DamageReduction", 2, new List<string> {
	            "DamageType", "AllOrDamageType",
                "ReductionType", "DamageReductionType",
                "Amount", "Lua"
            });
            AddBoost("ReduceCriticalAttackThreshold", 1, new List<string> {
	            "Threshold", "Int",
                "StatusId", "StatusIdOrGroup"
            });
            AddBoost("PhysicalForceRangeBonus", 1, new List<string> {
	            "Arg1", "String"
            });
            AddBoost("ObjectSize", 1, new List<string> {
	            "Size", "Int"
            });
            AddBoost("ObjectSizeOverride", 1, new List<string> {
                "Size", "String"
            });
            AddBoost("ItemReturnToOwner", 0, new List<string> {});
            AddBoost("AiArchetypeOverride", 1, new List<string> {
	            "Archetype", "String",
                "Arg2", "Int"
            });
            AddBoost("ExpertiseBonus", 1, new List<string> {
	            "Skill", "SkillType"
            });
            AddBoost("EntityThrowDamage", 1, new List<string> {
	            "Die", "String",
                "DamageType", "Damage Type"
            });
            AddBoost("WeaponDamageTypeOverride", 1, new List<string> {
	            "DamageType", "Damage Type"
            });
            AddBoost("MaximizeHealing", 1, new List<string> {
	            "Direction", "HealingDirection",
                "Type", "ResurrectType"
            });
            AddBoost("IgnoreEnterAttackRange", 0, new List<string> {});
            AddBoost("DamageBonus", 1, new List<string> {
	            "Amount", "Lua",
                "DamageType", "Damage Type",
                "Arg3", "Boolean"
            });
            AddBoost("Detach", 0, new List<string> {});
            AddBoost("ConsumeItemBlock", 0, new List<string> {});
            AddBoost("AdvanceSpells", 1, new List<string> {
	            "SpellId", "SpellId",
                "Arg2", "Int"
            });
            AddBoost("SpellResistance", 1, new List<string> {
	            "Resistance", "ResistanceBoostFlags"
            });
            AddBoost("WeaponAttackRollBonus", 1, new List<string> {
	            "Amount", "Lua"
            });
            AddBoost("SpellSaveDC", 1, new List<string> {
	            "DC", "Int"
            });
            AddBoost("RedirectDamage", 1, new List<string> {
	            "Arg1", "Float",
	            "DamageType", "Damage Type",
	            "DamageType2", "Damage Type",
                "Arg4", "Boolean"
            });
            AddBoost("CanSeeThrough", 1, new List<string> {
	            "CanSeeThrough", "Boolean"
            });
            AddBoost("CanShootThrough", 1, new List<string> {
                "CanShootThrough", "Boolean"
            });
            AddBoost("CanWalkThrough", 1, new List<string> {
                "CanWalkThrough", "Boolean"
            });
            AddBoost("MonkWeaponAttackOverride", 0, new List<string> {});
            AddBoost("MonkWeaponDamageDiceOverride", 1, new List<string> {
	            "Arg1", "Lua"
            });
            AddBoost("IntrinsicSummonerProficiency", 0, new List<string> {});
            AddBoost("HorizontalFOVOverride", 1, new List<string> {
	            "FOV", "Float"
            });
            AddBoost("CharacterUnarmedDamage", 1, new List<string> {
	            "Damage", "Lua",
                "DamageType", "Damage Type"
            });
            AddBoost("UnarmedMagicalProperty", 0, new List<string> {});
            AddBoost("ActionResourceReplenishTypeOverride", 2, new List<string> {
                "ActionResource", "String", // Action resource name
                "ReplenishType", "ResourceReplenishType"
            });
            AddBoost("AreaDamageEvade", 0, new List<string> {});
            AddBoost("ActionResourcePreventReduction", 1, new List<string> {
	            "ActionResource", "String", // Action resource name
                "Level", "Int"
            });
            AddBoost("AttackSpellOverride", 1, new List<string> {
	            "AttackSpell", "SpellId",
	            "OriginalSpell", "SpellId"
            });
            AddBoost("Lock", 0, new List<string> {
	            "DC", "Guid"
            });
            AddBoost("NoAOEDamageOnLand", 0, new List<string> {});
            AddBoost("IgnorePointBlankDisadvantage", 1, new List<string> {
	            "Flags", "WeaponFlags"
            });
            AddBoost("CriticalHitExtraDice", 1, new List<string> {
	            "ExtraDice", "Int",
                "AttackType", "AttackType"
            });
            AddBoost("DodgeAttackRoll", 2, new List<string> {
	            "Arg1", "Int",
	            "Arg2", "Int",
	            "Status", "StatusIdOrGroup"
            });
            AddBoost("GameplayObscurity", 1, new List<string> {
	            "Obscurity", "Float"
            });
            AddBoost("MaximumRollResult", 2, new List<string> {
                "RollType", "StatsRollType",
                "MinResult", "Int"
            });
            AddBoost("UnlockInterrupt", 1, new List<string> {
	            "Interrupt", "Interrupt"
            });
            AddBoost("IntrinsicSourceProficiency", 0, new List<string> {});
            AddBoost("JumpMaxDistanceBonus", 1, new List<string> {
	            "Bonus", "Float"
            });
            AddBoost("ArmorAbilityModifierCapOverride", 2, new List<string> {
	            "ArmorType", "ArmorType",
                "Cap", "Int"
            });
            AddBoost("IgnoreResistance", 2, new List<string> {
	            "DamageType", "Damage Type",
                "Flags", "ResistanceBoostFlags"
            });
            AddBoost("ConcentrationIgnoreDamage", 1, new List<string> {
	            "SpellSchool", "SpellSchool"
            });
            AddBoost("LeaveTriggers", 0, new List<string> {});
            AddBoost("IgnoreLowGroundPenalty", 1, new List<string> {
	            "RollType", "StatsRollType"
            });
            AddBoost("IgnoreSurfaceCover", 1, new List<string> {
	            "SurfaceType", "String" // Surface type
            });
            AddBoost("EnableBasicItemInteractions", 0, new List<string> {});
            AddBoost("SoundsBlocked", 0, new List<string> {});
        }

        public void LoadEnumerations(Stream stream)
        {
            StatEnumeration curEnum = null;

            string line;

            using (var reader = new StreamReader(stream))
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    if (trimmed.StartsWith("valuelist "))
                    {
                        var name = trimmed.Substring(11, trimmed.Length - 12);
                        curEnum = new StatEnumeration(name);
                        Enumerations.Add(curEnum.Name, curEnum);
                    }
                    else if (trimmed.StartsWith("value "))
                    {
                        var label = trimmed.Substring(7, trimmed.Length - 8);
                        curEnum.AddItem(label);
                    }
                }
            }
        }
    }
}
