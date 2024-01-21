using System.Diagnostics;

namespace LSLib.LS.Story.Compiler;

public class Compiler
{
    public CompilationContext Context = new CompilationContext();
    public HashSet<FunctionNameAndArity> IgnoreUnusedDatabases = new HashSet<FunctionNameAndArity>();
    public TargetGame Game = TargetGame.DOS2;
    public bool AllowTypeCoercion = false;
    public HashSet<string> TypeCoercionWhitelist;

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
            // BG3 allows promoting integer constants to float
            if (Game == TargetGame.BG3 && value is IRConstant 
                && (param.Type.IntrinsicTypeId == Value.Type.Float || param.Type.IntrinsicTypeId == Value.Type.Integer64)
                && value.Type.IntrinsicTypeId == Value.Type.Integer)
            {
                return;
            }

            object paramName = (param.Name != null) ? (object)param.Name : paramIndex;
            Context.Log.Error(value.Location,
                DiagnosticCode.LocalTypeMismatch,
                "Parameter {0} of {1} \"{2}\" expects {3}; {4} specified",
                paramName, func.Type, func.Name, param.Type.Name, value.Type.Name);
            return;
        }

        if (IsGuidAliasToAliasCast(param.Type, value.Type))
        {
            object paramName = (param.Name != null) ? (object)param.Name : paramIndex;
            Context.Log.Error(value.Location,
                DiagnosticCode.GuidAliasMismatch,
                "Parameter {0} of {1} \"{2}\" has GUID type {3}; {4} specified",
                paramName, func.Type, func.Name, param.Type.Name, value.Type.Name);
            return;
        }
    }

    private void VerifyIRFact(IRFact fact)
    {
        if (fact.Database == null)
        {
            return;
        }

        var db = Context.LookupSignature(fact.Database.Name);
        if (db == null)
        {
            Context.Log.Error(fact.Location, 
                DiagnosticCode.UnresolvedSymbol,
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

        if (fact.Not)
        {
            db.Deleted = true;
        }
        else
        {
            db.Inserted = true;
        }

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

        if (statement.Not)
        {
            func.Deleted = true;
        }
        else
        {
            func.Inserted = true;
        }

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
            
            VerifyIRValue(rule, ele, func);
            VerifyIRValueCall(rule, ele, func, index, -1, statement.Not);
            VerifyParamCompatibility(func, index, param, ele);

            index++;
        }
    }

    private void VerifyIRVariable(IRRule rule, IRVariable variable, FunctionSignature func)
    {
        var ruleVar = rule.Variables[variable.Index];
        if (variable.Type == null)
        {
            Context.Log.Error(variable.Location,
                DiagnosticCode.UnresolvedType,
                "Type of variable {0} could not be determined",
                ruleVar.Name);
            return;
        }

        if (ruleVar.Type == null)
        {
            Context.Log.Error(variable.Location,
                DiagnosticCode.UnresolvedType,
                "Type of rule variable {0} could not be determined",
                ruleVar.Name);
            return;
        }

        if ((func == null || TypeCoercionWhitelist == null || !TypeCoercionWhitelist.Contains(func.GetNameAndArity().ToString()))
            && !AllowTypeCoercion)
        {
            if (!AreIntrinsicTypesCompatible(ruleVar.Type.IntrinsicTypeId, variable.Type.IntrinsicTypeId))
            {
                Context.Log.Error(variable.Location,
                    DiagnosticCode.CastToUnrelatedType,
                    "Cannot cast {1} variable {0} to unrelated type {2}",
                    ruleVar.Name, ruleVar.Type.Name, variable.Type.Name);
                return;
            }

            if (IsRiskyComparison(ruleVar.Type.IntrinsicTypeId, variable.Type.IntrinsicTypeId))
            {
                Context.Log.Error(variable.Location,
                    DiagnosticCode.RiskyComparison,
                    "Coercion of {1} variable {0} to {2} may trigger incorrect behavior",
                    ruleVar.Name, ruleVar.Type.Name, variable.Type.Name);
                return;
            }

            if (IsGuidAliasToAliasCast(ruleVar.Type, variable.Type))
            {
                Context.Log.Error(variable.Location,
                    DiagnosticCode.CastToUnrelatedGuidAlias,
                    "{1} variable {0} converted to unrelated type {2}",
                    ruleVar.Name, ruleVar.Type.Name, variable.Type.Name);
            }
        }
    }

    private void VerifyIRConstant(IRConstant constant)
    {
        if (constant.Type.IntrinsicTypeId == Value.Type.GuidString)
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
                    if (constant.Type.TypeId > CompilationContext.MaxIntrinsicTypeId
                        && type.TypeId != constant.Type.TypeId)
                    {
                        Context.Log.Error(constant.Location, 
                            DiagnosticCode.GuidAliasMismatch,
                            "GUID constant \"{0}\" has inferred type {1}",
                            constant.StringValue, constant.Type.Name);
                    }
                }
                else if (prefix.Contains("GUID") && Game != TargetGame.BG3)
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
                        constant.StringValue, constant.Type.Name, objectInfo.Type.Name);
                }
            }
        }
    }

    private void VerifyIRValue(IRRule rule, IRValue value, FunctionSignature func)
    {
        if (value is IRConstant)
        {
            VerifyIRConstant(value as IRConstant);
        }
        else
        {
            VerifyIRVariable(rule, value as IRVariable, func);
        }
    }

    private void VerifyIRVariableCall(IRRule rule, IRVariable variable, FunctionSignature signature, Int32 parameterIndex, 
        Int32 conditionIndex, bool not)
    {
        var ruleVar = rule.Variables[variable.Index];
        var param = signature.Params[parameterIndex];

        if (param.Direction == ParamDirection.Out && !not)
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
            // NOT conditions never bind, but they allow unbound unused variables
            || (!ruleVar.IsUnused() && not)
            || (
                // Databases and events always bind
                signature.Type != FunctionType.Database
                && signature.Type != FunctionType.Event
                // PROC/QRYs bind if they're the first condition in a rule
                && !(rule.Type == RuleType.Proc && conditionIndex == 0 && signature.Type == FunctionType.Proc)
                && !(rule.Type == RuleType.Query && conditionIndex == 0 && signature.Type == FunctionType.UserQuery)
                && param.Direction != ParamDirection.Out
            )
        ) {

            if (
                // The variable was never bound
                ruleVar.FirstBindingIndex == -1
                // The variable was bound after this node (so it is still unbound here)
                || (conditionIndex != -1 && ruleVar.FirstBindingIndex >= conditionIndex)
            ) {
                object paramName = (param.Name != null) ? (object)param.Name : (parameterIndex + 1);
                if (!ruleVar.IsUnused())
                {
                    Context.Log.Error(variable.Location,
                        DiagnosticCode.ParamNotBound,
                        "Variable {0} is not bound here (when used as parameter {1} of {2} \"{3}\")",
                        ruleVar.Name, paramName, signature.Type, signature.GetNameAndArity());
                }
                else
                {
                    Context.Log.Error(variable.Location,
                        DiagnosticCode.ParamNotBound,
                        "Parameter {0} of {1} \"{2}\" requires a variable or constant, not a placeholder",
                        paramName, signature.Type, signature.GetNameAndArity());
                }
            }
        }
        else
        {
            if (conditionIndex != -1 && ruleVar.FirstBindingIndex == -1 && !not)
            {
                ruleVar.FirstBindingIndex = conditionIndex;
            }
        }
    }

    private void VerifyIRValueCall(IRRule rule, IRValue value, FunctionSignature signature, Int32 parameterIndex, 
        Int32 conditionIndex, bool not)
    {
        if (value is IRVariable)
        {
            VerifyIRVariableCall(rule, value as IRVariable, signature, parameterIndex, conditionIndex, not);
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
                    DiagnosticCode.InvalidFunctionTypeInCondition,
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

            VerifyIRValue(rule, condParam, func);
            VerifyIRValueCall(rule, condParam, func, index, conditionIndex, condition.Not);
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
            case Value.Type.GuidString:
                return Value.Type.String;

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

    /// <summary>
    /// Returns whether comparing the specified types is "risky",
    /// i.e. if there is unexpected behavior or side effects.
    /// </summary>
    private bool IsRiskyComparison(Value.Type type1, Value.Type type2)
    {
        return (type1 == Value.Type.String && type2 == Value.Type.GuidString)
            || (type1 == Value.Type.GuidString && type2 == Value.Type.String);
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
        VerifyIRValue(rule, value, null);

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

        if (condition.LValue is IRVariable
            && condition.RValue is IRVariable
            && (condition.LValue as IRVariable).Index == (condition.RValue as IRVariable).Index
            // This bug was fixed in DOS2 DE
            && Game == TargetGame.DOS2
            // There is a known bug in the main campaign that we have to ignore
            && rule.Goal.Name != "EndGame_PrisonersDilemma")
        {
            Context.Log.Error(condition.Location,
                DiagnosticCode.BinaryOperationSameRhsLhs,
                "Same variable used on both sides of a binary expression; this will result in an invalid compare in runtime");
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

        if (IsRiskyComparison(lhs.IntrinsicTypeId, rhs.IntrinsicTypeId))
        {
            Context.Log.Error(condition.Location,
                DiagnosticCode.RiskyComparison,
                "Comparison between {0} and {1} may trigger incorrect behavior",
                TypeToName(lhs.IntrinsicTypeId), TypeToName(rhs.IntrinsicTypeId));
            return;
        }

        if (IsGuidAliasToAliasCast(lhs, rhs))
        {
            Context.Log.Error(condition.Location, 
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
            if (rule.Type == RuleType.Proc && initialName.Name.Length > 4 && initialName.Name.Substring(0, 4).ToUpper() != "PROC")
            {
                Context.Log.Warn(rule.Conditions[0].Location, 
                    DiagnosticCode.RuleNamingStyle,
                    "Name of PROC \"{0}\" should start with the prefix \"PROC\"", 
                    initialName);
            }

            if (rule.Type == RuleType.Query && initialName.Name.Length > 3 && initialName.Name.Substring(0, 3).ToUpper() != "QRY")
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
                    DiagnosticCode.UnresolvedVariableType,
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
                && !IgnoreUnusedDatabases.Contains(signature.Key))
            {
                Debug.Assert(signature.Value.Inserted
                    || signature.Value.Deleted
                    || signature.Value.Read);

                if (!signature.Value.Read)
                {
                    // Unused databases are considered an error in DOS:2 DE.
                    if (Game == TargetGame.DOS2DE || Game == TargetGame.BG3)
                    {
                        // TODO - return location of declaration
                        Context.Log.Error(null,
                            DiagnosticCode.UnusedDatabaseError,
                            "{0} \"{1}\" is written to, but is never read",
                            signature.Value.Type, signature.Key);
                    }
                    else
                    {
                        Context.Log.Warn(null,
                            DiagnosticCode.UnusedDatabaseWarning,
                            "{0} \"{1}\" is written to, but is never read",
                            signature.Value.Type, signature.Key);
                    }
                }
                
                if (!signature.Value.Inserted
                    && !signature.Value.Deleted
                    && signature.Value.Read)
                {
                    // Unused databases are considered an error in DOS:2 DE.
                    if (Game == TargetGame.DOS2DE || Game == TargetGame.BG3)
                    {
                        Context.Log.Error(null,
                            DiagnosticCode.UnusedDatabaseError,
                            "{0} \"{1}\" is read, but is never written to",
                            signature.Value.Type, signature.Key);
                    }
                    else
                    {
                        Context.Log.Warn(null,
                            DiagnosticCode.UnusedDatabaseWarning,
                            "{0} \"{1}\" is read, but is never written to",
                            signature.Value.Type, signature.Key);
                    }
                }

                if (!signature.Value.Inserted
                    && signature.Value.Deleted
                    && signature.Value.Read)
                {
                    // TODO - return location of declaration
                    Context.Log.Warn(null,
                        DiagnosticCode.UnwrittenDatabase,
                        "{0} \"{1}\" is read and deleted, but is never inserted into",
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
            if (value.Type != null)
            {
                return value.Type;
            }

            var irVar = value as IRVariable;
            var ruleVar = rule.Variables[irVar.Index];
            if (ruleVar.Type != null)
            {
                return ruleVar.Type;
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
                Inserted = false,
                Deleted = false,
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
                    DiagnosticCode.ProcTypeMismatch,
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

    private bool TryPropagateSignature(IRRule rule, FunctionNameAndArity name, FunctionType? type, List<IRValue> parameters, 
        bool allowPartial, ref bool updated)
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
        updated = true;
        return ApplySignature(name, type, sig);
    }

    private bool PropagateSignature(FunctionNameAndArity name, FunctionType? type, List<IRConstant> parameters)
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

    private bool PropagateSignatureIfRequired(IRRule rule, FunctionNameAndArity name, FunctionType? type, List<IRValue> parameters, bool allowPartial, ref bool updated)
    {
        var signature = Context.LookupSignature(name);
        bool signatureOk = (signature != null && signature.FullyTyped);
        if (!signatureOk && TryPropagateSignature(rule, name, type, parameters, allowPartial, ref updated))
        {
            signature = Context.LookupSignature(name);
            signatureOk = signature.FullyTyped;
        }

        if (signatureOk)
        {
            if (PropagateRuleTypesFromParamList(rule, parameters, signature))
            {
                updated = true;
            }
        }

        return signatureOk;
    }

    private bool PropagateSignatureIfRequired(FunctionNameAndArity name, FunctionType? type, List<IRConstant> parameters, ref bool updated)
    {
        var signature = Context.LookupSignature(name);
        if (signature == null || !signature.FullyTyped)
        {
            updated = true;
            return PropagateSignature(name, type, parameters);
        }
        else
        {
            return true;
        }
    }

    private bool PropagateIRVariableType(IRRule rule, IRVariable variable, ValueType type)
    {
        bool updated = false;
        var ruleVar = rule.Variables[variable.Index];
        if (ruleVar.Type == null)
        {
            ruleVar.Type = type;
            updated = true;
        }

        if (variable.Type == null)
        {
            // If a more specific type alias is available from the rule variable, apply the
            // rule type instead of the function argument type
            if (ruleVar.Type.IsAliasOf(type))
            {
                variable.Type = ruleVar.Type;
            }
            else
            {
                variable.Type = type;
            }

            updated = true;
        }

        return updated;
    }

    private bool PropagateRuleTypesFromParamList(IRRule rule, List<IRValue> parameters, FunctionSignature signature)
    {
        bool updated = false;
        Int32 index = 0;
        foreach (var param in parameters)
        {
            if (param is IRVariable)
            {
                var irVar = param as IRVariable;
                if (PropagateIRVariableType(rule, param as IRVariable, signature.Params[index].Type))
                {
                    updated = true;
                }
            }

            index++;
        }

        return updated;
    }

    private bool PropagateRuleTypes(IRFact fact)
    {
        bool updated = false;
        if (fact.Database != null)
        {
            PropagateSignatureIfRequired(fact.Database.Name, FunctionType.Database, fact.Elements, ref updated);
        }
        return updated;
    }

    private bool PropagateRuleTypes(IRRule rule, IRBinaryCondition condition)
    {
        bool updated = false;
        if (condition.LValue.Type == null
            && condition.LValue is IRVariable)
        {
            var lval = condition.LValue as IRVariable;
            var ruleVariable = rule.Variables[lval.Index];
            if (ruleVariable.Type != null)
            {
                lval.Type = ruleVariable.Type;
                updated = true;
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
                updated = true;
            }
        }

        // TODO - handle implicit re-typing of rule variables?

        return updated;
    }

    private Int32 ComputeTupleSize(IRRule rule, IRFuncCondition condition, Int32 lastTupleSize)
    {
        Int32 tupleSize = lastTupleSize;
        foreach (var param in condition.Params)
        {
            if (param is IRVariable)
            {
                var variable = param as IRVariable;
                if (variable.Index >= tupleSize)
                {
                    tupleSize = variable.Index + 1;
                }
            }
        }

        return tupleSize;
    }

    private Int32 ComputeTupleSize(IRRule rule, IRBinaryCondition condition, Int32 lastTupleSize)
    {
        Int32 tupleSize = lastTupleSize;
        if (condition.LValue is IRVariable)
        {
            var variable = condition.LValue as IRVariable;
            if (variable.Index >= tupleSize)
            {
                tupleSize = variable.Index + 1;
            }
        }

        if (condition.RValue is IRVariable)
        {
            var variable = condition.RValue as IRVariable;
            if (variable.Index >= tupleSize)
            {
                tupleSize = variable.Index + 1;
            }
        }

        return tupleSize;
    }

    private bool PropagateRuleTypes(IRRule rule)
    {
        bool updated = false;

        Int32 lastTupleSize = 0;
        foreach (var condition in rule.Conditions)
        {
            if (condition is IRFuncCondition)
            {
                var func = condition as IRFuncCondition;
                PropagateSignatureIfRequired(rule, func.Func.Name, null, func.Params, false, ref updated);
                if (func.TupleSize == -1)
                {
                    func.TupleSize = ComputeTupleSize(rule, func, lastTupleSize);
                    updated = true;
                }
            }
            else
            {
                var bin = condition as IRBinaryCondition;
                if (PropagateRuleTypes(rule, bin))
                {
                    updated = true;
                }

                if (bin.TupleSize == -1)
                {
                    bin.TupleSize = ComputeTupleSize(rule, bin, lastTupleSize);
                    updated = true;
                }
            }

            lastTupleSize = condition.TupleSize;
        }

        foreach (var action in rule.Actions)
        {
            if (action.Func != null)
            {
                PropagateSignatureIfRequired(rule, action.Func.Name, null, action.Params, false, ref updated);
            }
        }

        return updated;
    }

    public bool PropagateRuleTypes()
    {
        bool updated = false;

        foreach (var goal in Context.GoalsByName.Values)
        {
            foreach (var fact in goal.InitSection)
            {
                if (PropagateRuleTypes(fact))
                {
                    updated = true;
                }
            }

            foreach (var rule in goal.KBSection)
            {
                if (PropagateRuleTypes(rule))
                {
                    updated = true;
                }
            }

            foreach (var fact in goal.ExitSection)
            {
                if (PropagateRuleTypes(fact))
                {
                    updated = true;
                }
            }
        }

        return updated;
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

            bool updated = false;
            if (!PropagateSignatureIfRequired(rule, def.Func.Name, type, def.Params, true, ref updated))
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
