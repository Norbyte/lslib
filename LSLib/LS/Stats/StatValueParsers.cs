using LSLib.LS.Stats.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS.Stats
{
    public interface IStatValueParser
    {
        object Parse(string value, ref bool succeeded, ref string errorText);
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

    public class BooleanParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == "true" || value == "false" || value == "")
            {
                succeeded = true;
                return (value == "true");
            }
            else
            {
                succeeded = false;
                errorText = "expected boolean value 'true' or 'false'";
                return null;
            }
        }
    }

    public class Int32Parser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == "")
            {
                succeeded = true;
                return 0;
            }
            else if (Int32.TryParse(value, out int intval))
            {
                succeeded = true;
                return intval;
            }
            else
            {
                succeeded = false;
                errorText = "expected an integer value";
                return null;
            }
        }
    }

    public class FloatParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == "")
            {
                succeeded = true;
                return 0.0f;
            }
            else if (Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
            {
                succeeded = true;
                return floatval;
            }
            else
            {
                succeeded = false;
                errorText = "expected a float value";
                return null;
            }
        }
    }

    public class EnumParser : IStatValueParser
    {
        private readonly StatEnumeration Enumeration;

        public EnumParser(StatEnumeration enumeration)
        {
            Enumeration = enumeration ?? throw new ArgumentNullException();
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == null || value == "")
            {
                value = Enumeration.Values[0];
            }

            if (Enumeration.ValueToIndexMap.ContainsKey(value))
            {
                succeeded = true;
                return value;
            }
            else
            {
                succeeded = false;
                if (Enumeration.Values.Count > 4)
                {
                    errorText = "expected one of: " + String.Join(", ", Enumeration.Values.Take(4)) + ", ...";
                }
                else
                {
                    errorText = "expected one of: " + String.Join(", ", Enumeration.Values);
                }
                return null;
            }
        }
    }

    public class MultiValueEnumParser : IStatValueParser
    {
        private readonly EnumParser Parser;

        public MultiValueEnumParser(StatEnumeration enumeration)
        {
            Parser = new EnumParser(enumeration);
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            succeeded = true;

            if (value.Length == 0)
            {
                return true;
            }

            foreach (var item in value.Split(new char[] { ';' }))
            {
                Parser.Parse(item.Trim(new char[] { ' ' }), ref succeeded, ref errorText);
                if (!succeeded)
                {
                    errorText = $"Value '{item}' not supported; {errorText}";
                    return null;
                }
            }

            return value;
        }
    }

    public class StringParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value.Length > 2048)
            {
                errorText = "Value cannot be longer than 2048 characters";
                succeeded = false;
                return null;
            }
            else
            {
                errorText = null;
                succeeded = true;
                return value;
            }
        }
    }

    public class UUIDParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == "")
            {
                succeeded = true;
                return Guid.Empty;
            }
            else if (Guid.TryParseExact(value, "D", out Guid parsed))
            {
                succeeded = true;
                return parsed;
            }
            else
            {
                errorText = $"'{value}' is not a valid UUID";
                succeeded = false;
                return null;
            }
        }
    }

    public class StatReferenceParser : IStatValueParser
    {
        private IStatReferenceValidator Validator;
        private List<StatReferenceConstraint> Constraints;

        public StatReferenceParser(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints)
        {
            Validator = validator;
            Constraints = constraints;
        }
        
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == "")
            {
                succeeded = true;
                return value;
            }

            foreach (var constraint in Constraints)
            {
                if (Validator.IsValidReference(value, constraint.StatType))
                {
                    succeeded = true;
                    return value;
                }
            }

            var refTypes = String.Join("/", Constraints.Select(c => c.StatType));
            errorText = $"'{value}' is not a valid {refTypes} reference";
            succeeded = false;
            return null;
        }
    }

    public class MultiValueStatReferenceParser : IStatValueParser
    {
        private readonly StatReferenceParser Parser;

        public MultiValueStatReferenceParser(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints)
        {
            Parser = new StatReferenceParser(validator, constraints);
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            succeeded = true;

            foreach (var item in value.Split(new char[] { ';' }))
            {
                var trimmed = item.Trim(new char[] { ' ' });
                if (trimmed.Length > 0)
                {
                    Parser.Parse(trimmed, ref succeeded, ref errorText);
                    if (!succeeded)
                    {
                        return null;
                    }
                }
            }

            return value;
        }
    }

    public enum ExpressionType
    {
        Boost,
        Functor,
        DescriptionParams
    };

    public class ExpressionParser : IStatValueParser
    {
        private readonly String ValidatorType;
        private readonly StatDefinitionRepository Definitions;
        private readonly StatValueParserFactory ParserFactory;
        private readonly ExpressionType ExprType;

        public ExpressionParser(String validatorType, StatDefinitionRepository definitions,
            StatValueParserFactory parserFactory, ExpressionType type)
        {
            ValidatorType = validatorType;
            Definitions = definitions;
            ParserFactory = parserFactory;
            ExprType = type;
        }
        
        public virtual object Parse(string value, ref bool succeeded, ref string errorText)
        {
            var valueBytes = Encoding.UTF8.GetBytes("__TYPE_" + ValidatorType + "__ " + value.TrimEnd());
            using (var buf = new MemoryStream(valueBytes))
            {
                List<string> errorTexts = new List<string>();

                var scanner = new StatPropertyScanner();
                scanner.SetSource(buf);
                var parser = new StatPropertyParser(scanner, Definitions, ParserFactory, valueBytes, ExprType);
                parser.OnError += (string message) => errorTexts.Add(message);
                succeeded = parser.Parse();
                if (!succeeded)
                {
                    var location = scanner.LastLocation();
                    var column = location.StartColumn - 10 - ValidatorType.Length + 1;
                    errorText = $"Syntax error at or near character {column}";
                    return null;
                }
                else if (errorTexts.Count > 0)
                {
                    succeeded = false;
                    errorText = String.Join("; ", errorTexts);
                    return null;
                }
                else
                {
                    succeeded = true;
                    return parser.GetParsedObject();
                }
            }
        }
    }

    public class LuaExpressionParser : IStatValueParser
    {
        public virtual object Parse(string value, ref bool succeeded, ref string errorText)
        {
            value = "BHAALS_BOON_SLAYER.Duration-1";
            var valueBytes = Encoding.UTF8.GetBytes(value);
            using (var buf = new MemoryStream(valueBytes))
            {
                var scanner = new Lua.StatLuaScanner();
                scanner.SetSource(buf);
                var parser = new Lua.StatLuaParser(scanner);
                succeeded = parser.Parse();
                if (!succeeded)
                {
                    var location = scanner.LastLocation();
                    errorText = $"Syntax error at or near character {location.StartColumn}";
                    return null;
                }
                else
                {
                    succeeded = true;
                    return null;
                }
            }
        }
    }

    public class UseCostsParser : IStatValueParser
    {
        private readonly IStatReferenceValidator Validator;

        public UseCostsParser(IStatReferenceValidator validator)
        {
            Validator = validator;
        }

        public virtual object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value.Length == 0) return value;

            foreach (var resource in value.Split(';'))
            {
                var res = resource.Trim();
                if (res.Length == 0) continue;

                var parts = res.Split(':');
                if (parts.Length < 2 || parts.Length > 4)
                {
                    errorText = $"Malformed use costs";
                    return null;
                }

                if (!Validator.IsValidGuidResource(parts[0], "ActionResource") && !Validator.IsValidGuidResource(parts[0], "ActionResourceGroup"))
                {
                    errorText = $"Nonexistent action resource or action resource group: {parts[0]}";
                    return null;
                }

                var distanceExpr = parts[1].Split('*');
                if (distanceExpr[0] == "Distance")
                {
                    if (distanceExpr.Length > 1 && !Single.TryParse(distanceExpr[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
                    {
                        errorText = $"Malformed distance multiplier: {distanceExpr[1]}";
                        return null;
                    }

                }
                else if (!Single.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
                {
                    errorText = $"Malformed resource amount: {parts[1]}";
                    return null;
                }

                if (parts.Length == 3 && !Int32.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int intval))
                {
                    errorText = $"Malformed level: {parts[2]}";
                    return null;
                }

                if (parts.Length == 4 && !Int32.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out intval))
                {
                    errorText = $"Malformed level: {parts[3]}";
                    return null;
                }
            }

            succeeded = true;
            return value;
        }
    }

    public class DiceRollParser : IStatValueParser
    {
        public virtual object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value.Length == 0) return value;

            var parts = value.Split('d');
            if (parts.Length != 2 
                || !Int32.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int numDice)
                || !Int32.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int dieSize))
            {
                errorText = $"Malformed dice roll";
                return null;
            }

            if (dieSize != 4 && dieSize != 6 && dieSize != 8 && dieSize != 10 && dieSize != 12 && dieSize != 20 && dieSize != 100)
            {
                errorText = $"Invalid die size: {dieSize}";
                return null;
            }

            succeeded = true;
            return value;
        }
    }

    public class AnyParser : IStatValueParser
    {
        private readonly List<IStatValueParser> Parsers;
        private readonly String Message;

        public AnyParser(IEnumerable<IStatValueParser> parsers, string message = null)
        {
            Parsers = parsers.ToList();
            Message = message;
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            List<string> errors = new List<string>();
            foreach (var parser in Parsers)
            {
                succeeded = false;
                string error = null;
                var result = parser.Parse(value, ref succeeded, ref error);
                if (succeeded)
                {
                    return result;
                }
                else
                {
                    errors.Add(error);
                }
            }

            if (Message != null && Message.Length > 0)
            {
                errorText = $"'{value}': {Message}";
            }
            else
            {
                errorText = String.Join("; ", errors);
            }

            return null;
        }
    }

    public class AnyType
    {
        public List<string> Types;
        public string Message;
    }

    public class StatValueParserFactory
    {
        private readonly IStatReferenceValidator ReferenceValidator;

        public StatValueParserFactory(IStatReferenceValidator referenceValidator)
        {
            ReferenceValidator = referenceValidator;
        }

        public IStatValueParser CreateReferenceParser(List<StatReferenceConstraint> constraints)
        {
            return new StatReferenceParser(ReferenceValidator, constraints);
        }

        public IStatValueParser CreateParser(StatField field, StatDefinitionRepository definitions)
        {
            switch (field.Name)
            {
                case "Boosts":
                case "DefaultBoosts":
                case "BoostsOnEquipMainHand":
                case "BoostsOnEquipOffHand":
                    return new ExpressionParser("Properties", definitions, this, ExpressionType.Boost);

                case "TooltipDamage":
                case "TooltipDamageList":
                case "TooltipStatusApply":
                case "TooltipConditionalDamage":
                    return new ExpressionParser("Properties", definitions, this, ExpressionType.DescriptionParams);

                case "DescriptionParams":
                case "ExtraDescriptionParams":
                case "ShortDescriptionParams":
                case "TooltipUpcastDescriptionParams":
                    return new ExpressionParser("DescriptionParams", definitions, this, ExpressionType.DescriptionParams);

                case "ConcentrationSpellID":
                case "CombatAIOverrideSpell":
                case "SpellContainerID":
                case "FollowUpOriginalSpell":
                case "RootSpellID":
                    return new StatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    {
                        new StatReferenceConstraint{ StatType = "SpellData" }
                    });

                case "ContainerSpells":
                    return new MultiValueStatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    {
                        new StatReferenceConstraint{ StatType = "SpellData" }
                    });

                case "InterruptPrototype":
                    return new StatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    {
                        new StatReferenceConstraint{ StatType = "InterruptData" }
                    });

                case "Passives":
                case "PassivesOnEquip":
                case "PassivesMainHand":
                case "PassivesOffHand":
                    return new MultiValueStatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    {
                        new StatReferenceConstraint{ StatType = "PassiveData" }
                    });

                case "StatusOnEquip":
                case "StatusInInventory":
                    return new MultiValueStatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    {
                        new StatReferenceConstraint{ StatType = "StatusData" }
                    });

                case "Cost":
                case "UseCosts":
                case "DualWieldingUseCosts":
                case "ActionResources":
                case "TooltipUseCosts":
                case "RitualCosts":
                case "HitCosts":
                    return new UseCostsParser(ReferenceValidator);

                case "Damage":
                case "VersatileDamage":
                case "StableRoll":
                    return new DiceRollParser();

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
                    return new UUIDParser();

                case "AmountOfTargets":
                    return new LuaExpressionParser();
            }

            return CreateParser(field.Type, field.EnumType, field.ReferenceTypes, definitions);
        }

        public IStatValueParser CreateParser(string type, StatEnumeration enumType, List<StatReferenceConstraint> constraints, StatDefinitionRepository definitions)
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
                    return new MultiValueEnumParser(enumType);
                }
                else
                {
                    return new EnumParser(enumType);
                }
            }

            switch (type)
            {
                case "Boolean":
                    return new BooleanParser();

                case "ConstantInt":
                case "Int":
                    return new Int32Parser();

                case "ConstantFloat":
                case "Float":
                    return new FloatParser();

                case "String":
                case "FixedString":
                // FIXME - add TranslatedStringParser "guid;ver"
                case "TranslatedString":
                    return new StringParser();

                case "Guid":
                    return new UUIDParser();

                case "Requirements":
                    return new ExpressionParser("Requirements", definitions, this, ExpressionType.Functor);

                case "StatsFunctors":
                    return new ExpressionParser("Properties", definitions, this, ExpressionType.Functor);

                case "Lua":
                case "RollConditions":
                case "TargetConditions":
                case "Conditions":
                    return new LuaExpressionParser();

                case "UseCosts":
                    return new UseCostsParser(ReferenceValidator);

                case "StatReference":
                    return new StatReferenceParser(ReferenceValidator, constraints);

                case "StatusId":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["EngineStatusType"]),
                        new StatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                        {
                            new StatReferenceConstraint{ StatType = "StatusData" }
                        })
                    }, "Expected a status name");

                case "ResurrectTypes":
                    return new MultiValueEnumParser(definitions.Enumerations["ResurrectType"]);

                case "StatusIdOrGroup":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["StatusGroupFlags"]),
                        new EnumParser(definitions.Enumerations["EngineStatusType"]),
                        new StatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                        {
                            new StatReferenceConstraint{ StatType = "StatusData" }
                        })
                    }, "Expected a status or StatusGroup name");

                case "SummonDurationOrInt":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["SummonDuration"]),
                        new Int32Parser()
                    });

                case "AllOrDamageType":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["AllEnum"]),
                        new EnumParser(definitions.Enumerations["Damage Type"]),
                    });

                case "RollAdjustmentTypeOrDamageType":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["RollAdjustmentType"]),
                        new EnumParser(definitions.Enumerations["Damage Type"]),
                    });

                case "AbilityOrAttackRollAbility":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["Ability"]),
                        new EnumParser(definitions.Enumerations["AttackRollAbility"]),
                    });

                case "DamageTypeOrDealDamageWeaponDamageType":
                    return new AnyParser(new List<IStatValueParser> {
                        new EnumParser(definitions.Enumerations["Damage Type"]),
                        new EnumParser(definitions.Enumerations["DealDamageWeaponDamageType"]),
                    });

                case "SpellId":
                    return new StatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    { 
                        new StatReferenceConstraint{ StatType = "SpellData" }
                    });

                case "Interrupt":
                    return new StatReferenceParser(ReferenceValidator, new List<StatReferenceConstraint>
                    { 
                        new StatReferenceConstraint{ StatType = "InterruptData" }
                    });

                // THESE NEED TO BE FIXED!
                case "StatusIDs":
                    return new StringParser();

                default:
                    throw new ArgumentException($"Could not create parser for type '{type}'");
            }
        }
    }
}