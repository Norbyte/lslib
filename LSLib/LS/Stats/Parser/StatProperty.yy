%namespace LSLib.LS.Stats.StatPropertyParser
%partial 
%visibility public
%parsertype StatPropertyParser
%tokentype StatPropertyTokens
%YYSTYPE LSLib.LS.Stats.StatPropertyParser.StatPropertyNode

%start Root

/* Trigger Lexemes */
%token EXPR_PROPERTIES
%token EXPR_CONDITIONS
%token EXPR_REQUIREMENTS

/* Requirements */
%token REQUIREMENT_NO_ARG
%token REQUIREMENT_INT_ARG
%token REQUIREMENT_TAG

/* Reserved words */
%token IF

/* Property Contexts */
%token CTX_SELF
%token CTX_TARGET
%token CTX_AOE
%token CTX_ON_HIT
%token CTX_ON_EQUIP

/* Action Types */
%token ACT_CUSTOM_PROPERTY
%token ACT_SURFACE_CHANGE
%token ACT_GAME_ACTION
%token ACT_CREATE_SURFACE
%token ACT_SWAP_PLACES
%token ACT_EQUALIZE
%token ACT_PICKUP
%token ACT_RESURRECT
%token ACT_SABOTAGE
%token ACT_SUMMON
%token ACT_FORCE
%token ACT_CLEANSE
%token ACT_AOEBOOST
%token ACT_SURFACEBOOST

/* Misc Constants */
%token SURFACE_TYPE
%token SURFACE_TYPE_EX
%token SURFACE_STATE
%token SURFACE_TYPE_OR_STATE
%token SKILL_CONDITION
%token SKILL_CONDITION_1ARG
%token SKILL_CONDITION_SURFACE
%token SKILL_CONDITION_IN_SURFACE

%token COMBAT

/* Special token for invalid characters */
%token BAD

/* Status/Tag name */
%token NAME
/* Integer literal */
%token INTEGER
/* Text-like (unquoted) literal */
%token TEXT

%%

/* A special "trigger word" is prepended to support parsing multiple types from the same lexer/parser */
Root : EXPR_PROPERTIES Properties
     | EXPR_CONDITIONS Conditions
     | EXPR_REQUIREMENTS Requirements
     ;


/******************************************************************
 *
 *
 *                        REQUIREMENTS PARSING
 *
 *
 ******************************************************************/
 
Requirements : /* empty */
             | UnaryRequirement
             | Requirements ';'
             | Requirements ';' UnaryRequirement
             ;

UnaryRequirement : Requirement
                 | '!' Requirement
                 ;

Requirement : RequirementNoArg
            | RequirementIntArg
            | RequirementTag
            ;

RequirementNoArg : REQUIREMENT_NO_ARG
                 | COMBAT /* Token conflict between requirements and the skill condition "Combat" */
                 ;

RequirementIntArg : REQUIREMENT_INT_ARG
                  | REQUIREMENT_INT_ARG INTEGER
                  ;
				  
RequirementTag : REQUIREMENT_TAG TEXT
               | REQUIREMENT_TAG NAME
               ;


/******************************************************************
 *
 *
 *                        PROPERTY PARSING
 *
 *
 ******************************************************************/
 
Properties : /* empty */
           | Property
           | Properties ';'
           | Properties ';' Property
           ;

Property : PropContext PropCondition PropAction;

PropContext : /* empty */
            | CTX_SELF ':'
            | CTX_SELF ':' PropSelfContext ':'
            | CTX_TARGET ':'
            | CTX_AOE ':'
            ;

PropSelfContext : CTX_ON_HIT
                | CTX_ON_EQUIP
                ;

PropCondition : /* empty */
              | IF '(' ConditionExpr ')' ':'
              ;

PropAction : ActCustomProperty
           | ActSurfaceChange
           | ActGameAction
           | ActCreateSurface
           | ActSwapPlaces
           | ActPickup
           | ActEqualize
           | ActResurrect
           | ActSabotage
           | ActSummon
           | ActForce
           | ActCleanse
           | ActStatus
           ;

ActCustomProperty : ACT_CUSTOM_PROPERTY;
ActSurfaceChange : ACT_SURFACE_CHANGE SurfaceChangeArgs;
ActGameAction : ACT_GAME_ACTION GameActionArgs;
ActCreateSurface : ACT_CREATE_SURFACE CreateSurfaceArgs;
ActSwapPlaces : ACT_SWAP_PLACES SwapPlacesArgs;
ActPickup : ACT_PICKUP PickupArgs;
/* Args: HealType */
ActEqualize : ACT_EQUALIZE ',' TextArg;
ActResurrect : ACT_RESURRECT ResurrectArgs;
ActSabotage : ACT_SABOTAGE SabotageArgs;
ActSummon : ACT_SUMMON ',' TextArg SummonOptArgs;
ActForce : ACT_FORCE ',' IntArg;
ActCleanse : ACT_CLEANSE ':' NAME;
ActStatus : StatusBoost StatusName StatusArgs;

