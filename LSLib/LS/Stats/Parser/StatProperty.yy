%namespace LSLib.LS.Stats.Properties
%partial 
%visibility public
%parsertype StatPropertyParser
%tokentype StatPropertyTokens
%YYSTYPE System.Object

%start Root

/* Trigger Lexemes */
%token EXPR_PROPERTIES
%token EXPR_CONDITIONS
%token EXPR_REQUIREMENTS

/* Requirements */
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
Root : EXPR_PROPERTIES Properties { $$ = $2; }
     | EXPR_CONDITIONS Conditions { $$ = $2; }
     | EXPR_REQUIREMENTS Requirements { $$ = $2; }
     ;


/******************************************************************
 *
 *
 *                        REQUIREMENTS PARSING
 *
 *
 ******************************************************************/
 
Requirements : /* empty */ { $$ = MakeRequirements(); }
             | UnaryRequirement { $$ = AddRequirement(MakeRequirements(), $1); }
             | Requirements ';'
             | Requirements ';' UnaryRequirement { $$ = AddRequirement($1, $3); }
             ;

UnaryRequirement : Requirement
                 | '!' Requirement { $$ = MakeNotRequirement($2); }
                 ;

Requirement : NAME { $$ = MakeRequirement($1); }
            | NAME INTEGER { $$ = MakeIntRequirement($1, $2); }
            | REQUIREMENT_TAG TEXT { $$ = MakeTagRequirement($1, $2); }
            | REQUIREMENT_TAG NAME { $$ = MakeTagRequirement($1, $2); }
            ;


/******************************************************************
 *
 *
 *                        PROPERTY PARSING
 *
 *
 ******************************************************************/
 
Properties : /* empty */ { $$ = MakePropertyList(); }
           | Property { $$ = AddProperty(MakePropertyList(), $1); }
           | Properties ';'
           | Properties ';' Property { $$ = AddProperty($1, $3); }
           ;

Property : PropContext PropCondition PropAction { $$ = MakeProperty($1, $2, $3); };

PropContext : { $$ = PropertyContext.None; } /* empty */
            | CTX_SELF ':' { $$ = PropertyContext.Self; }
            | CTX_SELF ':' PropSelfContext ':' { $$ = $3; }
            | CTX_TARGET ':' { $$ = PropertyContext.Target; }
            | CTX_AOE ':' { $$ = PropertyContext.AoE; }
            ;

PropSelfContext : CTX_ON_HIT { $$ = PropertyContext.SelfOnHit; }
                | CTX_ON_EQUIP { $$ = PropertyContext.SelfOnEquip; }
                ;

PropCondition : /* empty */
              | IF '(' ConditionExpr ')' ':' { $$ = $3; }
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

ActCustomProperty : ACT_CUSTOM_PROPERTY { $$ = MakeAction($1, MakeArgumentList()); };
ActSurfaceChange : ACT_SURFACE_CHANGE SurfaceChangeArgs { $$ = MakeAction($1, $2); };
ActGameAction : ACT_GAME_ACTION GameActionArgs { $$ = MakeAction($1, $2); };
ActCreateSurface : ACT_CREATE_SURFACE CreateSurfaceArgs { $$ = MakeAction($1, $2); };
ActSwapPlaces : ACT_SWAP_PLACES SwapPlacesArgs { $$ = MakeAction($1, $2); };
ActPickup : ACT_PICKUP PickupArgs { $$ = MakeAction($1, $2); };
/* Args: HealType */
ActEqualize : ACT_EQUALIZE ',' TextArg { $$ = MakeAction($1, MakeArgumentList($3)); };
ActResurrect : ACT_RESURRECT ResurrectArgs { $$ = MakeAction($1, $2); };
ActSabotage : ACT_SABOTAGE SabotageArgs { $$ = MakeAction($1, $2); };
ActSummon : ACT_SUMMON ',' TextArg SummonOptArgs { $$ = MakeAction($1, PrependArgumentList($3, $4)); };
ActForce : ACT_FORCE ',' IntArg { $$ = MakeAction($1, MakeArgumentList($3)); };
ActCleanse : ACT_CLEANSE ':' NAME { $$ = MakeAction($1, MakeArgumentList($3)); };
ActStatus : StatusBoost StatusName StatusArgs { $$ = MakeStatusBoost($1, $2, $3); };

SurfaceChangeArgs : /* empty */ { $$ = MakeArgumentList(); }
                  | ',' IntArg { $$ = MakeArgumentList($2); }
                  | ',' IntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4); }
                  | ',' IntArg ',' OptionalIntArg ',' IntArg { $$ = MakeArgumentList($2, $4, $6); }
                  | ',' IntArg ',' OptionalIntArg ',' IntArg ',' IntArg { $$ = MakeArgumentList($2, $4, $6, $8); }
                  ;

/* TODO -- specific arg checks for each action !!! */
GameActionArgs : /* empty */ { $$ = MakeArgumentList(); }
               | ',' IntArg { $$ = MakeArgumentList($2); }
               | ',' IntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4); }
               | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg { $$ = MakeArgumentList($2, $4, $6); }
               | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg { $$ = MakeArgumentList($2, $4, $6, $8); }
               | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4, $6, $8, $10); }
               ;

