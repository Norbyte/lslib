using LSLib.LS.Stats.Properties;
using LSLib.LS.Stats.StatParser;
using LSLib.LS.Story.GoalParser;
using System.Globalization;

namespace LSLib.LS.Stats;

public class DiagnosticContext
{
    public bool IgnoreMissingReferences = false;
    public StatDeclaration CurrentDeclaration;
    public CodeLocation PropertyValueSpan;
}

public enum PropertyDiagnosticContextType
{
    Argument,
    Call,
    Property,
    Entry
}

public struct PropertyDiagnosticContext
{
    public PropertyDiagnosticContextType Type;
    public string Context;
    public CodeLocation Location;
}

public class PropertyDiagnostic
{
    public string Message;
    public CodeLocation Location;
    public List<PropertyDiagnosticContext> Contexts;
}

public class PropertyDiagnosticContainer
{
    public List<PropertyDiagnostic> Messages;

    public bool Empty
    {
        get {  return Messages == null || Messages.Count == 0; }
    }

    public void AddContext(PropertyDiagnosticContextType type, string name, CodeLocation location = null)
    {
        if (Empty) return;

        var context = new PropertyDiagnosticContext
        {
            Type = type,
            Context = name,
            Location = location
        };

        foreach (var msg in Messages)
        {
            msg.Contexts ??= [];
            msg.Contexts.Add(context);
        }
    }

    public void Add(string message, CodeLocation location = null)
    {
        Messages ??= [];
        Messages.Add(new PropertyDiagnostic
        {
            Message = message,
            Location = location
        });
    }

    public void MergeInto(PropertyDiagnosticContainer container)
    {
        if (Empty) return;

        container.Messages ??= [];
        container.Messages.AddRange(Messages);
    }

    public void MergeInto(StatLoadingContext context, string declarationName)
    {
        if (Empty) return;

        foreach (var message in Messages)
        {
            var location = message.Location;
            foreach (var ctx in message.Contexts)
            {
                location ??= ctx.Location;
            }

            context.LogError(DiagnosticCode.StatPropertyValueInvalid, message.Message,
                location, message.Contexts);
        }
    }

    public void Clear()
    {
        Messages?.Clear();
    }
}

public interface IStatValueValidator
{
    void Validate(DiagnosticContext ctx, CodeLocation location, object value, PropertyDiagnosticContainer errors);
}

abstract public class StatStringValidator : IStatValueValidator
{
    abstract public void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors);

    public void Validate(DiagnosticContext ctx, CodeLocation location, object value, PropertyDiagnosticContainer errors)
    {
        Validate(ctx, (string)value, errors);
    }
}

public class StatReferenceConstraint
{
    public string StatType;
}

public interface IStatReferenceValidator
{
    bool IsValidReference(string reference, string statType);
    bool IsValidGuidResource(string name, string resourceType);
}

public class BooleanValidator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value != "true" && value != "false" && value != "")
        {
            errors.Add("expected boolean value 'true' or 'false'");
        }
    }
}

public class Int32Validator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value != "" && !Int32.TryParse(value, out int intval))
        {
            errors.Add("expected an integer value");
        }
    }
}

public class FloatValidator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value != "" && !Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
        {
            errors.Add("expected a float value");
        }
    }
}

public class EnumValidator(StatEnumeration enumeration) : StatStringValidator
{
    private readonly StatEnumeration Enumeration = enumeration ?? throw new ArgumentNullException();

    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value != "" && !Enumeration.ValueToIndexMap.ContainsKey(value))
        {
            if (Enumeration.Values.Count > 20)
            {
                errors.Add("expected one of: " + String.Join(", ", Enumeration.Values.Take(20)) + ", ...");
            }
            else
            {
                errors.Add("expected one of: " + String.Join(", ", Enumeration.Values));
            }
        }
    }
}

public class MultiValueEnumValidator(StatEnumeration enumeration) : StatStringValidator
{
    private readonly EnumValidator Validator = new(enumeration);

    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value.Length == 0) return;

        foreach (var item in value.Split([';']))
        {
            Validator.Validate(ctx, item.Trim([' ']), errors);
        }
    }
}

public class StringValidator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value.Length > 2047)
        {
            // FixedString constructors crash over 2047 chars as there is no pool for that string size
            errors.Add("Value cannot be longer than 2047 characters");
        }
    }
}

public class UUIDValidator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value != "" && !Guid.TryParseExact(value, "D", out Guid parsed))
        {
            errors.Add($"'{value}' is not a valid UUID");
        }
    }
}

