%namespace LSLib.LS.Story
%partial 
%visibility public
%parsertype GoalParser
%tokentype GoalTokens

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

%%

GoalFile: Goal;

Goal : Version SubGoalCombiner InitSection KBSection ExitSection TargetEdges;

Version : VERSION INTEGER;

SubGoalCombiner : SUBGOALCOMBINER SGC_AND;

InitSection : INITSECTION Facts;

KBSection : KBSECTION Rules;

ExitSection : EXITSECTION Facts ENDEXITSECTION;

TargetEdges : /* empty */
            | TargetEdges TargetEdge
			;

TargetEdge : PARENT_TARGET_EDGE STRING;

Facts : /* empty */
      | Facts Fact
	  ;
	  
Fact : FactStatement
     | NOT FactStatement
	 ;

FactStatement : IDENTIFIER '(' FactElementList ')' ';';

FactElementList : /* empty */
                | FactElement
                | FactElementList ',' FactElement
				;
		 
FactElement : TypedConstant;
			  
Constant : IDENTIFIER
         | STRING
		 | INTEGER
		 | FLOAT
		 ;
		
Rules : /* empty */
      | Rules Rule
	  ;
	  
Rule : RuleType Conditions THEN Actions;

RuleType : IF
         | PROC
		 | QRY
		 ;

Conditions : Condition
           | Conditions AND Condition
		   | Conditions AND NOT Condition
		   ;
		   
Condition  : IDENTIFIER '(' ConditionParamList ')'
           | TypedObject '.' IDENTIFIER '(' ConditionParamList ')'
           | ConditionParam Operator ConditionParam
		   ;

ConditionParamList : /* Empty */
                   | ConditionParam
				   | ConditionParamList ',' ConditionParam
				   ;
				   
ConditionParam : TypedConstant
               | TypedLocalVar
			   ;

TypedObject : LOCAL_VAR
            | '(' IDENTIFIER ')' LOCAL_VAR
			;

Operator : EQ_OP
         | NE_OP
		 | LT_OP
		 | LTE_OP
		 | GT_OP
		 | GTE_OP
		 ;

Actions : /* empty */
        | Actions Action
		;

Action : ActionStatement
       | NOT ActionStatement
	   | GOAL_COMPLETED ';'
	   ;

ActionStatement : IDENTIFIER '(' ActionParamList ')' ';'
                | TypedObject '.' IDENTIFIER '(' ActionParamList ')' ';'
				;

ActionParamList : /* Empty */
                | ActionParam
				| ActionParamList ',' ActionParam
				;
				
ActionParam : TypedConstant
            | TypedLocalVar
			;

TypedConstant : Constant
              | '(' IDENTIFIER ')' Constant
			  ;

TypedLocalVar : LOCAL_VAR
              | '(' IDENTIFIER ')' LOCAL_VAR
			  ;