/* Radius; Duration; SurfaceType/DamageType; %Chance */
CreateSurfaceArgs : /* empty */ { $$ = MakeArgumentList(); }
                  | ',' IntArg { $$ = MakeArgumentList($2); }
                  | ',' IntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4); }
                  | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg { $$ = MakeArgumentList($2, $4, $6); }
                  | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg { $$ = MakeArgumentList($2, $4, $6, $8); }
                  | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4, $6, $8, $10); }
                  ;

/* -; -; CasterEffect:TargetEffect */
SwapPlacesArgs : /* empty */ { $$ = MakeArgumentList(); }
               | ',' OptionalIntArg { $$ = MakeArgumentList($2); }
               | ',' OptionalIntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4); }
               | ',' OptionalIntArg ',' OptionalIntArg ',' OptionalTextArg { $$ = MakeArgumentList($2, $4, $6); }
               | ',' OptionalIntArg ',' OptionalIntArg ',' OptionalTextArg ':' OptionalTextArg { $$ = MakeArgumentList($2, $4, $6, $8); }
               ;

/* -; -; TargetEffect */
PickupArgs : /* empty */ { $$ = MakeArgumentList(); }
           | ',' OptionalIntArg { $$ = MakeArgumentList($2); }
           | ',' OptionalIntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4); }
           | ',' OptionalIntArg ',' OptionalIntArg ',' OptionalTextArg { $$ = MakeArgumentList($2, $4, $6); }
           ;

ResurrectArgs : /* empty */ { $$ = MakeArgumentList(); }
              | ',' IntArg { $$ = MakeArgumentList($2); }
              | ',' IntArg ',' IntArg { $$ = MakeArgumentList($2, $4); }
              ;

SabotageArgs : /* empty */ { $$ = MakeArgumentList(); }
             | ',' IntArg { $$ = MakeArgumentList($2); }
             ;
			 
/* TODO - Arg #2 - TotemArg */
SummonOptArgs : /* empty */ { $$ = MakeArgumentList(); }
              | ',' IntArg { $$ = MakeArgumentList($2); }
              | ',' IntArg ',' OptionalTextArg { $$ = MakeArgumentList($2, $4); }
              | ',' IntArg ',' OptionalTextArg ',' TextArg { $$ = MakeArgumentList($2, $4, $6); }
              ;

StatusArgs : /* empty */ { $$ = MakeArgumentList(); }
           | ',' IntArg { $$ = MakeArgumentList($2); }
           | ',' IntArg ',' OptionalIntArg { $$ = MakeArgumentList($2, $4); }
           | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg { $$ = MakeArgumentList($2, $4, $6); }
           | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg { $$ = MakeArgumentList($2, $4, $6, $8); }
           | ',' IntArg ',' OptionalIntArg ',' OptionalTextArg ',' IntArg ',' IntArg { $$ = MakeArgumentList($2, $4, $6, $8, $10); }
           ;

IntArg : INTEGER { $$ = Int32.Parse($1 as string); };

OptionalIntArg : /* empty */
               | IntArg
               ;

TextArg : INTEGER
        | NAME
        | TEXT
		| ACT_SURFACE_CHANGE
		| REQUIREMENT_TAG
        ;

OptionalTextArg : /* empty */ { $$ = ""; }
                | TextArg
                ;

StatusName : NAME;

StatusBoost : /* empty */ { $$ = MakeStatusBoostType(StatusBoostType.None, null); }
            | ACT_AOEBOOST ':' { $$ = MakeStatusBoostType(StatusBoostType.AoE, null); }
            | ACT_SURFACEBOOST '(' SurfaceList ')' ':' { $$ = MakeStatusBoostType(StatusBoostType.Surface, $3); }
            ;

SurfaceList : Surface { $$ = AddSurface(MakeSurfaceList(), $1); }
            | SurfaceList '|' Surface { $$ = AddSurface($1, $3); }
            ;

Surface : NAME { $$ = MakeSurface($1); };

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

Condition : NAME { $$ = MakeCondition($1, null); }
          | CTX_SELF { $$ = MakeCondition("Self", null); } /* Conflict with "SELF" action context token */
          | ACT_SUMMON { $$ = MakeCondition($1, null); } /* Token conflict between actions and the condition "Summon" */
          | NAME ':' TextArg { $$ = MakeCondition($1, $3); }
          ;

UnaryCondition : ConditionBlock
               | UnaryOperator ConditionBlock { $$ = MakeNotCondition($2); }
               ;

ConditionBlock : BracketedConditionExpr
               | Condition
               ;

BracketedConditionExpr : '(' ConditionExpr ')' { $$ = $2; };

UnaryOperator : '!';

ConditionExpr : UnaryCondition
              | ConditionExpr BinaryOperator UnaryCondition { $$ = MakeBinaryCondition($1, $2, $3); }
              ;

BinaryOperator : '|' { $$ = ConditionOperator.Or; }
               | '&' { $$ = ConditionOperator.And; }
               ;