public class StatReferenceValidator(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints) : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (ctx.IgnoreMissingReferences || value == "") return;

        foreach (var constraint in constraints)
        {
            if (validator.IsValidReference(value, constraint.StatType))
            {
                return;
            }
        }

        var refTypes = String.Join("/", constraints.Select(c => c.StatType));
        errors.Add($"'{value}' is not a valid {refTypes} reference");
    }
}

public class MultiValueStatReferenceValidator(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints) : StatStringValidator
{
    private readonly StatReferenceValidator Validator = new(validator, constraints);

    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        foreach (var item in value.Split([';']))
        {
            var trimmed = item.Trim([' ']);
            if (trimmed.Length > 0)
            {
                Validator.Validate(ctx, trimmed, errors);
            }
        }
    }
}

public enum ExpressionType
{
    Boost,
    Functor,
    DescriptionParams
};

public class ExpressionValidator(String validatorType, StatDefinitionRepository definitions,
    StatValueValidatorFactory validatorFactory, ExpressionType type) : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        var typeLen = 10 + validatorType.Length;
        var valueBytes = Encoding.UTF8.GetBytes("__TYPE_" + validatorType + "__ " + value.TrimEnd());
        using var buf = new MemoryStream(valueBytes);

        var scanner = new StatPropertyScanner();
        scanner.SetSource(buf);
        var parser = new StatPropertyParser(scanner, definitions, ctx, validatorFactory, valueBytes, type, errors, ctx.PropertyValueSpan, typeLen);
        var succeeded = parser.Parse();
        if (!succeeded)
        {
            // FIXME pass location to error container
            var location = scanner.LastLocation();
            var column = location.StartColumn - typeLen;
            errors.Add($"Syntax error at or near character {column}");
        }
    }
}

public class LuaExpressionValidator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        using var buf = new MemoryStream(valueBytes);
        var scanner = new Lua.StatLuaScanner();
        scanner.SetSource(buf);
        var parser = new Lua.StatLuaParser(scanner);
        var succeeded = parser.Parse();
        if (!succeeded)
        {
            // FIXME pass location to error container
            var location = scanner.LastLocation();
            if (location.StartColumn != -1)
            {
                errors.Add($"Syntax error at or near character {location.StartColumn}");
            }
            else
            {
                errors.Add($"Syntax error");
            }
        }
    }
}

public class UseCostsValidator(IStatReferenceValidator validator) : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value.Length == 0) return;

        foreach (var resource in value.Split(';'))
        {
            var res = resource.Trim();
            if (res.Length == 0) continue;

            var parts = res.Split(':');
            if (parts.Length < 2 || parts.Length > 4)
            {
                errors.Add($"Malformed use costs");
                return;
            }

            if (!ctx.IgnoreMissingReferences && !validator.IsValidGuidResource(parts[0], "ActionResource") && !validator.IsValidGuidResource(parts[0], "ActionResourceGroup"))
            {
                errors.Add($"Nonexistent action resource or action resource group: {parts[0]}");
            }

            var distanceExpr = parts[1].Split('*');
            if (distanceExpr[0] == "Distance")
            {
                if (distanceExpr.Length > 1 && !Single.TryParse(distanceExpr[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
                {
                    errors.Add($"Malformed distance multiplier: {distanceExpr[1]}");
                    continue;
                }

            }
            else if (!Single.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
            {
                errors.Add($"Malformed resource amount: {parts[1]}");
                continue;
            }

            if (parts.Length == 3 && !Int32.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int intval))
            {
                errors.Add($"Malformed level: {parts[2]}");
                continue;
            }

            if (parts.Length == 4 && !Int32.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out intval))
            {
                errors.Add($"Malformed level: {parts[3]}");
                continue;
            }
        }
    }
}

public class DiceRollValidator : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value.Length == 0) return;

        var parts = value.Split('d');
        if (parts.Length != 2 
            || !Int32.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int numDice)
            || !Int32.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int dieSize))
        {
            errors.Add($"Malformed dice roll");
            return;
        }

        if (dieSize != 4 && dieSize != 6 && dieSize != 8 && dieSize != 10 && dieSize != 12 && dieSize != 20 && dieSize != 100)
        {
            errors.Add($"Invalid die size: {dieSize}");
            return;
        }
    }
}

