using LSLib.LS.Story.GoalParser;
using LSLib.LS.Story.HeaderParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story.Compiler
{
    /// <summary>
    /// Type declaration
    /// </summary>
    public class ValueType
    {
        // Type ID
        public uint TypeId;
        // Osiris builtin type ID
        public Value.Type IntrinsicTypeId;
        // Type name
        public String Name;
    }

    /// <summary>
    /// Parameter direction. 
    /// Only relevant for queries, which get both input and output parameters.
    /// </summary>
    public enum ParamDirection
    {
        In,
        Out
    }

    /// <summary>
    /// Osiris internal function type.
    /// </summary>
    public enum FunctionType
    {
        // Osiris items
        // Query defined by the Osiris runtime
        SysQuery,
        // Call defined by the Osiris runtime
        SysCall,

        // Application defined items
        // Event defined by the application (D:OS)
        Event,
        // Query defined by the application (D:OS)
        Query,
        // Call defined by the application (D:OS)
        Call,

        // User-defined items
        // Proc (~call) defined in user code
        Proc,
        // Query defined in user code
        UserQuery,
        // Database defined in user code
        Database
    };
    
    /// <summary>
    /// Function name
    /// In Osiris, multiple functions are allowed with the same name,
    /// if they have different arity (number of parameters).
    /// </summary>
    public class FunctionNameAndArity : IEquatable<FunctionNameAndArity>
    {
        // Function name
        public readonly String Name;
        // Number of parameters
        public readonly int Arity;

        public FunctionNameAndArity(String name, int arity)
        {
            Name = name;
            Arity = arity;
        }

        public override bool Equals(object fun)
        {
            return Equals(fun as FunctionNameAndArity);
        }

        public bool Equals(FunctionNameAndArity fun)
        {
            return Name.ToLower() == fun.Name.ToLower() 
                && Arity == fun.Arity;
        }

        public override int GetHashCode()
        {
            return Name.ToLower().GetHashCode() | Arity;
        }

        public override string ToString()
        {
            return Name + "(" + Arity.ToString() + ")";
        }
    }

    /// <summary>
    /// Function parameter (or database column, depending on the function type)
    /// </summary>
    public class FunctionParam
    {
        // Parameter direction, i.e. either In or Out.
        public ParamDirection Direction;
        // Parameter type
        // For builtin functions this is always taken from the function header.
        // For user defined functions this is inferred from code.
        public ValueType Type;
        // Parameter name
        public String Name;
    }
    
    public class FunctionSignature
    {
        // Type of function (call, query, database, etc.)
        public FunctionType Type;
        // Function name
        public String Name;
        // List of arguments
        public List<FunctionParam> Params;
        // Indicates that we were able to infer the type of all parameters
        public Boolean FullyTyped;

        public FunctionNameAndArity GetNameAndArity() => new FunctionNameAndArity(Name, Params.Count);
    }

    /// <summary>
    /// Metadata for built-in functions
    /// </summary>
    public class BuiltinFunction
    {
        public FunctionSignature Signature;
        // Metadata passed from story headers.
        // These aren't used at all during compilation and are only used in the compiled story file.
        public UInt32 Meta1;
        public UInt32 Meta2;
        public UInt32 Meta3;
        public UInt32 Meta4;
    }

    public class CodeLocation
    {
        public String Path;
        public int StartLine, EndLine;
        public int StartColumn, EndColumn;
    }

    /// <summary>
    /// Diagnostic message level
    /// </summary>
    public enum MessageLevel
    {
        Error,
        Warning
    }

    /// <summary>
    /// Holder for compiler diagnostic codes.
    /// </summary>
    public class DiagnosticCode
    {
        public const String TypeIdAlreadyDefined = "01";
        public const String TypeNameAlreadyDefined = "02";
        public const String TypeIdInvalid = "03";
        public const String IntrinsicTypeIdInvalid = "04";

        public const String SignatureAlreadyDefined = "05";
        public const String UnresolvedTypeInSignature = "06";
        public const String GoalAlreadyDefined = "07";

        public const String UnresolvedGoal = "08";
        public const String UnresolvedLocalType = "09";
        public const String UnresolvedSignature = "10";
        public const String LocalTypeMismatch = "11";
        public const String UnresolvedType = "12";
        public const String InvalidProcDefinition = "13";
        public const String InvalidSymbolInFact = "14";
        public const String InvalidSymbolInStatement = "15";
        public const String CanOnlyDeleteFromDatabase = "16";
        public const String InvalidSymbolInInitialCondition = "17";
        public const String InvalidSymbolInCondition = "18";
        public const String UnresolvedSymbol = "19";
        public const String StringLtGtComparison = "20";
        public const String GuidAliasMismatch = "21";
        public const String GuidPrefixNotKnown = "22";
        public const String RuleNamingStyle = "23";
    }

    public class Diagnostic
    {
        public readonly CodeLocation Location;
        public readonly MessageLevel Level;
        public readonly String Code;
        public readonly String Message;

        public Diagnostic(CodeLocation location, MessageLevel level, String code, String message)
        {
            Location = location;
            Level = level;
            Code = code;
            Message = message;
        }
    }

    public class CompilationLog
    {
        public List<Diagnostic> Log = new List<Diagnostic>();

        public void Warn(CodeLocation Location, String code, String message)
        {
            var diag = new Diagnostic(Location, MessageLevel.Warning, code, message);
            Log.Add(diag);
        }

        public void Error(CodeLocation Location, String code, String message)
        {
            var diag = new Diagnostic(Location, MessageLevel.Error, code, message);
            Log.Add(diag);
        }
    }

    /// <summary>
    /// Compilation context that holds input and intermediate data used during the compilation process.
    /// </summary>
    public class CompilationContext
    {
        public const uint MaxIntrinsicTypeId = 5;

        public Dictionary<uint, ValueType> TypesById = new Dictionary<uint, ValueType>();
        public Dictionary<String, ValueType> TypesByName = new Dictionary<String, ValueType>();
        public Dictionary<String, IRGoal> GoalsByName = new Dictionary<String, IRGoal>();
        public Dictionary<FunctionNameAndArity, FunctionSignature> Signatures = new Dictionary<FunctionNameAndArity, FunctionSignature>();
        public Dictionary<FunctionNameAndArity, object> Functions = new Dictionary<FunctionNameAndArity, object>();
        public CompilationLog Log = new CompilationLog();

        public CompilationContext()
        {
            RegisterIntrinsicTypes();
        }

        /// <summary>
        /// Registers all Osiris builtin types that are not declared in the story header separately
        /// </summary>
        private void RegisterIntrinsicTypes()
        {
            var tUnknown = new ValueType
            {
                Name = "UNKNOWN",
                TypeId = 0,
                IntrinsicTypeId = Value.Type.Unknown
            };
            AddType(tUnknown);

            var tInteger = new ValueType
            {
                Name = "INTEGER",
                TypeId = 1,
                IntrinsicTypeId = Value.Type.Integer
            };
            AddType(tInteger);
            
            var tInteger64 = new ValueType
            {
                Name = "INTEGER64",
                TypeId = 2,
                IntrinsicTypeId = Value.Type.Integer64
            };
            AddType(tInteger64);
            
            var tReal = new ValueType
            {
                Name = "REAL",
                TypeId = 3,
                IntrinsicTypeId = Value.Type.Float
            };
            AddType(tReal);
            
            var tString = new ValueType
            {
                Name = "STRING",
                TypeId = 4,
                IntrinsicTypeId = Value.Type.String
            };
            AddType(tString);

            var tGuidString = new ValueType
            {
                Name = "GUIDSTRING",
                TypeId = 5,
                IntrinsicTypeId = Value.Type.GuidString
            };
            AddType(tGuidString);
        }

        private void AddType(ValueType type)
        {
            TypesById.Add(type.TypeId, type);
            TypesByName.Add(type.Name, type);
        }

        public bool RegisterType(ValueType type)
        {
            if (TypesById.ContainsKey(type.TypeId))
            {
                Log.Error(null, DiagnosticCode.TypeIdAlreadyDefined, "Type ID already in use");
                return false;
            }

            if (TypesByName.ContainsKey(type.Name))
            {
                Log.Error(null, DiagnosticCode.TypeNameAlreadyDefined, "Type name already in use");
                return false;
            }

            if (type.TypeId < MaxIntrinsicTypeId || type.TypeId > 255)
            {
                Log.Error(null, DiagnosticCode.TypeIdInvalid, "Type ID must be in the range 5..255");
                return false;
            }

            if (type.IntrinsicTypeId <= 0 || (uint)type.IntrinsicTypeId > MaxIntrinsicTypeId)
            {
                Log.Error(null, DiagnosticCode.TypeIdInvalid, "Alias type ID must refer to an intrinsic type");
                return false;
            }

            AddType(type);
            return true;
        }

        public bool RegisterFunction(FunctionSignature signature, object func)
        {
            var nameAndArity = signature.GetNameAndArity();
            if (Signatures.ContainsKey(nameAndArity))
            {
                Log.Error(null, DiagnosticCode.SignatureAlreadyDefined, 
                    String.Format("Signature already registered: {0}({1})", nameAndArity.Name, nameAndArity.Arity));
                return false;
            }

            Signatures.Add(nameAndArity, signature);
            Functions.Add(nameAndArity, func);
            return true;
        }

        public bool RegisterGoal(IRGoal goal)
        {
            if (GoalsByName.ContainsKey(goal.Name))
            {
                Log.Error(null, DiagnosticCode.GoalAlreadyDefined,
                    String.Format("Goal already registered: {0}", goal.Name));
                return false;
            }

            GoalsByName.Add(goal.Name, goal);
            return true;
        }

        public ValueType LookupType(String typeName)
        {
            if (TypesByName.TryGetValue(typeName, out ValueType type))
            {
                return type;
            }
            else
            {
                return null;
            }
        }

        public FunctionSignature LookupSignature(FunctionNameAndArity name)
        {
            if (Signatures.TryGetValue(name, out FunctionSignature signature))
            {
                return signature;
            }
            else
            {
                return null;
            }
        }

        public object LookupName(FunctionNameAndArity name)
        {
            if (Functions.TryGetValue(name, out object function))
            {
                return function;
            }
            else
            {
                return null;
            }
        }

        public IRGoal LookupGoal(String name)
        {
            if (GoalsByName.TryGetValue(name, out IRGoal goal))
            {
                return goal;
            }
            else
            {
                return null;
            }
        }
    }



    public class NameReferences
    {
        private CompilationContext Context;
        public Dictionary<FunctionNameAndArity, HashSet<object>> References = new Dictionary<FunctionNameAndArity, HashSet<object>>();

        public NameReferences(CompilationContext context)
        {
            Context = context;
        }

        public void Add(FunctionNameAndArity fun, object reference)
        {
            // We only track references to symbols that are not yet fully typed or declared.
            var signature = Context.LookupSignature(fun);
            if (signature != null && signature.FullyTyped)
            {
                return;
            }

            HashSet<object> references;
            if (!References.TryGetValue(fun, out references))
            {
                references = new HashSet<object>();
                References.Add(fun, references);
            }

            references.Add(reference);
        }
    }

    public class Compiler
    {
        public CompilationContext Context = new CompilationContext();

        private void CollectNameReferences(NameReferences references, IRFact fact)
        {
            references.Add(fact.Database.Name, fact);
        }

        private void CollectNameReferences(NameReferences references, IRRule rule, IRStatement statement)
        {
            if (statement.Func != null)
            {
                references.Add(statement.Func.Name, rule);
            }
        }

        private void CollectNameReferences(NameReferences references, IRRule rule, IRCondition condition)
        {
            if (condition is IRFuncCondition)
            {
                var funcCond = condition as IRFuncCondition;
                references.Add(funcCond.Func.Name, rule);
            }
        }

        private void CollectNameReferences(NameReferences references, IRRule rule)
        {
            foreach (var condition in rule.Conditions)
            {
                CollectNameReferences(references, rule, condition);
            }

            foreach (var action in rule.Actions)
            {
                CollectNameReferences(references, rule, action);
            }
        }

        private void CollectNameReferences(NameReferences references, IRGoal goal)
        {
            foreach (var fact in goal.InitSection)
            {
                CollectNameReferences(references, fact);
            }

            foreach (var rule in goal.KBSection)
            {
                CollectNameReferences(references, rule);
            }

            foreach (var fact in goal.ExitSection)
            {
                CollectNameReferences(references, fact);
            }
        }

        private NameReferences CollectNameReferences()
        {
            var references = new NameReferences(Context);
            foreach (var goal in Context.GoalsByName.Values)
            {
                CollectNameReferences(references, goal);
            }

            return references;
        }

        public void ResolveNames()
        {
            var references = CollectNameReferences();
        }

        private void VerifyIRFact(IRFact fact)
        {
            var db = Context.LookupSignature(fact.Database.Name);
            if (db == null)
            {
                var message = String.Format("Database \"{0}\" could not be resolved", fact.Database.Name);
                Context.Log.Error(null, DiagnosticCode.UnresolvedSignature, message);
                return;
            }

            if (db.Type != FunctionType.Database
                && db.Type != FunctionType.Call
                && db.Type != FunctionType.SysCall
                && db.Type != FunctionType.Proc)
            {
                var message = String.Format("Init/Exit actions can only reference databases, calls and PROCs; \"{0}\" is a {1}", fact.Database.Name, db.Type);
                Context.Log.Error(null, DiagnosticCode.InvalidSymbolInFact, message);
                return;
            }

            int index = 0;
            foreach (var param in db.Params)
            {
                var ele = fact.Elements[index];
                index++;

                if (ele.Type == null)
                {
                    // TODO - propagate types in binary LHS/RHS and FuncCondition parameters
                    var message = String.Format("No type information available for fact arg");
                    // TODO separate code
                    Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }

                if (param.Type.IntrinsicTypeId != ele.Type.IntrinsicTypeId)
                {
                    var message = String.Format("Intrinsic type of column {0} of {1} \"{2}\" differs: {3} vs {4}", 
                        index, db.Type, db.Name, param.Type.IntrinsicTypeId, ele.Type.IntrinsicTypeId);
                    // TODO separate code
                    Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }

                if (IsGuidAliasToAliasCast(param.Type, ele.Type))
                {
                    var message = String.Format("GUID alias cast of column {0} of {1} \"{2}\" differs: {3} vs {4}",
                        index, db.Type, db.Name, param.Type.TypeId, ele.Type.TypeId);
                    // TODO separate code
                    Context.Log.Warn(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }
            }
        }

        private void VerifyIRStatement(IRRule rule, IRStatement statement)
        {
            if (statement.Func == null) return;

            var func = Context.LookupSignature(statement.Func.Name);
            if (func == null)
            {
                var message = String.Format("Symbol \"{0}\" could not be resolved", statement.Func.Name);
                Context.Log.Error(null, DiagnosticCode.UnresolvedSignature, message);
                return;
            }

            if (func.Type != FunctionType.Database
                && func.Type != FunctionType.Call
                && func.Type != FunctionType.SysCall
                && func.Type != FunctionType.Proc)
            {
                var message = String.Format("KB rule actions can only reference databases, calls and PROCs; \"{0}\" is a {1}", 
                    statement.Func.Name, func.Type);
                Context.Log.Error(null, DiagnosticCode.InvalidSymbolInStatement, message);
                return;
            }

            if (statement.Not
                && func.Type != FunctionType.Database)
            {
                var message = String.Format("KB rule NOT actions can only reference databases; \"{0}\" is a {1}",
                    statement.Func.Name, func.Type);
                Context.Log.Error(null, DiagnosticCode.CanOnlyDeleteFromDatabase, message);
                return;
            }

            int index = 0;
            foreach (var param in func.Params)
            {
                var ele = statement.Params[index];
                index++;

                ValueType type = ele.Type;
                if (type == null)
                {
                    // TODO - propagate types in binary LHS/RHS and FuncCondition parameters
                    var message = String.Format("No type information available for statement arg");
                    // TODO separate code
                    Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }
                
                VerifyIRValue(rule, ele);

                if (param.Type.IntrinsicTypeId != type.IntrinsicTypeId)
                {
                    var message = String.Format("Intrinsic type of parameter {0} of Func [TODO TYPE] {1} differs: {2} vs {3}",
                        index, func.Name, param.Type.IntrinsicTypeId, type.IntrinsicTypeId);
                    // TODO separate code
                    Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }

                if (IsGuidAliasToAliasCast(param.Type, type))
                {
                    var message = String.Format("GUID alias cast: parameter {0} of Func [TODO TYPE] {1} differs: {2} vs {3}",
                        index, func.Name, param.Type.TypeId, type.TypeId);
                    // TODO separate code
                    Context.Log.Warn(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }
            }
        }

        private void VerifyIRVariable(IRRule rule, IRVariable variable)
        {
            var ruleVar = rule.Variables[variable.Index];
            if (!AreIntrinsicTypesCompatible(ruleVar.Type.IntrinsicTypeId, variable.Type.IntrinsicTypeId))
            {
                var message = String.Format("Rule variable {0} of type {1} cannot be casted to {2}",
                    ruleVar.Name, ruleVar.Type.IntrinsicTypeId, variable.Type.IntrinsicTypeId);
                // TODO separate code
                Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                return;
            }

            if (IsGuidAliasToAliasCast(ruleVar.Type, variable.Type))
            {
                var message = String.Format("GUID alias cast: Rule variable {0} of type {1} casted to {2}",
                    ruleVar.Name, ruleVar.Type.TypeId, variable.Type.TypeId);
                // TODO separate code
                Context.Log.Warn(null, DiagnosticCode.LocalTypeMismatch, message);
            }
        }

        private void VerifyIRConstant(IRConstant constant)
        {
            if (constant.Type.IntrinsicTypeId == Value.Type.GuidString
                && constant.Type.TypeId > CompilationContext.MaxIntrinsicTypeId)
            {
                // Check if the value is prefixed by any of the known GUID subtypes.
                // If a match is found, verify that the type of the constant matched the GUID subtype.
                var underscore = constant.StringValue.IndexOf('_');
                if (underscore != -1)
                {
                    var prefix = constant.StringValue.Substring(0, underscore);
                    var type = Context.LookupType(prefix);
                    if (type != null)
                    {
                        if (type.TypeId != constant.Type.TypeId)
                        {
                            var message = String.Format("GUID constant \"{0}\" has inferred type {1}",
                                constant.StringValue, constant.Type.Name);
                            Context.Log.Warn(null, DiagnosticCode.GuidAliasMismatch, message);
                        }
                    }
                    else if (prefix.Contains("GUID"))
                    {
                        var message = String.Format("GUID constant \"{0}\" is prefixed with unknown type {1}",
                            constant.StringValue, prefix);
                        Context.Log.Warn(null, DiagnosticCode.GuidPrefixNotKnown, message);
                    }
                }
            }
        }

        private void VerifyIRValue(IRRule rule, IRValue value)
        {
            if (value is IRConstant)
            {
                VerifyIRConstant(value as IRConstant);
            }
            else
            {
                VerifyIRVariable(rule, value as IRVariable);
            }
        }

        private void VerifyIRFuncCondition(IRRule rule, IRFuncCondition condition, bool initialCondition)
        {
            // TODO - Merge FuncCondition and IRStatement base?
            // Base --> IRParameterizedCall --> FuncCond: has (NOT) field
            var func = Context.LookupSignature(condition.Func.Name);
            if (func == null)
            {
                var message = String.Format("Symbol \"{0}\" could not be resolved", condition.Func.Name);
                Context.Log.Error(null, DiagnosticCode.UnresolvedSymbol, message);
                return;
            }

            if (!func.FullyTyped)
            {
                var message = String.Format("Signature of \"{0}\" could not be determined", condition.Func.Name);
                Context.Log.Error(null, DiagnosticCode.UnresolvedSignature, message);
                return;
            }

            if (initialCondition)
            {
                switch (rule.Type)
                {
                    case RuleType.Proc:
                        if (func.Type != FunctionType.Proc)
                        {
                            var message = String.Format("Initial proc condition can only be a PROC name; \"{0}\" is a {1}",
                                condition.Func.Name, func.Type);
                            Context.Log.Error(null, DiagnosticCode.InvalidSymbolInInitialCondition, message);
                            return;
                        }
                        break;

                    case RuleType.Query:
                        if (func.Type != FunctionType.UserQuery)
                        {
                            var message = String.Format("Initial query condition can only be a user-defined QRY name; \"{0}\" is a {1}",
                                condition.Func.Name, func.Type);
                            Context.Log.Error(null, DiagnosticCode.InvalidSymbolInInitialCondition, message);
                            return;
                        }
                        break;

                    case RuleType.Rule:
                        if (func.Type != FunctionType.Event
                            && func.Type != FunctionType.Database)
                        {
                            var message = String.Format("Initial rule condition can only be an event or a DB; \"{0}\" is a {1}",
                                condition.Func.Name, func.Type);
                            Context.Log.Error(null, DiagnosticCode.InvalidSymbolInInitialCondition, message);
                            return;
                        }
                        break;

                    default:
                        throw new Exception("Unknown rule type");
                }
            }
            else
            {
                if (func.Type != FunctionType.SysQuery
                    && func.Type != FunctionType.Query
                    && func.Type != FunctionType.Database
                    && func.Type != FunctionType.UserQuery)
                {
                    var message = String.Format("Subsequent rule conditions can only be queries or DBs; \"{0}\" is a {1}",
                        condition.Func.Name, func.Type);
                    Context.Log.Error(null, DiagnosticCode.InvalidSymbolInCondition, message);
                    return;
                }
            }

            int index = 0;
            foreach (var param in func.Params)
            {
                var condParam = condition.Params[index];
                ValueType type = condParam.Type;
                index++;

                if (type == null)
                {
                    // TODO - propagate types in binary LHS/RHS and FuncCondition parameters
                    var message = String.Format("No type information available for func condition arg");
                    // TODO separate code
                    Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }

                VerifyIRValue(rule, condParam);

                if (param.Type.IntrinsicTypeId != type.IntrinsicTypeId)
                {
                    var message = String.Format("Intrinsic type of parameter {0} of {1} \"{2}\" differs: {3} vs {4}",
                        index, func.Type, func.Name, param.Type.IntrinsicTypeId, type.IntrinsicTypeId);
                    // TODO separate code
                    Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }

                if (IsGuidAliasToAliasCast(param.Type, type))
                {
                    var message = String.Format("GUID alias cast of parameter {0} of {1} \"{2}\": {3} vs {4}",
                        index, func.Type, func.Name, param.Type.TypeId, type.TypeId);
                    // TODO separate code
                    Context.Log.Warn(null, DiagnosticCode.LocalTypeMismatch, message);
                    continue;
                }
            }
        }

        private Value.Type IntrinsicTypeToCompatibilityType(Value.Type typeId)
        {
            switch ((Value.Type)typeId)
            {
                case Value.Type.Integer:
                case Value.Type.Integer64:
                case Value.Type.Float:
                    return Value.Type.Integer;

                case Value.Type.String:
                    return Value.Type.String;

                case Value.Type.GuidString:
                    return Value.Type.GuidString;

                default:
                    throw new ArgumentException("Cannot check compatibility of unknown types");
            }
        }

        private bool AreIntrinsicTypesCompatible(Value.Type type1, Value.Type type2)
        {
            Value.Type translatedType1 = IntrinsicTypeToCompatibilityType(type1),
                translatedType2 = IntrinsicTypeToCompatibilityType(type2);
            return translatedType1 == translatedType2;
        }

        private bool IsGuidAliasToAliasCast(ValueType type1, ValueType type2)
        {
            return
                type1.IntrinsicTypeId == type2.IntrinsicTypeId
                && type1.IntrinsicTypeId == Value.Type.GuidString
                && type1.TypeId != (int)Value.Type.GuidString
                && type2.TypeId != (int)Value.Type.GuidString
                && type1.TypeId != type2.TypeId;
        }

        private void VerifyIRBinaryCondition(IRRule rule, IRBinaryCondition condition)
        {
            ValueType lhs = condition.LValue.Type, 
                rhs = condition.RValue.Type;

            // Don't raise compiler errors if the untyped value is a variable,
            // as we already have a separate rule-level error for untyped variables.
            if ((lhs == null && condition.LValue is IRVariable)
                || (rhs == null && condition.RValue is IRVariable))
            {
                return;
            }

            VerifyIRValue(rule, condition.LValue);
            VerifyIRValue(rule, condition.RValue);

            if (!AreIntrinsicTypesCompatible(lhs.IntrinsicTypeId, rhs.IntrinsicTypeId))
            {
                var message = String.Format("Intrinsic type of LHS/RHS differs: {0} vs {1}",
                    lhs.IntrinsicTypeId, rhs.IntrinsicTypeId);
                // TODO separate code
                Context.Log.Error(null, DiagnosticCode.LocalTypeMismatch, message);
                return;
            }

            if (IsGuidAliasToAliasCast(lhs, rhs))
            {
                var message = String.Format("GUID alias cast - LHS/RHS differs: {0} vs {1}",
                    lhs.TypeId, rhs.TypeId);
                // TODO separate code
                Context.Log.Warn(null, DiagnosticCode.LocalTypeMismatch, message);
                return;
            }

            // Using greater than/less than operators for strings and GUIDs is probably a mistake.
            if ((lhs.IntrinsicTypeId == Value.Type.String
                || lhs.IntrinsicTypeId == Value.Type.GuidString)
                && (condition.Op == RelOpType.Greater
                || condition.Op == RelOpType.GreaterOrEqual
                || condition.Op == RelOpType.Less
                || condition.Op == RelOpType.LessOrEqual))
            {
                var message = String.Format("String comparison using operator {0} - probably a mistake?", condition.Op);
                Context.Log.Warn(null, DiagnosticCode.StringLtGtComparison, message);
                return;
            }
        }

        private void VerifyIRRule(IRRule rule)
        {
            if (rule.Type == RuleType.Proc || rule.Type == RuleType.Query)
            {
                var initialName = (rule.Conditions[0] as IRFuncCondition).Func.Name;
                if (rule.Type == RuleType.Proc && initialName.Name.Substring(0, 4).ToUpper() != "PROC")
                {
                    var message = String.Format("Name of PROC \"{0}\" should start with the prefix \"PROC\"", initialName);
                    Context.Log.Warn(null, DiagnosticCode.RuleNamingStyle, message);
                }

                if (rule.Type == RuleType.Query && initialName.Name.Substring(0, 3).ToUpper() != "QRY")
                {
                    var message = String.Format("Name of Query \"{0}\" should start with the prefix \"QRY\"", initialName);
                    Context.Log.Warn(null, DiagnosticCode.RuleNamingStyle, message);
                }
            }

            bool initialCondition = true;
            foreach (var condition in rule.Conditions)
            {
                if (condition is IRBinaryCondition)
                {
                    VerifyIRBinaryCondition(rule, condition as IRBinaryCondition);
                }
                else
                {
                    VerifyIRFuncCondition(rule, condition as IRFuncCondition, initialCondition);
                }

                initialCondition = false;
            }

            foreach (var action in rule.Actions)
            {
                VerifyIRStatement(rule, action);
            }

            foreach (var variable in rule.Variables)
            {
                if (variable.Type == null)
                {
                    var message = String.Format("Variable \"{0}\" of rule could not be typed",
                        variable.Name);
                    Context.Log.Error(null, DiagnosticCode.UnresolvedLocalType, message);
                }
            }
        }

        public void VerifyIR()
        {
            foreach (var goal in Context.GoalsByName.Values)
            {
                foreach (var parentGoal in goal.ParentTargetEdges)
                {
                    if (Context.LookupGoal(parentGoal.Name) == null)
                    {
                        var message = String.Format("Parent goal of \"{0}\" could not be resolved: \"{1}\"", goal.Name, parentGoal.Name);
                        Context.Log.Error(null, DiagnosticCode.UnresolvedGoal, message);
                    }
                }

                foreach (var fact in goal.InitSection)
                {
                    VerifyIRFact(fact);
                }

                foreach (var rule in goal.KBSection)
                {
                    VerifyIRRule(rule);
                }

                foreach (var fact in goal.ExitSection)
                {
                    VerifyIRFact(fact);
                }
            }
        }

        private ValueType ConstantTypeToValueType(IRConstantType type)
        {
            switch (type)
            {
                case IRConstantType.Unknown: return null;
                // TODO - lookup type ID from enum
                case IRConstantType.Integer: return Context.TypesById[1];
                case IRConstantType.Float: return Context.TypesById[3];
                case IRConstantType.String: return Context.TypesById[4];
                case IRConstantType.Name: return Context.TypesById[5];
                default: throw new ArgumentException("Invalid IR constant type");
            }
        }

        private ValueType DetermineSignature(IRConstant value)
        {
            var irConst = value as IRConstant;
            if (irConst.Type != null)
            {
                return Context.LookupType(irConst.Type.Name);
            }
            else
            {
                return ConstantTypeToValueType(irConst.ValueType);
            }
        }

        private ValueType DetermineSignature(IRRule rule, IRValue value)
        {
            if (value is IRConstant)
            {
                return DetermineSignature(value as IRConstant);
            }
            else if (value is IRVariable)
            {
                var irVar = value as IRVariable;
                var ruleVar = rule.Variables[irVar.Index];
                if (ruleVar.Type != null)
                {
                    return Context.LookupType(ruleVar.Type.Name);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                throw new ArgumentException("Invalid IR value type");
            }
        }


        private bool ApplySignature(FunctionNameAndArity name, FunctionType? type, List<ValueType> paramTypes)
        {
            var registeredSignature = Context.LookupSignature(name);
            var signature = registeredSignature;
            if (signature != null && signature.FullyTyped)
            {
                throw new InvalidOperationException("Cannot apply signature to an already typed name");
            }

            if (signature == null)
            {
                signature = new FunctionSignature
                {
                    Name = name.Name,
                    Type = (type == null) ? FunctionType.Database : (FunctionType)type
                };
            }
            else
            {
                if (type != null && signature.Type != type)
                {
                    var message = String.Format("Auto-typing name {0}: first seen as {1}, now seen as {2}",
                        name, signature.Type, type);
                    // TODO error code!
                    Context.Log.Error(null, DiagnosticCode.InvalidProcDefinition, message);
                }
            }

            signature.FullyTyped = !paramTypes.Any(ty => ty == null);
            signature.Params = new List<FunctionParam>();
            foreach (var paramType in paramTypes)
            {
                var sigParam = new FunctionParam
                {
                    Type = paramType,
                    Direction = ParamDirection.In,
                    Name = null
                };
                signature.Params.Add(sigParam);
            }
            
            if (registeredSignature == null)
            {
                Context.RegisterFunction(signature, null);
            }

            return signature.FullyTyped;
        }

        private bool TryPropagateSignature(IRRule rule, FunctionNameAndArity name, FunctionType? type, List<IRValue> parameters, bool allowPartial)
        {
            // Build a signature with all parameters to make sure that all types can be resolved
            var sig = new List<ValueType>();
            foreach (var param in parameters)
            {
                var paramSignature = DetermineSignature(rule, param);
                if (paramSignature != null)
                {
                    sig.Add(paramSignature);
                }
                else
                {
                    if (allowPartial)
                    {
                        sig.Add(null);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Apply signature to symbol
            return ApplySignature(name, type, sig);
        }

        private bool TryPropagateSignature(FunctionNameAndArity name, FunctionType? type, List<IRConstant> parameters)
        {
            // Build a signature with all parameters to make sure that all types can be resolved
            var sig = new List<ValueType>();
            foreach (var param in parameters)
            {
                var paramSignature = DetermineSignature(param);
                sig.Add(paramSignature);
            }

            // Apply signature to symbol
            ApplySignature(name, type, sig);

            return true;
        }

        private bool PropagateSignatureIfRequired(IRRule rule, FunctionNameAndArity name, FunctionType? type, List<IRValue> parameters, bool allowPartial)
        {
            var signature = Context.LookupSignature(name);
            bool signatureOk = (signature != null && signature.FullyTyped);
            if (!signatureOk && TryPropagateSignature(rule, name, type, parameters, allowPartial))
            {
                signature = Context.LookupSignature(name);
                signatureOk = signature.FullyTyped;
            }

            if (signatureOk)
            {
                PropagateRuleTypesFromParamList(rule, parameters, signature);
            }

            return signatureOk;
        }

        private bool PropagateSignatureIfRequired(FunctionNameAndArity name, FunctionType? type, List<IRConstant> parameters)
        {
            var signature = Context.LookupSignature(name);
            if (signature == null || !signature.FullyTyped)
            {
                return TryPropagateSignature(name, type, parameters);
            }
            else
            {
                return true;
            }
        }

        private void PropagateIRVariableType(IRRule rule, IRVariable variable, ValueType type)
        {
            if (variable.Type == null)
            {
                variable.Type = type;
            }
            else
            {
                // TODO - check for conflicts? shouldn't be possible
            }

            var ruleVar = rule.Variables[variable.Index];
            if (ruleVar.Type == null)
            {
                ruleVar.Type = type;
            }
            else
            {
                // TODO - check for conflicts?
            }
        }

        private void PropagateRuleTypesFromParamList(IRRule rule, List<IRValue> parameters, FunctionSignature signature)
        {
            Int32 index = 0;
            foreach (var param in parameters)
            {
                if (param is IRVariable)
                {
                    var irVar = param as IRVariable;
                    PropagateIRVariableType(rule, param as IRVariable, signature.Params[index].Type);
                }

                index++;
            }
        }

        private void PropagateRuleTypes(IRFact fact)
        {
            PropagateSignatureIfRequired(fact.Database.Name, FunctionType.Database, fact.Elements);
        }

        private void PropagateRuleTypes(IRRule rule, IRBinaryCondition condition)
        {
            if (condition.LValue.Type == null
                && condition.LValue is IRVariable)
            {
                var lval = condition.LValue as IRVariable;
                var ruleVariable = rule.Variables[lval.Index];
                if (ruleVariable.Type != null)
                {
                    lval.Type = ruleVariable.Type;
                }
            }

            if (condition.RValue.Type == null
                && condition.RValue is IRVariable)
            {
                var rval = condition.RValue as IRVariable;
                var ruleVariable = rule.Variables[rval.Index];
                if (ruleVariable.Type != null)
                {
                    rval.Type = ruleVariable.Type;
                }
            }

            // TODO - handle implicit re-typing of rule variables?
        }

        private UInt32 ComputeTupleSize(IRRule rule, IRFuncCondition condition, UInt32 lastTupleSize)
        {
            UInt32 tupleSize = lastTupleSize;
            foreach (var param in condition.Params)
            {
                if (param is IRVariable)
                {
                    var variable = param as IRVariable;
                    if (variable.Index >= tupleSize)
                    {
                        tupleSize = (UInt32)variable.Index + 1;
                    }
                }
            }

            return tupleSize;
        }

        private UInt32 ComputeTupleSize(IRRule rule, IRBinaryCondition condition, UInt32 lastTupleSize)
        {
            UInt32 tupleSize = lastTupleSize;
            if (condition.LValue is IRVariable)
            {
                var variable = condition.LValue as IRVariable;
                if (variable.Index >= tupleSize)
                {
                    tupleSize = (UInt32)variable.Index + 1;
                }
            }

            if (condition.RValue is IRVariable)
            {
                var variable = condition.RValue as IRVariable;
                if (variable.Index >= tupleSize)
                {
                    tupleSize = (UInt32)variable.Index + 1;
                }
            }

            return tupleSize;
        }

        private bool PropagateRuleTypes(IRRule rule)
        {
            UInt32 lastTupleSize = 0;
            foreach (var condition in rule.Conditions)
            {
                if (condition is IRFuncCondition)
                {
                    var func = condition as IRFuncCondition;
                    PropagateSignatureIfRequired(rule, func.Func.Name, null, func.Params, false);
                    if (func.TupleSize == 0)
                    {
                        func.TupleSize = ComputeTupleSize(rule, func, lastTupleSize);
                    }
                }
                else
                {
                    var bin = condition as IRBinaryCondition;
                    PropagateRuleTypes(rule, bin);
                    if (bin.TupleSize == 0)
                    {
                        bin.TupleSize = ComputeTupleSize(rule, bin, lastTupleSize);
                    }
                }

                lastTupleSize = condition.TupleSize;
            }

            foreach (var action in rule.Actions)
            {
                if (action.Func != null)
                {
                    PropagateSignatureIfRequired(rule, action.Func.Name, null, action.Params, false);
                }
            }

            return true;
        }

        public void PropagateRuleTypes()
        {
            foreach (var goal in Context.GoalsByName.Values)
            {
                foreach (var fact in goal.InitSection)
                {
                    PropagateRuleTypes(fact);
                }

                foreach (var rule in goal.KBSection)
                {
                    PropagateRuleTypes(rule);
                }
                foreach (var fact in goal.ExitSection)
                {
                    PropagateRuleTypes(fact);
                }

            }
        }

        private void AddQueryOrProc(IRRule rule)
        {
            // Check if all parameters in the PROC/QRY declaration are typed.
            var procDefn = rule.Conditions[0];
            if (procDefn is IRFuncCondition)
            {
                var def = procDefn as IRFuncCondition;
                FunctionType type;
                switch (rule.Type)
                {
                    case RuleType.Proc: type = FunctionType.Proc; break;
                    case RuleType.Query: type = FunctionType.UserQuery; break;
                    default: throw new InvalidOperationException("Cannot register this type as a PROC or QUERY");
                }

                if (!PropagateSignatureIfRequired(rule, def.Func.Name, type, def.Params, true))
                {
                    // TODO - possibly a warning?
                    //var message = String.Format("Signature must be completely typed in declaration of {0} {1}", 
                    //    rule.Type, def.Func.Name);
                    //Context.Log.Error(null, DiagnosticCode.InvalidProcDefinition, message);
                }
            }
            else
            {
                var message = String.Format("Declaration of a {0} must start with a {0} name and signature.", rule.Type);
                Context.Log.Error(null, DiagnosticCode.InvalidProcDefinition, message);
            }
        }

        public void AddGoal(IRGoal goal)
        {
            Context.RegisterGoal(goal);
            foreach (var rule in goal.KBSection)
            {
                if (rule.Type == RuleType.Query
                    || rule.Type == RuleType.Proc)
                {
                    AddQueryOrProc(rule);
                }
            }
        }
    }
}
