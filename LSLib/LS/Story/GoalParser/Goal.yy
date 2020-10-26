%namespace LSLib.LS.Story.GoalParser
%partial 
%visibility public
%parsertype GoalParser
%tokentype GoalTokens
%YYSTYPE System.Object
%YYLTYPE LSLib.LS.Story.GoalParser.CodeLocation

%start GoalFile

/* Reserved word */
%token VERSION
%token SUBGOALCOMBINER
%token SGC_AND
%token INITSECTION
%token KBSECTION
%token EXITSECTION
%token ENDEXITSECTION
%token IF
%token PROC
%token QRY
%token THEN
%token AND
%token NOT

%token GOAL_COMPLETED
%token PARENT_TARGET_EDGE

/* Operators */
%token EQ_OP
%token NE_OP
%token LT_OP
%token LTE_OP
%token GT_OP
%token GTE_OP

/* Special token for invalid characters */
%token BAD

/* Type, database, query, event or proc identifier */
%token IDENTIFIER
/* Local variable identifier */
%token LOCAL_VAR
/* Integer literal */
%token INTEGER
/* Float literal */
%token FLOAT
/* String literal */
%token STRING
/* GUID string literal */
%token GUIDSTRING

%%

GoalFile: Goal;

Goal : Version SubGoalCombiner InitSection KBSection ExitSection TargetEdges 
     { $$ = MakeGoal(@$, $1, $2, $3, $4, $5, $6); };

Version : VERSION INTEGER
    { $$ = $2; };

SubGoalCombiner : SUBGOALCOMBINER SGC_AND
    { $$ = $2; };

InitSection : INITSECTION Facts
    { $$ = $2; };

KBSection : KBSECTION Rules
    { $$ = $2; };

ExitSection : EXITSECTION Facts ENDEXITSECTION
    { $$ = $2; };

TargetEdges : /* empty */ { $$ = MakeParentTargetEdgeList(); }
            | TargetEdges TargetEdge { $$ = MakeParentTargetEdgeList($1, $2); }
            ;

TargetEdge : PARENT_TARGET_EDGE STRING
    { $$ = MakeParentTargetEdge(@$, $2); };

Facts : /* empty */ { $$ = MakeFactList(); }
      | Facts Fact { $$ = MakeFactList($1, $2); }
      ;
      
Fact : FactStatement { $$ = $1; }
     | NOT FactStatement { $$ = MakeNotFact(@$, $2); }
     | GOAL_COMPLETED ';' { $$ = MakeGoalCompletedFact(@$); }
     ;

FactStatement : IDENTIFIER '(' FactElementList ')' ';'
    { $$ = MakeFactStatement(@$, $1, $3); };

FactElementList : /* empty */ { $$ = MakeFactElementList(); }
                | FactElement { $$ = MakeFactElementList($1); }
                | FactElementList ',' FactElement { $$ = MakeFactElementList($1, $3); }
                ;
         
FactElement : TypedConstant
    { $$ = $1; };
              
Constant : GUIDSTRING { $$ = MakeConstGuidString(@$, $1); }
         | STRING { $$ = MakeConstString(@$, $1); }
         | INTEGER { $$ = MakeConstInteger(@$, $1); }
         | FLOAT { $$ = MakeConstFloat(@$, $1); }
         ;
        
Rules : /* empty */ { $$ = MakeRuleList(); }
      | Rules Rule { $$ = MakeRuleList($1, $2); }
      ;
      
Rule : RuleType Conditions THEN ActionList
    { $$ = MakeRule(@$, $1, $2, $4); };

RuleType : IF { $$ = MakeRuleType(RuleType.Rule); }
         | PROC { $$ = MakeRuleType(RuleType.Proc); }
         | QRY { $$ = MakeRuleType(RuleType.Query); }
         ;

Conditions : InitialCondition { $$ = MakeConditionList($1); }
           | Conditions AND Condition { $$ = MakeConditionList($1, $3); }
           ;
           
InitialCondition : FuncCondition;
           
Condition  : FuncCondition
           | NotFuncCondition
           | BinaryCondition
           ;

FuncCondition : IDENTIFIER '(' ConditionParamList ')' { $$ = MakeFuncCondition(@$, $1, $3, false); }
              | TypedLocalVar '.' IDENTIFIER '(' ConditionParamList ')' { $$ = MakeObjectFuncCondition(@$, $1, $3, $5, false); }
              ;

NotFuncCondition : NOT IDENTIFIER '(' ConditionParamList ')' { $$ = MakeFuncCondition(@$, $2, $4, true); }
                 | NOT TypedLocalVar '.' IDENTIFIER '(' ConditionParamList ')' { $$ = MakeObjectFuncCondition(@$, $2, $4, $6, true); }
                 ;

BinaryCondition : ConditionParam Operator ConditionParam { $$ = MakeBinaryCondition(@$, $1, $2, $3); }
                | NOT ConditionParam Operator ConditionParam { $$ = MakeNegatedBinaryCondition(@$, $2, $3, $4); }
                ;

ConditionParamList : /* Empty */ { $$ = MakeConditionParamList(); }
                   | ConditionParam { $$ = MakeConditionParamList($1); }
                   | ConditionParamList ',' ConditionParam { $$ = MakeConditionParamList($1, $3); }
                   ;
                   
ConditionParam : TypedConstant { $$ = $1; }
               | TypedLocalVar { $$ = $1; }
               ;

Operator : EQ_OP { $$ = MakeOperator(RelOpType.Equal); }
         | NE_OP { $$ = MakeOperator(RelOpType.NotEqual); }
         | LT_OP { $$ = MakeOperator(RelOpType.Less); }
         | LTE_OP { $$ = MakeOperator(RelOpType.LessOrEqual); }
         | GT_OP { $$ = MakeOperator(RelOpType.Greater); }
         | GTE_OP { $$ = MakeOperator(RelOpType.GreaterOrEqual); }
         ;

ActionList : /* empty */ { $$ = MakeActionList(); }
           | ActionList Action { $$ = MakeActionList($1, $2); }
           ;

Action : ActionStatement { $$ = $1; }
       | GOAL_COMPLETED ';' { $$ = MakeGoalCompletedAction(@$); }
       ;

ActionStatement : IDENTIFIER '(' ActionParamList ')' ';' { $$ = MakeActionStatement(@$, $1, $3, false); }
                | TypedLocalVar '.' IDENTIFIER '(' ActionParamList ')' ';' { $$ = MakeActionStatement(@$, $1, $3, $5, false); }
                | NOT IDENTIFIER '(' ActionParamList ')' ';' { $$ = MakeActionStatement(@$, $2, $4, true); }
                | NOT TypedLocalVar '.' IDENTIFIER '(' ActionParamList ')' ';' { $$ = MakeActionStatement(@$, $2, $4, $6, true); }
                ;

ActionParamList : /* Empty */ { $$ = MakeActionParamList(); }
                | ActionParam { $$ = MakeActionParamList($1); }
                | ActionParamList ',' ActionParam { $$ = MakeActionParamList($1, $3); }
                ;
                
ActionParam : TypedConstant { $$ = $1; }
            | TypedLocalVar { $$ = $1; }
            ;

TypedConstant : Constant { $$ = $1; }
              | '(' IDENTIFIER ')' Constant { $$ = MakeTypedConstant(@$, $2, $4); }
              ;

TypedLocalVar : LOCAL_VAR { $$ = MakeLocalVar(@$, $1); }
              | '(' IDENTIFIER ')' LOCAL_VAR { $$ = MakeLocalVar(@$, $2, $4); }
              ;