public class TreasureDropValidator(IStatReferenceValidator validator) : StatStringValidator
{
    public override void Validate(DiagnosticContext ctx, string value, PropertyDiagnosticContainer errors)
    {
        if (value.Length > 2 && value[0] == 'I' && value[1] == '_')
        {
            var item = value.Substring(2);
            if (!validator.IsValidReference(item, "Object") 
                && !validator.IsValidReference(item, "Armor")
                && !validator.IsValidReference(item, "Weapon"))
            {
                errors.Add($"Nonexistent object, armor or weapon: {item}");
            }
        }
        else if (value.Length > 2 && value[0] == 'T' && value[1] == '_')
        {
            var treasureTable = value.Substring(2);
            if (!validator.IsValidReference(treasureTable, "TreasureTable"))
            {
                errors.Add($"Nonexistent treasure table: {treasureTable}");
            }
        }
        else if (!validator.IsValidReference(value, "ObjectCategory"))
        {
            errors.Add($"Nonexistent object category: {value}");
        }
    }
}

public class ObjectListValidator(IPropertyValidator PropertyValidator, StatEntryType ObjectType) : IStatValueValidator
{
    public void Validate(DiagnosticContext ctx, CodeLocation location, object value, PropertyDiagnosticContainer errors)
    {
        var objs = (IEnumerable<object>)value;
        foreach (var subobject in objs)
        {
            // FIXME - pass declaration name from ctx
            PropertyValidator.ValidateEntry(ObjectType, "", (StatDeclaration)subobject, errors);
        }
    }
}

public class AnyParser(IEnumerable<IStatValueValidator> validators, string message = null) : IStatValueValidator
{
    private readonly List<IStatValueValidator> Validators = validators.ToList();

    public void Validate(DiagnosticContext ctx, CodeLocation location, object value, PropertyDiagnosticContainer errors)
    {
        foreach (var validator in Validators)
        {
            errors.Messages?.Clear();
            validator.Validate(ctx, location, value, errors);
            if (errors.Messages == null || errors.Messages.Count == 0) return;
        }

        if (message != null)
        {
            errors.Add(message);
        }
    }
}

public class AnyType
{
    public List<string> Types;
    public string Message;
}

public class StatValueValidatorFactory(IStatReferenceValidator ReferenceValidator, IPropertyValidator PropertyValidator)
{
    public IStatValueValidator CreateReferenceValidator(List<StatReferenceConstraint> constraints)
    {
        return new StatReferenceValidator(ReferenceValidator, constraints);
    }