SurfaceChangeArgs : /* empty */
                  | ',' IntArg
                  | ',' IntArg ',' OptionalIntArg
                  | ',' IntArg ',' OptionalIntArg ',' IntArg
                  | ',' IntArg ',' OptionalIntArg ',' IntArg ',' IntArg
                  ;

/* TODO -- specific arg checks for each action !!! */
GameActionArgs : /* empty */
               | ',' IntArg
               | ',' IntArg ',' OptionalIntArg
               | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg
               | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg
               | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg ',' OptionalIntArg
               ;

/* Radius; Duration; SurfaceType/DamageType; %Chance */
CreateSurfaceArgs : /* empty */
                  | ',' IntArg
                  | ',' IntArg ',' OptionalIntArg
                  | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg
                  | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg
                  | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg ',' OptionalIntArg
                  ;

/* -; -; CasterEffect:TargetEffect */
SwapPlacesArgs : /* empty */
               | ',' OptionalIntArg
               | ',' OptionalIntArg ',' OptionalIntArg
               | ',' OptionalIntArg ',' OptionalIntArg ','
               | ',' OptionalIntArg ',' OptionalIntArg ',' OptionalTextArg ':' OptionalTextArg
               ;

/* -; -; TargetEffect */
PickupArgs : /* empty */
           | ',' OptionalIntArg
           | ',' OptionalIntArg ',' OptionalIntArg
           | ',' OptionalIntArg ',' OptionalIntArg ',' OptionalTextArg
           ;

ResurrectArgs : /* empty */
              | ',' IntArg
              | ',' IntArg ',' IntArg
              ;

SabotageArgs : /* empty */
             | ',' IntArg
             ;
			 
/* TODO - Arg #2 - TotemArg */
SummonOptArgs : /* empty */
              | ',' IntArg
              | ',' IntArg ',' OptionalTextArg
              | ',' IntArg ',' OptionalTextArg ',' TextArg
              ;

StatusArgs : /* empty */
           | ',' IntArg
           | ',' IntArg ',' OptionalIntArg
           | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg
           | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg
           | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg ',' IntArg
           ;

IntArg : INTEGER;

OptionalIntArg : /* empty */
               | IntArg
               ;

TextArg : INTEGER
        | NAME
        | TEXT
		| SURFACE_TYPE
		| SURFACE_STATE
		| SURFACE_TYPE_OR_STATE
		| SKILL_CONDITION
		| COMBAT
		| ACT_SURFACE_CHANGE
		| REQUIREMENT_NO_ARG
		| REQUIREMENT_INT_ARG
		| REQUIREMENT_TAG
        ;

OptionalTextArg : /* empty */
                | TextArg
                ;

StatusName : NAME
/* TODO - Disabled to track down common errors
           | SURFACE_TYPE_OR_STATE
           | SURFACE_TYPE
           | SURFACE_STATE */
           ;

StatusBoost : /* empty */
            | ACT_AOEBOOST ':'
            | ACT_SURFACEBOOST '(' SurfaceList ')' ':'
            ;

SurfaceList : SurfaceType
            | SurfaceList '|' SurfaceType
            ;


SurfaceState : SURFACE_STATE
             | SURFACE_TYPE_OR_STATE
             ;
			
SurfaceType : SURFACE_TYPE
            | SURFACE_TYPE_OR_STATE
            ;

/******************************************************************
 *
 *
 *                        CONDITION PARSING
 *
 *
 ******************************************************************/

Conditions : /* empty */
           | ConditionExpr
           ;

Condition : SKILL_CONDITION
          | COMBAT /* Token conflict between requirements and the condition "Combat" */
          | ACT_SUMMON /* Token conflict between actions and the condition "Summon" */
          | SKILL_CONDITION_1ARG ':' TextArg
          | SKILL_CONDITION_IN_SURFACE ':' SURFACE_TYPE_EX
          | SKILL_CONDITION_SURFACE ':' SurfaceState
          ;

UnaryCondition : ConditionBlock
               | UnaryOperator ConditionBlock
               ;

ConditionBlock : BracketedConditionExpr
               | Condition
               ;

BracketedConditionExpr : '(' ConditionExpr ')';

UnaryOperator : '!';

ConditionExpr : UnaryCondition
              | ConditionExpr BinaryOperator UnaryCondition
              ;

BinaryOperator : '|'
               | '&'
               ;