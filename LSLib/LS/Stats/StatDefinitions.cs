namespace LSLib.LS.Stats;

public class StatEnumeration(string name)
{
    public readonly string Name = name;
    public readonly List<string> Values = [];
    public readonly Dictionary<string, int> ValueToIndexMap = [];

    public void AddItem(int index, string value)
    {
        if (Values.Count != index)
        {
            throw new Exception("Enumeration items must be added in order.");
        }

        Values.Add(value);

        // Some vanilla enums are bogus and contain names multiple times
        ValueToIndexMap.TryAdd(value, index);
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

    private IStatValueValidator Validator;

    public IStatValueValidator GetValidator(StatValueValidatorFactory factory, StatDefinitionRepository definitions)
    {
        Validator ??= factory.CreateValidator(this, definitions);
        return Validator;
    }
}

public class StatEntryType(string name, string nameProperty, string basedOnProperty)
{
    public readonly string Name = name;
    public readonly string NameProperty = nameProperty;
    public readonly string BasedOnProperty = basedOnProperty;
    public readonly Dictionary<string, StatField> Fields = [];
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

    public readonly Dictionary<string, StatEnumeration> Enumerations = [];
    public readonly Dictionary<string, StatEntryType> Types = [];
    public readonly Dictionary<string, StatFunctorType> Functors = [];
    public readonly Dictionary<string, StatFunctorType> Boosts = [];
    public readonly Dictionary<string, StatFunctorType> DescriptionParams = [];

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
                    var name = trimmed[15..^1];
                    defn = new StatEntryType(name, "Name", "Using");
                    Types.Add(defn.Name, defn);
                    AddField(defn, "Name", "FixedString");
                    var usingRef = AddField(defn, "Using", "StatReference");
                    usingRef.ReferenceTypes =
                    [
                        new StatReferenceConstraint
                        {
                            StatType = name
                        }
                    ];
                }
                else if (trimmed.StartsWith("modifier \""))
                {
                    var nameEnd = trimmed.IndexOf('"', 10);
                    var name = trimmed[10..nameEnd];
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

        var treasureTableType = new StatEntryType("TreasureTable", "Name", null);
        Types.Add(treasureTableType.Name, treasureTableType);
        AddField(treasureTableType, "Name", "FixedString");
        AddField(treasureTableType, "MinLevel", "ConstantInt");
        AddField(treasureTableType, "MaxLevel", "ConstantInt");
        AddField(treasureTableType, "CanMerge", "ConstantInt");
        AddField(treasureTableType, "IgnoreLevelDiff", "ConstantInt");
        AddField(treasureTableType, "UseTreasureGroupCounters", "ConstantInt");
        AddField(treasureTableType, "Subtables", "TreasureSubtables");

        var treasureSubtableType = new StatEntryType("TreasureSubtable", null, null);
        Types.Add(treasureSubtableType.Name, treasureSubtableType);
        AddField(treasureSubtableType, "DropCount", "FixedString"); // FIXME validate
        AddField(treasureSubtableType, "StartLevel", "ConstantInt");
        AddField(treasureSubtableType, "EndLevel", "ConstantInt");
        AddField(treasureSubtableType, "Objects", "TreasureSubtableObject");

        var treasureObjectType = new StatEntryType("TreasureSubtableObject", null, null);
        Types.Add(treasureObjectType.Name, treasureObjectType);
        AddField(treasureObjectType, "Drop", "TreasureDrop"); // FIXME validate
        AddField(treasureObjectType, "Frequency", "ConstantInt");
        AddField(treasureObjectType, "Common", "ConstantInt");
        AddField(treasureObjectType, "Uncommon", "ConstantInt");
        AddField(treasureObjectType, "Rare", "ConstantInt");
        AddField(treasureObjectType, "Epic", "ConstantInt");
        AddField(treasureObjectType, "Legendary", "ConstantInt");
        AddField(treasureObjectType, "Divine", "ConstantInt");
        AddField(treasureObjectType, "Unique", "ConstantInt");

        AddEnumeration("ResurrectType",
        [
            "Living",
            "Guaranteed",
            "Construct",
            "Undead"
        ]);

        AddEnumeration("SetStatusDurationType",
        [
            "SetMinimum",
            "ForceSet",
            "Add",
            "Multiply"
        ]);

        AddEnumeration("ExecuteWeaponFunctorsType",
        [
            "MainHand",
            "OffHand",
            "BothHands"
        ]);

        AddEnumeration("SpellCooldownType",
        [
            "Default",
            "OncePerTurn",
            "OncePerCombat",
            "UntilRest",
            "OncePerTurnNoRealtime",
            "UntilShortRest",
            "UntilPerRestPerItem",
            "OncePerShortRestPerItem"
        ]);

        AddEnumeration("SummonDuration",
        [
            "UntilLongRest",
            "Permanent"
        ]);

        AddEnumeration("ForceFunctorOrigin",
        [
            "OriginToEntity",
            "OriginToTarget",
            "TargetToEntity"
        ]);

        AddEnumeration("ForceFunctorAggression",
        [
            "Aggressive",
            "Friendly",
            "Neutral"
        ]);

        AddEnumeration("StatItemSlot",
        [
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
        ]);

        AddEnumeration("Magical",
        [
            "Magical",
            "Nonmagical"
        ]);

        AddEnumeration("Nonlethal",
        [
            "Lethal",
            "Nonlethal"
        ]);

        AddEnumeration("AllEnum",
        [
            "All"
        ]);

        AddEnumeration("ZoneShape",
        [
            "Cone",
            "Square",
        ]);

        AddEnumeration("SurfaceLayer",
        [
            "Ground",
            "Cloud",
        ]);

        AddEnumeration("RollAdjustmentType",
        [
            "All",
            "Distribute",
        ]);

        AddEnumeration("StatsRollType",
        [
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
        ]);

        AddEnumeration("AdvantageType",
        [
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
        ]);

        AddEnumeration("SkillType",
        [
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
        ]);

        AddEnumeration("CriticalHitType",
        [
            "AttackTarget",
            "AttackRoll"
        ]);

        AddEnumeration("Result",
        [
            "Success",
            "Failure"
        ]);

        AddEnumeration("CriticalHitResult",
        [
            "Success",
            "Failure"
        ]);

        AddEnumeration("CriticalHitWhen",
        [
            "Never",
            "Always",
            "ForcedAlways"
        ]);

        AddEnumeration("MovementSpeedType",
        [
            "Stroll",
            "Walk",
            "Run",
            "Sprint",
        ]);

        AddEnumeration("DamageReductionType",
        [
            "Half",
            "Flat",
            "Threshold"
        ]);

        AddEnumeration("AttackRollAbility",
        [
            "SpellCastingAbility",
            "UnarmedMeleeAbility",
            "AttackAbility"
        ]);

        AddEnumeration("HealingDirection",
        [
            "Incoming",
            "Outgoing"
        ]);

        AddEnumeration("ResistanceBoostFlags",
        [
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
        ]);

        AddEnumeration("UnlockSpellType",
        [
            "Singular", 
            "AddChildren", 
            "MostPowerful"
        ]);

        AddEnumeration("ProficiencyBonusBoostType",
        [
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
        ]);

        AddEnumeration("ResourceReplenishType",
        [
            "Never",
            "Default",
            "Combat",
            "Rest",
            "ShortRest",
            "FullRest",
            "ExhaustedRest"
        ]);

        AddEnumeration("AttackType",
        [
            "DirectHit",
            "MeleeWeaponAttack",
            "RangedWeaponAttack",
            "MeleeOffHandWeaponAttack",
            "RangedOffHandWeaponAttack",
            "MeleeSpellAttack",
            "RangedSpellAttack",
            "MeleeUnarmedAttack",
            "RangedUnarmedAttack"
        ]);

        AddEnumeration("DealDamageWeaponDamageType",
        [
            "MainWeaponDamageType",
            "OffhandWeaponDamageType",
            "MainMeleeWeaponDamageType",
            "OffhandMeleeWeaponDamageType",
            "MainRangedWeaponDamageType",
            "OffhandRangedWeaponDamageType",
            "SourceWeaponDamageType",
            "ThrownWeaponDamageType",
        ]);

        AddEnumeration("EngineStatusType",
        [
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
        ]);


        // Add functors
        AddFunctor("ApplyStatus", 1, [
            "StatusId", "StatusId",
            "Chance", "Int",
            "Duration", "Lua",
            "StatusSpecificParam1", "String",
            "StatusSpecificParam2", "Int",
            "StatusSpecificParam3", "Int",
            "StatsConditions", "Conditions",
            "RequiresConcentration", "Boolean"
        ]);
        AddFunctor("SurfaceChange", 1, [
            "SurfaceChange", "Surface Change",
            "Chance", "Float",
            "Arg3", "Float",
            "Arg4", "Float",
            "Arg5", "Float"
        ]);
        AddFunctor("Resurrect", 0, [
            "Chance", "Float",
            "HealthPercentage", "Float",
            "Type", "ResurrectType"
        ]);
        AddFunctor("Sabotage", 0, [
            "Amount", "Int"
        ]);
        AddFunctor("Summon", 1, [
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
        ]);
        AddFunctor("Force", 1, [
            "Distance", "Lua",
            "Origin", "ForceFunctorOrigin",
            "Aggression", "ForceFunctorAggression",
            "Arg4", "Boolean",
            "Arg5", "Boolean",
        ]);
        AddFunctor("Douse", 0, [
            "Arg1", "Float",
            "Arg2", "Float"
        ]);
        AddFunctor("SwapPlaces", 0, [
            "Animation", "String",
            "Arg2", "Boolean",
            "Arg3", "Boolean"
        ]);
        AddFunctor("Pickup", 0, [
            "Arg1", "String"
        ]);
        AddFunctor("CreateSurface", 3, [
            "Radius", "Float",
            "Duration", "Float",
            "SurfaceType", "Surface Type",
            "IsControlledByConcentration", "Boolean",
            "Arg5", "Float",
            "Arg6", "Boolean"
        ]);
        AddFunctor("CreateConeSurface", 3, [
            "Radius", "Float",
            "Duration", "Float",
            "SurfaceType", "Surface Type",
            "IsControlledByConcentration", "Boolean",
            "Arg5", "Float",
            "Arg6", "Boolean"
        ]);
        AddFunctor("RemoveStatus", 1, [
            "StatusId", "StatusIdOrGroup"
        ]);
        AddFunctor("DealDamage", 1, [
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
        ]);
        AddFunctor("ExecuteWeaponFunctors", 0, [
            "WeaponType", "ExecuteWeaponFunctorsType"
        ]);
        AddFunctor("RegainHitPoints", 1, [
            "HitPoints", "Lua",
            "Type", "ResurrectType"
        ]);
        AddFunctor("TeleportSource", 0, [
            "Arg1", "Boolean",
            "Arg2", "Boolean",
        ]);
        AddFunctor("SetStatusDuration", 2, [
            "StatusId", "StatusId",
            "Duration", "Float",
            "ChangeType", "SetStatusDurationType",
        ]);
        AddFunctor("UseSpell", 1, [
            "SpellId", "SpellId",
            "IgnoreHasSpell", "Boolean",
            "IgnoreChecks", "Boolean",
            "Arg4", "Boolean",
            "SpellCastGuid", "Guid",
        ]);
        AddFunctor("UseActionResource", 1, [
            "ActionResource", "String", // Action resource name
            "Amount", "String", // Float or percentage
            "Level", "Int",
            "Arg4", "Boolean"
        ]);
        AddFunctor("UseAttack", 0, [
            "IgnoreChecks", "Boolean"
        ]);
        AddFunctor("CreateExplosion", 0, [
            "SpellId", "SpellId"
        ]);
        AddFunctor("BreakConcentration", 0, []);
        AddFunctor("ApplyEquipmentStatus", 2, [
            "ItemSlot", "StatItemSlot",
            "StatusId", "StatusId",
            "Chance", "Int",
            "Duration", "Lua",
            "StatusSpecificParam1", "String",
            "StatusSpecificParam2", "Int",
            "StatusSpecificParam3", "Int",
            "StatsConditions", "Conditions",
            "RequiresConcentration", "Boolean"
        ]);
        AddFunctor("RestoreResource", 2, [
            "ActionResource", "String", // Action resource name
            "Amount", "Lua", // or percentage?
            "Level", "Int"
        ]);
        AddFunctor("Spawn", 1, [
            "TemplateId", "Guid", // Root template Guid
            "AiHelper", "String", // Should be SpellId, but seemingly defunct?
            "StatusToApply1", "StatusId",
            "StatusToApply2", "StatusId",
            "StatusToApply3", "StatusId",
            "StatusToApply4", "StatusId",
            "Arg7", "Boolean"
        ]);
        AddFunctor("Stabilize", 0, []);
        AddFunctor("Unlock", 0, []);
        AddFunctor("ResetCombatTurn", 0, []);
        AddFunctor("RemoveAuraByChildStatus", 1, [
            "StatusId", "StatusId"
        ]);
        AddFunctor("SummonInInventory", 1, [
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
        ]);
        AddFunctor("SpawnInInventory", 1, [
            "TemplateId", "Guid", // Root template Guid
            "Arg2", "Int",
            "Arg3", "Boolean",
            "Arg4", "Boolean",
            "Arg5", "Boolean",
            "Arg6", "String",
            "Arg7", "String",
            "Arg8", "String", // etc.
        ]);
        AddFunctor("RemoveUniqueStatus", 1, [
            "StatusId", "StatusId"
        ]);
        AddFunctor("DisarmWeapon", 0, []);
        AddFunctor("DisarmAndStealWeapon", 0, []);
        AddFunctor("SwitchDeathType", 1, [
            "DeathType", "Death Type"
        ]);
        AddFunctor("TriggerRandomCast", 2, [
            "Arg1", "Int",
            "Arg2", "Float",
            "Arg3", "String", // RandomCastOutcomesID resource
            "Arg4", "String", // RandomCastOutcomesID resource
            "Arg5", "String", // RandomCastOutcomesID resource
            "Arg6", "String", // RandomCastOutcomesID resource
        ]);
        AddFunctor("GainTemporaryHitPoints", 1, [
            "Amount", "Lua"
        ]);
        AddFunctor("FireProjectile", 1, [
            "Arg1", "String"
        ]);
        AddFunctor("ShortRest", 0, []);
        AddFunctor("CreateZone", 0, [
            "Shape", "ZoneShape",
            "Arg2", "Float",
            "Duration", "Float",
            "Arg4", "String",
            "Arg5", "Boolean",
        ]);
        AddFunctor("DoTeleport", 0, [
            "Arg1", "Float"
        ]);
        AddFunctor("RegainTemporaryHitPoints", 1, [
            "Amount", "Lua"
        ]);
        AddFunctor("RemoveStatusByLevel", 1, [
            "StatusId", "StatusIdOrGroup",
            "Arg2", "Int",
            "Arg3", "Ability"
        ]);
        AddFunctor("SurfaceClearLayer", 0, [
            "Layer1", "SurfaceLayer",
            "Layer2", "SurfaceLayer",
        ]);
        AddFunctor("Unsummon", 0, []);
        AddFunctor("CreateWall", 0, []);
        AddFunctor("Counterspell", 0, []);
        AddFunctor("AdjustRoll", 1, [
            "Amount", "Lua",
            "Type", "RollAdjustmentType",
            "DamageType", "Damage Type",
        ]);
        AddFunctor("SpawnExtraProjectiles", 0, [
            "Arg1", "String", // ProjectileTypeId
        ]);
        AddFunctor("Kill", 0, []);
        AddFunctor("TutorialEvent", 0, [
            "Event", "Guid",
        ]);
        AddFunctor("Drop", 0, [
            "Arg1", "String",
        ]);
        AddFunctor("ResetCooldowns", 1, [
            "Type", "SpellCooldownType",
        ]);
        AddFunctor("SetRoll", 1, [
            "Roll", "Int",
            "DistributionOrDamageType", "RollAdjustmentTypeOrDamageType"
        ]);
        AddFunctor("SetDamageResistance", 1, [
            "DamageType", "Damage Type",
        ]);
        AddFunctor("SetReroll", 0, [
            "Roll", "Int",
            "Arg2", "Boolean"
        ]);
        AddFunctor("SetAdvantage", 0, []);
        AddFunctor("SetDisadvantage", 0, []);
        AddFunctor("MaximizeRoll", 1, [
            "DamageType", "Damage Type"
        ]);
        AddFunctor("CameraWait", 0, [
            "Arg1", "Float"
        ]);



        AddDescriptionParams("DealDamage", 1, [
            "Damage", "Lua",
            "DamageType", "DamageTypeOrDealDamageWeaponDamageType",
            "Magical", "Magical",
            "Nonlethal", "Nonlethal",
            "Arg5", "Int",
            "Tooltip", "Guid",
        ]);
        AddDescriptionParams("RegainHitPoints", 1, [
            "HitPoints", "Lua",
            "Tooltip", "Guid",
        ]);
        AddDescriptionParams("Distance", 1, [
            "Distance", "Float"
        ]);
        AddDescriptionParams("GainTemporaryHitPoints", 1, [
            "Amount", "Lua"
        ]);
        AddDescriptionParams("LevelMapValue", 1, [
            "LevelMap", "String"
        ]);
        AddDescriptionParams("ApplyStatus", 1, [
            "StatusId", "StatusId",
            "Chance", "Int",
            "Duration", "Lua",
            "StatusSpecificParam1", "String",
            "StatusSpecificParam2", "Int",
            "StatusSpecificParam3", "Int",
            "StatsConditions", "Conditions",
            "RequiresConcentration", "Boolean"
        ]);



        AddBoost("AC", 1, [
	        "AC", "Int"
        ]);
        AddBoost("Ability", 2, [
	        "Ability", "Ability",
	        "Amount", "Int",
	        "Arg3", "Int",
        ]);
        AddBoost("RollBonus", 2, [
	        "RollType", "StatsRollType",
	        "Bonus", "Lua",
	        "Arg3", "String",
        ]);
        AddBoost("Advantage", 1, [
	        "Type", "AdvantageType",
	        "Arg2", "String", // Depends on type
	        "Tag1", "String", // TagManager resource
	        "Tag2", "String", // TagManager resource
	        "Tag3", "String", // TagManager resource
        ]);
        AddBoost("Disadvantage", 1, [
	        "Type", "AdvantageType",
	        "Arg2", "String", // Depends on type
	        "Tag1", "String", // TagManager resource
	        "Tag2", "String", // TagManager resource
	        "Tag3", "String", // TagManager resource
        ]);
        AddBoost("ActionResource", 2, [
	        "Resource", "String", // Action resource name
	        "Amount", "Float",
	        "Level", "Int",
            "DieType", "DieType",
        ]);
        AddBoost("CriticalHit", 3, [
	        "Type", "CriticalHitType",
	        "Result", "CriticalHitResult",
	        "When", "CriticalHitWhen",
	        "Arg4", "Float",
        ]);
        AddBoost("AbilityFailedSavingThrow", 1, [
	        "Ability", "Ability"
        ]);
        AddBoost("Resistance", 2, [
            "DamageType", "AllOrDamageType",
            "ResistanceBoostFlags", "ResistanceBoostFlags"
        ]);
        AddBoost("WeaponDamageResistance", 1, [
            "DamageType1", "Damage Type",
            "DamageType2", "Damage Type",
            "DamageType3", "Damage Type",
        ]);
        AddBoost("ProficiencyBonusOverride", 1, [
	        "Bonus", "Lua"
        ]);
        AddBoost("ActionResourceOverride", 2, [
            "Resource", "String", // Action resource name
	        "Amount", "Float",
            "Level", "Int",
            "DieType", "DieType",
        ]);
        AddBoost("AddProficiencyToAC", 0, []);
        AddBoost("JumpMaxDistanceMultiplier", 1, [
	            "Multiplier", "Float"
        ]);
        AddBoost("AddProficiencyToDamage", 0, []);
        AddBoost("ActionResourceConsumeMultiplier", 3, [
            "Resource", "String", // Action resource name
	        "Multiplier", "Float",
            "Level", "Int",
        ]);
        AddBoost("BlockVerbalComponent", 0, []);
        AddBoost("BlockSomaticComponent", 0, []);
        AddBoost("HalveWeaponDamage", 1, [
	        "Ability", "Ability"
        ]);
        AddBoost("UnlockSpell", 1, [
	        "SpellId", "SpellId",
            "Type", "UnlockSpellType",
            "SpellGuid", "String", // "None" or GUID or ""
            "Cooldown", "SpellCooldownType",
            "Ability", "Ability"
        ]);
        AddBoost("SourceAdvantageOnAttack", 0, [
	        "Arg1", "Float"
        ]);
        AddBoost("ProficiencyBonus", 1, [
	        "Type", "ProficiencyBonusBoostType",
            "Arg2", "String"
        ]);
        AddBoost("BlockSpellCast", 0, [
	        "Arg1", "Float"
        ]);
        AddBoost("Proficiency", 1, [
	        "Arg1", "ProficiencyGroupFlags",
	        "Arg2", "ProficiencyGroupFlags",
	        "Arg3", "ProficiencyGroupFlags",
        ]);
        AddBoost("SourceAllyAdvantageOnAttack", 0, []);
        AddBoost("IncreaseMaxHP", 1, [
	         "Amount", "String" // Lua or %
        ]);
        AddBoost("ActionResourceBlock", 1, [
            "Resource", "String", // Action resource name
            "Level", "Int",
        ]);
        AddBoost("StatusImmunity", 1, [
	        "StatusId", "StatusIdOrGroup",
	        "Tag1", "String", // Tag resource name
	        "Tag2", "String", // Tag resource name
	        "Tag3", "String", // Tag resource name
	        "Tag4", "String", // Tag resource name
	        "Tag5", "String", // Tag resource name
        ]);
        AddBoost("UseBoosts", 1, [
	        "Arg1", "StatsFunctors"
        ]);
        AddBoost("CannotHarmCauseEntity", 1, [
	        "Arg1", "String"
        ]);
        AddBoost("TemporaryHP", 1, [
	        "Amount", "Lua"
        ]);
        AddBoost("Weight", 1, [
	        "Weight", "Float"
        ]);
        AddBoost("WeightCategory", 1, [
	        "Category", "Int"
        ]);
        AddBoost("FactionOverride", 1, [
	        "Faction", "String" // Faction resource GUID or "Source"
        ]);
        AddBoost("ActionResourceMultiplier", 2, [
            "Resource", "String", // Action resource name
	        "Multiplier", "Int",
            "Level", "Int",
        ]);
        AddBoost("BlockRegainHP", 0, [
	        "Type", "ResurrectTypes"
        ]);
        AddBoost("Initiative", 1, [
	        "Initiative", "Int"
        ]);
        AddBoost("DarkvisionRange", 1, [
	        "Range", "Float"
        ]);
        AddBoost("DarkvisionRangeMin", 1, [
            "Range", "Float"
        ]);
        AddBoost("DarkvisionRangeOverride", 1, [
            "Range", "Float"
        ]);
        AddBoost("Tag", 1, [
	        "Arg1", "String" // Tag resource name
        ]);
        AddBoost("IgnoreDamageThreshold", 2, [
	        "DamageType", "AllOrDamageType",
            "Threshold", "Int"
        ]);
        AddBoost("Skill", 2, [
	        "Skill", "SkillType",
            "Amount", "Lua"
        ]);
        AddBoost("WeaponDamage", 2, [
	        "Amount", "Lua",
            "DamageType", "Damage Type",
            "Arg3", "Boolean"
        ]);
        AddBoost("NullifyAbilityScore", 1, [
            "Ability", "Ability"
        ]);
        AddBoost("IgnoreFallDamage", 0, []);
        AddBoost("Reroll", 3, [
	        "RollType", "StatsRollType",
            "RollBelow", "Int",
            "Arg3", "Boolean"
        ]);
        AddBoost("DownedStatus", 1, [
	        "StatusId", "StatusId",
            "Arg2", "Int"
        ]);
        AddBoost("Invulnerable", 0, []);
        AddBoost("WeaponEnchantment", 1, [
	        "Enchantment", "Int"
        ]);
        AddBoost("GuaranteedChanceRollOutcome", 1, [
	        "Arg1", "Boolean"
        ]);
        AddBoost("Attribute", 1, [
	        "Flags", "AttributeFlags"
        ]);
        AddBoost("IgnoreLeaveAttackRange", 0, []);
        AddBoost("GameplayLight", 2, [
	        "Arg1", "Float",
	        "Arg2", "Boolean",
	        "Arg3", "Float",
	        "Arg4", "Boolean"
        ]);
        AddBoost("DialogueBlock", 0, []);
        AddBoost("DualWielding", 1, [
	        "DW", "Boolean"
        ]);
        AddBoost("Savant", 1, [
	        "SpellSchool", "SpellSchool"
        ]);
        AddBoost("MinimumRollResult", 2, [
	        "RollType", "StatsRollType",
            "MinResult", "Int"
        ]);
        AddBoost("Lootable", 0, []);
        AddBoost("CharacterWeaponDamage", 1, [
	        "Amount", "Lua",
            "DamageType", "Damage Type"
        ]);
        AddBoost("ProjectileDeflect", 0, [
	        "Type1", "String",
	        "Type2", "String",
        ]);
        AddBoost("AbilityOverrideMinimum", 2, [
	        "Ability", "Ability",
            "Minimum", "Int"
        ]);
        AddBoost("ACOverrideFormula", 2, [
	        "AC", "Int",
            "Arg2", "Boolean",
            "Ability1", "Ability",
            "Ability2", "Ability",
            "Ability3", "Ability",
        ]);
        AddBoost("FallDamageMultiplier", 1, [
	        "Multiplier", "Float"
        ]);
        AddBoost("ActiveCharacterLight", 1, [
	        "Light", "String"
        ]);
        AddBoost("Invisibility", 0, []);
        AddBoost("TwoWeaponFighting", 0, []);
        AddBoost("WeaponAttackTypeOverride", 1, [
	        "Type", "AttackType"
        ]);
        AddBoost("WeaponDamageDieOverride", 1, [
	        "DamageDie", "String", // die, eg. 1d10
        ]);
        AddBoost("CarryCapacityMultiplier", 1, [
	        "Multiplier", "Float"
        ]);
        AddBoost("WeaponProperty", 1, [
	        "Flags1", "WeaponFlags"
        ]);
        AddBoost("WeaponAttackRollAbilityOverride", 1, [
	        "Ability", "AbilityOrAttackRollAbility"
        ]);
        AddBoost("BlockTravel", 0, []);
        AddBoost("BlockGatherAtCamp", 0, []);
        AddBoost("BlockAbilityModifierDamageBonus", 0, []);
        AddBoost("VoicebarkBlock", 0, []);
        AddBoost("HiddenDuringCinematic", 0, []);
        AddBoost("SightRangeAdditive", 1, [
	        "Range", "Float"
        ]);
        AddBoost("SightRangeMinimum", 1, [
            "Range", "Float"
        ]);
        AddBoost("SightRangeMaximum", 1, [
            "Range", "Float"
        ]);
        AddBoost("SightRangeOverride", 1, [
            "Range", "Float"
        ]);
        AddBoost("CannotBeDisarmed", 0, []);
        AddBoost("MovementSpeedLimit", 1, [
	            "Type", "MovementSpeedType"
        ]);
        AddBoost("NonLethal", 0, []);
        AddBoost("UnlockSpellVariant", 1, [
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
        ]);
        AddBoost("DetectDisturbancesBlock", 1, [
	        "Arg1", "Boolean"
        ]);
        AddBoost("BlockAbilityModifierFromAC", 1, [
	        "Ability", "Ability"
        ]);
        AddBoost("ScaleMultiplier", 0, [
	        "Multiplier", "Float"
        ]);
        AddBoost("CriticalDamageOnHit", 0, []);
        AddBoost("DamageReduction", 2, [
	        "DamageType", "AllOrDamageType",
            "ReductionType", "DamageReductionType",
            "Amount", "Lua"
        ]);
        AddBoost("ReduceCriticalAttackThreshold", 1, [
	        "Threshold", "Int",
            "StatusId", "StatusIdOrGroup"
        ]);
        AddBoost("PhysicalForceRangeBonus", 1, [
	        "Arg1", "String"
        ]);
        AddBoost("ObjectSize", 1, [
	        "Size", "Int"
        ]);
        AddBoost("ObjectSizeOverride", 1, [
            "Size", "String"
        ]);
        AddBoost("ItemReturnToOwner", 0, []);
        AddBoost("AiArchetypeOverride", 1, [
	        "Archetype", "String",
            "Arg2", "Int"
        ]);
        AddBoost("ExpertiseBonus", 1, [
	        "Skill", "SkillType"
        ]);
        AddBoost("EntityThrowDamage", 1, [
	        "Die", "String",
            "DamageType", "Damage Type"
        ]);
        AddBoost("WeaponDamageTypeOverride", 1, [
	        "DamageType", "Damage Type"
        ]);
        AddBoost("MaximizeHealing", 1, [
	        "Direction", "HealingDirection",
            "Type", "ResurrectType"
        ]);
        AddBoost("IgnoreEnterAttackRange", 0, []);
        AddBoost("DamageBonus", 1, [
	        "Amount", "Lua",
            "DamageType", "Damage Type",
            "Arg3", "Boolean"
        ]);
        AddBoost("Detach", 0, []);
        AddBoost("ConsumeItemBlock", 0, []);
        AddBoost("AdvanceSpells", 1, [
	        "SpellId", "SpellId",
            "Arg2", "Int"
        ]);
        AddBoost("SpellResistance", 1, [
	        "Resistance", "ResistanceBoostFlags"
        ]);
        AddBoost("WeaponAttackRollBonus", 1, [
	        "Amount", "Lua"
        ]);
        AddBoost("SpellSaveDC", 1, [
	        "DC", "Int"
        ]);
        AddBoost("RedirectDamage", 1, [
	        "Arg1", "Float",
	        "DamageType", "Damage Type",
	        "DamageType2", "Damage Type",
            "Arg4", "Boolean"
        ]);
        AddBoost("CanSeeThrough", 1, [
	        "CanSeeThrough", "Boolean"
        ]);
        AddBoost("CanShootThrough", 1, [
            "CanShootThrough", "Boolean"
        ]);
        AddBoost("CanWalkThrough", 1, [
            "CanWalkThrough", "Boolean"
        ]);
        AddBoost("MonkWeaponAttackOverride", 0, []);
        AddBoost("MonkWeaponDamageDiceOverride", 1, [
	        "Arg1", "Lua"
        ]);
        AddBoost("IntrinsicSummonerProficiency", 0, []);
        AddBoost("HorizontalFOVOverride", 1, [
	        "FOV", "Float"
        ]);
        AddBoost("CharacterUnarmedDamage", 1, [
	        "Damage", "Lua",
            "DamageType", "Damage Type"
        ]);
        AddBoost("UnarmedMagicalProperty", 0, []);
        AddBoost("ActionResourceReplenishTypeOverride", 2, [
            "ActionResource", "String", // Action resource name
            "ReplenishType", "ResourceReplenishType"
        ]);
        AddBoost("AreaDamageEvade", 0, []);
        AddBoost("ActionResourcePreventReduction", 1, [
	        "ActionResource", "String", // Action resource name
            "Level", "Int"
        ]);
        AddBoost("AttackSpellOverride", 1, [
	        "AttackSpell", "SpellId",
	        "OriginalSpell", "SpellId"
        ]);
        AddBoost("Lock", 0, [
	        "DC", "Guid"
        ]);
        AddBoost("NoAOEDamageOnLand", 0, []);
        AddBoost("IgnorePointBlankDisadvantage", 1, [
	        "Flags", "WeaponFlags"
        ]);
        AddBoost("CriticalHitExtraDice", 1, [
	        "ExtraDice", "Int",
            "AttackType", "AttackType"
        ]);
        AddBoost("DodgeAttackRoll", 2, [
	        "Arg1", "Int",
	        "Arg2", "Int",
	        "Status", "StatusIdOrGroup"
        ]);
        AddBoost("GameplayObscurity", 1, [
	        "Obscurity", "Float"
        ]);
        AddBoost("MaximumRollResult", 2, [
            "RollType", "StatsRollType",
            "MinResult", "Int"
        ]);
        AddBoost("UnlockInterrupt", 1, [
	        "Interrupt", "Interrupt"
        ]);
        AddBoost("IntrinsicSourceProficiency", 0, []);
        AddBoost("JumpMaxDistanceBonus", 1, [
	        "Bonus", "Float"
        ]);
        AddBoost("ArmorAbilityModifierCapOverride", 2, [
	        "ArmorType", "ArmorType",
            "Cap", "Int"
        ]);
        AddBoost("IgnoreResistance", 2, [
	        "DamageType", "Damage Type",
            "Flags", "ResistanceBoostFlags"
        ]);
        AddBoost("ConcentrationIgnoreDamage", 1, [
	        "SpellSchool", "SpellSchool"
        ]);
        AddBoost("LeaveTriggers", 0, []);
        AddBoost("IgnoreLowGroundPenalty", 1, [
	        "RollType", "StatsRollType"
        ]);
        AddBoost("IgnoreSurfaceCover", 1, [
	        "SurfaceType", "String" // Surface type
        ]);
        AddBoost("EnableBasicItemInteractions", 0, []);
        AddBoost("SoundsBlocked", 0, []);
    }

    public void LoadEnumerations(Stream stream)
    {
        StatEnumeration curEnum = null;

        string line;

        using var reader = new StreamReader(stream);
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                if (trimmed.StartsWith("valuelist "))
                {
                    var name = trimmed[11..^1];
                    curEnum = new StatEnumeration(name);
                    Enumerations.Add(curEnum.Name, curEnum);
                }
                else if (trimmed.StartsWith("value "))
                {
                    var label = trimmed[7..^1];
                    curEnum.AddItem(label);
                }
            }
        }
    }
}