    public IStatValueValidator CreateValidator(StatField field, StatDefinitionRepository definitions)
    {
        switch (field.Name)
        {
            case "Boosts":
            case "DefaultBoosts":
            case "BoostsOnEquipMainHand":
            case "BoostsOnEquipOffHand":
                return new ExpressionValidator("Properties", definitions, this, ExpressionType.Boost);

            case "TooltipDamage":
            case "TooltipDamageList":
            case "TooltipStatusApply":
            case "TooltipConditionalDamage":
                return new ExpressionValidator("Properties", definitions, this, ExpressionType.DescriptionParams);

            case "DescriptionParams":
            case "ExtraDescriptionParams":
            case "ShortDescriptionParams":
            case "TooltipUpcastDescriptionParams":
                return new ExpressionValidator("DescriptionParams", definitions, this, ExpressionType.DescriptionParams);

            case "ConcentrationSpellID":
            case "CombatAIOverrideSpell":
            case "SpellContainerID":
            case "FollowUpOriginalSpell":
            case "RootSpellID":
                return new StatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "SpellData" }
                ]);

            case "ContainerSpells":
                return new MultiValueStatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "SpellData" }
                ]);

            case "InterruptPrototype":
                return new StatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "InterruptData" }
                ]);

            case "Passives":
            case "PassivesOnEquip":
            case "PassivesMainHand":
            case "PassivesOffHand":
                return new MultiValueStatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "PassiveData" }
                ]);

            case "StatusOnEquip":
            case "StatusInInventory":
                return new MultiValueStatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "StatusData" }
                ]);

            case "Cost":
            case "UseCosts":
            case "DualWieldingUseCosts":
            case "ActionResources":
            case "TooltipUseCosts":
            case "RitualCosts":
            case "HitCosts":
                return new UseCostsValidator(ReferenceValidator);

            case "Damage":
            case "VersatileDamage":
            case "StableRoll":
                return new DiceRollValidator();

            case "Template":
            case "StatusEffectOverride":
            case "StatusEffectOnTurn":
            case "ManagedStatusEffectGroup":
            case "ApplyEffect":
            case "SpellEffect":
            case "StatusEffect":
            case "DisappearEffect":
            case "PreviewEffect":
            case "PositionEffect":
            case "HitEffect":
            case "TargetEffect":
            case "BeamEffect":
            case "CastEffect":
            case "PrepareEffect":
            case "TooltipOnSave":
                return new UUIDValidator();

            case "AmountOfTargets":
                return new LuaExpressionValidator();
        }

        return CreateValidator(field.Type, field.EnumType, field.ReferenceTypes, definitions);
    }

    public IStatValueValidator CreateValidator(string type, StatEnumeration enumType, List<StatReferenceConstraint> constraints, StatDefinitionRepository definitions)
    {
        if (enumType == null && definitions.Enumerations.TryGetValue(type, out StatEnumeration enumInfo) && enumInfo.Values.Count > 0)
        {
            enumType = enumInfo;
        }

        if (enumType != null)
        {
            if (type == "SpellFlagList" 
                || type == "SpellCategoryFlags" 
                || type == "CinematicArenaFlags"
                || type == "RestErrorFlags"
                || type == "AuraFlags"
                || type == "StatusEvent" 
                || type == "AIFlags" 
                || type == "WeaponFlags"
                || type == "ProficiencyGroupFlags"
                || type == "InterruptContext"
                || type == "InterruptDefaultValue"
                || type == "AttributeFlags"
                || type == "PassiveFlags"
                || type == "ResistanceFlags"
                || type == "LineOfSightFlags"
                || type == "StatusPropertyFlags"
                || type == "StatusGroupFlags"
                || type == "StatsFunctorContext")
            {
                return new MultiValueEnumValidator(enumType);
            }
            else
            {
                return new EnumValidator(enumType);
            }
        }

        return type switch
        {
            "Boolean" => new BooleanValidator(),
            "ConstantInt" or "Int" => new Int32Validator(),
            "ConstantFloat" or "Float" => new FloatValidator(),
            "String" or "FixedString" or "TranslatedString" => new StringValidator(),
            "Guid" => new UUIDValidator(),
            "Requirements" => new ExpressionValidator("Requirements", definitions, this, ExpressionType.Functor),
            "StatsFunctors" => new ExpressionValidator("Properties", definitions, this, ExpressionType.Functor),
            "Lua" or "RollConditions" or "TargetConditions" or "Conditions" => new LuaExpressionValidator(),
            "UseCosts" => new UseCostsValidator(ReferenceValidator),
            "StatReference" => new StatReferenceValidator(ReferenceValidator, constraints),
            "StatusId" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["EngineStatusType"]),
                    new StatReferenceValidator(ReferenceValidator,
                    [
                        new StatReferenceConstraint{ StatType = "StatusData" }
                    ])
                }, "Expected a status name"),
            "ResurrectTypes" => new MultiValueEnumValidator(definitions.Enumerations["ResurrectType"]),
            "StatusIdOrGroup" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["StatusGroupFlags"]),
                    new EnumValidator(definitions.Enumerations["EngineStatusType"]),
                    new StatReferenceValidator(ReferenceValidator,
                    [
                        new StatReferenceConstraint{ StatType = "StatusData" }
                    ])
                }, "Expected a status or StatusGroup name"),
            "SummonDurationOrInt" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["SummonDuration"]),
                    new Int32Validator()
                }),
            "AllOrDamageType" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["AllEnum"]),
                    new EnumValidator(definitions.Enumerations["Damage Type"]),
                }),
            "RollAdjustmentTypeOrDamageType" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["RollAdjustmentType"]),
                    new EnumValidator(definitions.Enumerations["Damage Type"]),
                }),
            "AbilityOrAttackRollAbility" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["Ability"]),
                    new EnumValidator(definitions.Enumerations["AttackRollAbility"]),
                }),
            "DamageTypeOrDealDamageWeaponDamageType" => new AnyParser(new List<IStatValueValidator> {
                    new EnumValidator(definitions.Enumerations["Damage Type"]),
                    new EnumValidator(definitions.Enumerations["DealDamageWeaponDamageType"]),
                }),
            "SpellId" => new StatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "SpellData" }
                ]),
            "Interrupt" => new StatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint{ StatType = "InterruptData" }
                ]),
            "TreasureSubtables" => new ObjectListValidator(PropertyValidator, definitions.Types["TreasureSubtable"]),
            "TreasureSubtableObject" => new ObjectListValidator(PropertyValidator, definitions.Types["TreasureSubtableObject"]),
            "TreasureDrop" => new TreasureDropValidator(ReferenceValidator),
            "StatusIDs" =>
                new MultiValueStatReferenceValidator(ReferenceValidator,
                [
                    new StatReferenceConstraint { StatType = "StatusData" }
                ]),
        _ => throw new ArgumentException($"Could not create parser for type '{type}'"),
        };
    }
}
