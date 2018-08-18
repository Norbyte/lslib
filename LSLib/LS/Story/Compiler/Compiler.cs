using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        // Indicates that the function is "written" in at least one place
        public Boolean Written;
        // Indicates that the function is "read" in at least one place
        public Boolean Read;

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
        public const String InternalError = "00";
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
        public const String ParamNotBound = "24";
        public const String UnusedDatabase = "25";
        public const String DbNamingStyle = "26";
        public const String UnresolvedGameObjectName = "27";
        public const String GameObjectTypeMismatch = "28";
        public const String GameObjectNameMismatch = "29";
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
        /// <summary>
        /// Controls whether specific warnings are enabled or disabled.
        /// All are enabled by default.
        /// </summary>
        public Dictionary<string, bool> WarningSwitches = new Dictionary<string, bool>();

        public CompilationLog()
        {
            WarningSwitches.Add(DiagnosticCode.RuleNamingStyle, false);
        }

        public void Warn(CodeLocation location, String code, String message)
        {
            if (WarningSwitches.TryGetValue(code, out bool enabled) && !enabled) return;

            var diag = new Diagnostic(location, MessageLevel.Warning, code, message);
            Log.Add(diag);
        }

        public void Warn(CodeLocation location, String code, String format, object arg1)
        {
            var message = String.Format(format, arg1);
            Warn(location, code, message);
        }

        public void Warn(CodeLocation location, String code, String format, object arg1, object arg2)
        {
            var message = String.Format(format, arg1, arg2);
            Warn(location, code, message);
        }

        public void Warn(CodeLocation location, String code, String format, object arg1, object arg2, object arg3)
        {
            var message = String.Format(format, arg1, arg2, arg3);
            Warn(location, code, message);
        }

        public void Warn(CodeLocation location, String code, String format, object arg1, object arg2, object arg3, object arg4)
        {
            var message = String.Format(format, arg1, arg2, arg3, arg4);
            Warn(location, code, message);
        }

        public void Warn(CodeLocation location, String code, String format, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            var message = String.Format(format, arg1, arg2, arg3, arg4, arg5);
            Warn(location, code, message);
        }

        public void Error(CodeLocation location, String code, String message)
        {
            var diag = new Diagnostic(location, MessageLevel.Error, code, message);
            Log.Add(diag);
        }

        public void Error(CodeLocation location, String code, String format, object arg1)
        {
            var message = String.Format(format, arg1);
            Error(location, code, message);
        }

        public void Error(CodeLocation location, String code, String format, object arg1, object arg2)
        {
            var message = String.Format(format, arg1, arg2);
            Error(location, code, message);
        }

        public void Error(CodeLocation location, String code, String format, object arg1, object arg2, object arg3)
        {
            var message = String.Format(format, arg1, arg2, arg3);
            Error(location, code, message);
        }

        public void Error(CodeLocation location, String code, String format, object arg1, object arg2, object arg3, object arg4)
        {
            var message = String.Format(format, arg1, arg2, arg3, arg4);
            Error(location, code, message);
        }

        public void Error(CodeLocation location, String code, String format, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            var message = String.Format(format, arg1, arg2, arg3, arg4, arg5);
            Error(location, code, message);
        }
    }

    public class GameObjectInfo
    {
        public String Name;
        public ValueType Type;
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
        public Dictionary<String, GameObjectInfo> GameObjects = new Dictionary<String, GameObjectInfo>();
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
    

    public class Compiler
    {
        public CompilationContext Context = new CompilationContext();

        private string TypeToName(uint typeId)
        {
            var type = Context.TypesById[typeId];
            return type.Name;
        }

        private string TypeToName(Value.Type typeId)
        {
            return TypeToName((uint)typeId);
        }

        private void VerifyParamCompatibility(FunctionSignature func, int paramIndex, FunctionParam param, IRValue value)
        {
            if (param.Type.IntrinsicTypeId != value.Type.IntrinsicTypeId)
            {
                object paramName = (param.Name != null) ? (object)param.Name : paramIndex;
                Context.Log.Error(value.Location,
                    DiagnosticCode.LocalTypeMismatch,
                    "Parameter {0} of {1} \"{2}\" expects {3}; {4} specified",
                    paramName, func.Type, func.Name, TypeToName(param.Type.IntrinsicTypeId), TypeToName(value.Type.IntrinsicTypeId));
                return;
            }

            if (IsGuidAliasToAliasCast(param.Type, value.Type))
            {
                object paramName = (param.Name != null) ? (object)param.Name : paramIndex;
                Context.Log.Warn(value.Location,
                    DiagnosticCode.GuidAliasMismatch,
                    "Parameter {0} of {1} \"{2}\" has GUID type {3}; {4} specified",
                    paramName, func.Type, func.Name, TypeToName(param.Type.TypeId), TypeToName(value.Type.TypeId));
                return;
            }
        }

        private void VerifyIRFact(IRFact fact)
        {
            var db = Context.LookupSignature(fact.Database.Name);
            if (db == null)
            {
                Context.Log.Error(fact.Location, 
                    DiagnosticCode.UnresolvedSignature,
                    "Database \"{0}\" could not be resolved",
                    fact.Database.Name);
                return;
            }

            if (db.Type != FunctionType.Database
                && db.Type != FunctionType.Call
                && db.Type != FunctionType.SysCall
                && db.Type != FunctionType.Proc)
            {
                Context.Log.Error(fact.Location, 
                    DiagnosticCode.InvalidSymbolInFact,
                    "Init/Exit actions can only reference databases, calls and PROCs; \"{0}\" is a {1}",
                    fact.Database.Name, db.Type);
                return;
            }

            db.Written = true;

            int index = 0;
            foreach (var param in db.Params)
            {
                var ele = fact.Elements[index];
                index++;

                if (ele.Type == null)
                {
                    Context.Log.Error(ele.Location, 
                        DiagnosticCode.InternalError, 
                        "No type information available for fact argument");
                    continue;
                }

                VerifyParamCompatibility(db, index, param, ele);
            }
        }

        private void VerifyIRStatement(IRRule rule, IRStatement statement)
        {
            if (statement.Func == null) return;

            var func = Context.LookupSignature(statement.Func.Name);
            if (func == null)
            {
                Context.Log.Error(statement.Location, 
                    DiagnosticCode.UnresolvedSymbol,
                    "Symbol \"{0}\" could not be resolved", 
                    statement.Func.Name);
                return;
            }

            if (!func.FullyTyped)
            {
                Context.Log.Error(statement.Location, 
                    DiagnosticCode.UnresolvedSignature,
                    "Signature of \"{0}\" could not be determined", 
                    statement.Func.Name);
                return;
            }

            if (func.Type != FunctionType.Database
                && func.Type != FunctionType.Call
                && func.Type != FunctionType.SysCall
                && func.Type != FunctionType.Proc)
            {
                Context.Log.Error(statement.Location, 
                    DiagnosticCode.InvalidSymbolInStatement,
                    "KB rule actions can only reference databases, calls and PROCs; \"{0}\" is a {1}",
                    statement.Func.Name, func.Type);
                return;
            }

            if (statement.Not
                && func.Type != FunctionType.Database)
            {
                Context.Log.Error(statement.Location,
                    DiagnosticCode.CanOnlyDeleteFromDatabase,
                    "KB rule NOT actions can only reference databases; \"{0}\" is a {1}",
                    statement.Func.Name, func.Type);
                return;
            }

            func.Written = true;

            int index = 0;
            foreach (var param in func.Params)
            {
                var ele = statement.Params[index];

                ValueType type = ele.Type;
                if (type == null)
                {
                    Context.Log.Error(ele.Location, 
                        DiagnosticCode.InternalError,
                        "No type information available for statement argument");
                    continue;
                }
                
                VerifyIRValue(rule, ele);
                VerifyIRValueCall(rule, ele, func, index, -1);
                VerifyParamCompatibility(func, index, param, ele);

                index++;
            }
        }

        private void VerifyIRVariable(IRRule rule, IRVariable variable)
        {
            var ruleVar = rule.Variables[variable.Index];
            if (!AreIntrinsicTypesCompatible(ruleVar.Type.IntrinsicTypeId, variable.Type.IntrinsicTypeId))
            {
                Context.Log.Error(variable.Location, 
                    DiagnosticCode.LocalTypeMismatch,
                    "Rule variable {0} of type {1} cannot be converted to {2}",
                    ruleVar.Name, TypeToName(ruleVar.Type.IntrinsicTypeId), TypeToName(variable.Type.IntrinsicTypeId));
                return;
            }

            if (IsGuidAliasToAliasCast(ruleVar.Type, variable.Type))
            {
                Context.Log.Warn(variable.Location, 
                    DiagnosticCode.GuidAliasMismatch,
                    "GUID alias cast: Rule variable {0} of type {1} converted to {2}",
                    ruleVar.Name, TypeToName(ruleVar.Type.TypeId), TypeToName(variable.Type.TypeId));
            }
        }

        private void VerifyIRConstant(IRConstant constant)
        {
            if (constant.Type.IntrinsicTypeId == Value.Type.GuidString
                && constant.Type.TypeId > CompilationContext.MaxIntrinsicTypeId)
            {
                var nameWithoutType = constant.StringValue;
                ValueType type = null;

                // Check if the value is prefixed by any of the known GUID subtypes.
                // If a match is found, verify that the type of the constant matched the GUID subtype.
                var underscore = constant.StringValue.IndexOf('_');
                if (underscore != -1)
                {
                    var prefix = constant.StringValue.Substring(0, underscore);
                    type = Context.LookupType(prefix);
                    if (type != null)
                    {
                        nameWithoutType = constant.StringValue.Substring(underscore + 1);
                        if (type.TypeId != constant.Type.TypeId)
                        {
                            Context.Log.Warn(constant.Location, 
                                DiagnosticCode.GuidAliasMismatch,
                                "GUID constant \"{0}\" has inferred type {1}",
                                constant.StringValue, constant.Type.Name);
                        }
                    }
                    else if (prefix.Contains("GUID"))
                    {
                        Context.Log.Warn(constant.Location, 
                            DiagnosticCode.GuidPrefixNotKnown,
                            "GUID constant \"{0}\" is prefixed with unknown type {1}",
                            constant.StringValue, prefix);
                    }
                }

                var guid = constant.StringValue.Substring(constant.StringValue.Length - 36);
                if (!Context.GameObjects.TryGetValue(guid, out GameObjectInfo objectInfo))
                {
                    Context.Log.Warn(constant.Location,
                        DiagnosticCode.UnresolvedGameObjectName,
                        "Object \"{0}\" could not be resolved",
                        constant.StringValue);
                }
                else
                {
                    if (objectInfo.Name != nameWithoutType)
                    {
                        Context.Log.Warn(constant.Location,
                            DiagnosticCode.GameObjectNameMismatch,
                            "Constant \"{0}\" references game object with different name (\"{1}\")",
                            nameWithoutType, objectInfo.Name);
                    }

                    if (constant.Type.TypeId != (uint)Value.Type.GuidString
                        && objectInfo.Type.TypeId != (uint)Value.Type.GuidString
                        && constant.Type.TypeId != objectInfo.Type.TypeId)
                    {
                        Context.Log.Warn(constant.Location,
                            DiagnosticCode.GameObjectTypeMismatch,
                            "Constant \"{0}\" of type {1} references game object of type {2}",
                            constant.StringValue, TypeToName(constant.Type.TypeId), TypeToName(objectInfo.Type.TypeId));
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

        private void VerifyIRVariableCall(IRRule rule, IRVariable variable, FunctionSignature signature, Int32 parameterIndex, Int32 conditionIndex)
        {
            var ruleVar = rule.Variables[variable.Index];
            var param = signature.Params[parameterIndex];

            if (param.Direction == ParamDirection.Out)
            {
                Debug.Assert(conditionIndex != -1);
                if (ruleVar.FirstBindingIndex == -1)
                {
                    ruleVar.FirstBindingIndex = conditionIndex;
                }
            }
            else if (
                // We're in the THEN section of a rule, so we cannot bind here
                conditionIndex == -1 
                || (
                    // Databases and events always bind
                    signature.Type != FunctionType.Database
                    && signature.Type != FunctionType.Event
                    // PROC/QRYs bind if they're the first condition in a rule
                    && !(rule.Type == RuleType.Proc && conditionIndex == 0 && signature.Type == FunctionType.Proc)
                    && !(rule.Type == RuleType.Query && conditionIndex == 0 && signature.Type == FunctionType.UserQuery)
                )
            ) {

                if (
                    // The variable was never bound
                    ruleVar.FirstBindingIndex == -1
                    // The variable was bound after this node (so it is still unbound here)
                    || (conditionIndex != -1 && ruleVar.FirstBindingIndex >= conditionIndex)
                ) {
                    object paramName = (param.Name != null) ? (object)param.Name : parameterIndex;
                    Context.Log.Error(variable.Location, 
                        DiagnosticCode.ParamNotBound,
                        "Variable {0} is not bound (when used as parameter {1} in {2} \"{3}\")",
                        ruleVar.Name, paramName, signature.Type, signature.GetNameAndArity());
                }
            }
            else
            {
                if (conditionIndex != -1 && ruleVar.FirstBindingIndex == -1)
                {
                    ruleVar.FirstBindingIndex = conditionIndex;
                }
            }
        }

        private void VerifyIRValueCall(IRRule rule, IRValue value, FunctionSignature signature, Int32 parameterIndex, Int32 conditionIndex)
        {
            if (value is IRVariable)
            {
                VerifyIRVariableCall(rule, value as IRVariable, signature, parameterIndex, conditionIndex);
            }
        }

        private void VerifyIRFuncCondition(IRRule rule, IRFuncCondition condition, int conditionIndex)
        {
            // TODO - Merge FuncCondition and IRStatement base?
            // Base --> IRParameterizedCall --> FuncCond: has (NOT) field
            var func = Context.LookupSignature(condition.Func.Name);
            if (func == null)
            {
                Context.Log.Error(condition.Location, 
                    DiagnosticCode.UnresolvedSymbol, 
                    "Symbol \"{0}\" could not be resolved", 
                    condition.Func.Name);
                return;
            }

            if (!func.FullyTyped)
            {
                Context.Log.Error(condition.Location, 
                    DiagnosticCode.UnresolvedSignature,
                    "Signature of \"{0}\" could not be determined", 
                    condition.Func.Name);
                return;
            }

            func.Read = true;

            if (conditionIndex == 0)
            {
                switch (rule.Type)
                {
                    case RuleType.Proc:
                        if (func.Type != FunctionType.Proc)
                        {
                            Context.Log.Error(condition.Location, 
                                DiagnosticCode.InvalidSymbolInInitialCondition,
                                "Initial proc condition can only be a PROC name; \"{0}\" is a {1}",
                                condition.Func.Name, func.Type);
                            return;
                        }
                        break;

                    case RuleType.Query:
                        if (func.Type != FunctionType.UserQuery)
                        {
                            Context.Log.Error(condition.Location, 
                                DiagnosticCode.InvalidSymbolInInitialCondition,
                                "Initial query condition can only be a user-defined QRY name; \"{0}\" is a {1}",
                                condition.Func.Name, func.Type);
                            return;
                        }
                        break;

                    case RuleType.Rule:
                        if (func.Type != FunctionType.Event
                            && func.Type != FunctionType.Database)
                        {
                            Context.Log.Error(condition.Location, 
                                DiagnosticCode.InvalidSymbolInInitialCondition,
                                "Initial rule condition can only be an event or a DB; \"{0}\" is a {1}",
                                condition.Func.Name, func.Type);
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
                    Context.Log.Error(condition.Location, 
                        DiagnosticCode.InvalidSymbolInCondition,
                        "Subsequent rule conditions can only be queries or DBs; \"{0}\" is a {1}",
                        condition.Func.Name, func.Type);
                    return;
                }
            }

            int index = 0;
            foreach (var param in func.Params)
            {
                var condParam = condition.Params[index];
                ValueType type = condParam.Type;

                if (type == null)
                {
                    Context.Log.Error(condParam.Location, 
                        DiagnosticCode.InternalError,
                        "No type information available for func condition arg");
                    continue;
                }

                VerifyIRValue(rule, condParam);
                VerifyIRValueCall(rule, condParam, func, index, conditionIndex);
                VerifyParamCompatibility(func, index, param, condParam);

                index++;
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

        private void VerifyIRBinaryConditionValue(IRRule rule, IRValue value, Int32 conditionIndex)
        {
            VerifyIRValue(rule, value);

            if (value is IRVariable)
            {
                var variable = value as IRVariable;
                var ruleVar = rule.Variables[variable.Index];
                if (ruleVar.FirstBindingIndex == -1 || ruleVar.FirstBindingIndex >= conditionIndex)
                {
                    Context.Log.Error(variable.Location, 
                        DiagnosticCode.ParamNotBound,
                        "Variable {0} is not bound (when used in a binary expression)", 
                        ruleVar.Name);
                }
            }
        }

        private void VerifyIRBinaryCondition(IRRule rule, IRBinaryCondition condition, Int32 conditionIndex)
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

            VerifyIRBinaryConditionValue(rule, condition.LValue, conditionIndex);
            VerifyIRBinaryConditionValue(rule, condition.RValue, conditionIndex);
            
            if (!AreIntrinsicTypesCompatible(lhs.IntrinsicTypeId, rhs.IntrinsicTypeId))
            {
                Context.Log.Error(condition.Location, 
                    DiagnosticCode.LocalTypeMismatch,
                    "Type of left expression ({0}) differs from type of right expression ({1})",
                    TypeToName(lhs.IntrinsicTypeId), TypeToName(rhs.IntrinsicTypeId));
                return;
            }

            if (IsGuidAliasToAliasCast(lhs, rhs))
            {
                Context.Log.Warn(condition.Location, 
                    DiagnosticCode.GuidAliasMismatch,
                    "GUID alias type of left expression ({0}) differs from type of right expression ({1})",
                    TypeToName(lhs.TypeId), TypeToName(rhs.TypeId));
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
                Context.Log.Warn(condition.Location, 
                    DiagnosticCode.StringLtGtComparison,
                    "String comparison using operator {0} - probably a mistake?", 
                    condition.Op);
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
                    Context.Log.Warn(rule.Conditions[0].Location, 
                        DiagnosticCode.RuleNamingStyle,
                        "Name of PROC \"{0}\" should start with the prefix \"PROC\"", 
                        initialName);
                }

                if (rule.Type == RuleType.Query && initialName.Name.Substring(0, 3).ToUpper() != "QRY")
                {
                    Context.Log.Warn(rule.Conditions[0].Location, 
                        DiagnosticCode.RuleNamingStyle,
                        "Name of Query \"{0}\" should start with the prefix \"QRY\"", 
                        initialName);
                }
            }

            for (var i = 0; i < rule.Conditions.Count; i++)
            {
                var condition = rule.Conditions[i];
                if (condition is IRBinaryCondition)
                {
                    VerifyIRBinaryCondition(rule, condition as IRBinaryCondition, i);
                }
                else
                {
                    VerifyIRFuncCondition(rule, condition as IRFuncCondition, i);
                }
            }

            foreach (var action in rule.Actions)
            {
                VerifyIRStatement(rule, action);
            }

            foreach (var variable in rule.Variables)
            {
                if (variable.Type == null)
                {
                    // TODO - return location of first variable reference instead of rule
                    Context.Log.Error(rule.Location, 
                        DiagnosticCode.UnresolvedLocalType,
                        "Variable \"{0}\" of rule could not be typed",
                        variable.Name);
                }
            }
        }
           
        private void VerifyDatabases()
        {
            foreach (var signature in Context.Signatures)
            {
                if (signature.Value.Type == FunctionType.Database
                    && signature.Key.Name.Substring(0, 2).ToUpper() != "DB")
                {
                    // TODO - return location of declaration
                    Context.Log.Warn(null, 
                        DiagnosticCode.DbNamingStyle,
                        "Name of database \"{0}\" should start with the prefix \"DB\"", 
                        signature.Key.Name);
                }
            }
        }

        private void VerifyUnusedDatabases()
        {
            foreach (var signature in Context.Signatures)
            {
                if (signature.Value.Type == FunctionType.Database
                    && (!signature.Value.Written || !signature.Value.Read))
                {
                    Debug.Assert(signature.Value.Written || signature.Value.Read);
                    if (!signature.Value.Written)
                    {
                        // TODO - return location of declaration
                        Context.Log.Warn(null, 
                            DiagnosticCode.UnusedDatabase,
                            "{0} \"{1}\" is used in a rule, but is never written to",
                            signature.Value.Type, signature.Key);
                    }
                    else
                    {
                        // TODO - return location of declaration
                        Context.Log.Warn(null, 
                            DiagnosticCode.UnusedDatabase,
                            "{0} \"{1}\" is written to, but is never used in a rule",
                            signature.Value.Type, signature.Key);
                    }
                }
            }
        }

        public void VerifyIR()
        {
            foreach (var goal in Context.GoalsByName.Values)
            {
                foreach (var parentGoal in goal.ParentTargetEdges)
                {
                    if (Context.LookupGoal(parentGoal.Goal.Name) == null)
                    {
                        Context.Log.Error(parentGoal.Location, 
                            DiagnosticCode.UnresolvedGoal,
                            "Parent goal of \"{0}\" could not be resolved: \"{1}\"", 
                            goal.Name, parentGoal.Goal.Name);
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

            // Validate database names
            // We do this here as there is no explicit declaration for databases,
            // they are created implicitly on first use.
            VerifyDatabases();
            VerifyUnusedDatabases();
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
                    Type = (type == null) ? FunctionType.Database : (FunctionType)type,
                    Written = false,
                    Read = false
                };
            }
            else
            {
                if (type != null && signature.Type != type)
                {
                    // TODO error code!
                    // TODO location of definition
                    Context.Log.Error(null, 
                        DiagnosticCode.InvalidProcDefinition,
                        "Auto-typing name {0}: first seen as {1}, now seen as {2}",
                        name, signature.Type, type);
                }
            }

            signature.FullyTyped = !paramTypes.Any(ty => ty == null);
            signature.Params = new List<FunctionParam>(paramTypes.Count);
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
            var sig = new List<ValueType>(parameters.Count);
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
            var sig = new List<ValueType>(parameters.Count);
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
                    /*Context.Log.Error(procDefn.Location, 
                        DiagnosticCode.InvalidProcDefinition,
                        "Signature must be completely typed in declaration of {0} {1}",
                        rule.Type, def.Func.Name);*/
                }
            }
            else
            {
                Context.Log.Error(procDefn.Location, 
                    DiagnosticCode.InvalidProcDefinition,
                    "Declaration of a {0} must start with a {0} name and signature.", 
                    rule.Type);
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
