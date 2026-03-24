%namespace LSLib.Stats.Expression
%partial 
%visibility public
%parsertype ExpressionParser
%tokentype ExpressionTokens
%YYSTYPE System.Object

%start Root

/* Special token for invalid characters */
%token BAD

/* Integer literal */
%token INTEGER
/* Text-like (unquoted) literal */
%token NAME
/* eg. 1d10 */
%token DICE_ROLL

/* Target/Owner/Cause */
%token CONTEXT_TYPE
/* Duration/Amount */
%token STATUS_PROPERTY
/* eg. Strength, StrengthModifier, ... */
%token VARIABLE_REF

/* "LevelMapValue" */
%token LEVEL_MAP_VALUE
/* "ClassLevel" */
%token CLASS_LEVEL
/* "ResourceRoll" */
%token RESOURCE_ROLL
/* "foreach" */
%token FOR_EACH
/* "Max" */
%token MAX_EXPR

/* Lua binary operator */
%token BINOP
/* Lua binary or unary operator */
%token BIN_OR_UNOP

%%

Root : LExp;

LVar : VARIABLE_REF
     // Status duration (eg. Cause.StrengthModifier)
     | NAME '.' STATUS_PROPERTY
     ;

LOptionalExpList : /* empty */
                 | LExpList
                 ;

LExpList : LExp 
         | LExpList ',' LExp
         ;

LExp : LExpNoUnOp
     | LUnOp LExp
     ;

LExpNoUnOp : INTEGER
     | DICE_ROLL
     | LVar
     | LPrefixExp
     | LExpNoUnOp LBinOp LExp 
     ;

LQualifiedExp : LVariableExp
              // Variable or modifier with context (eg. Cause.StrengthModifier)
              | CONTEXT_TYPE '.' LVariableExp
              ;

LVariableExp : LVar
             | LevelMapValue
             | ClassLevel
             | ResourceRoll
             ;

LPrefixExp : LQualifiedExp
           | ForEach
           | Max
           | '(' LExp ')'
           ;

// eg. LevelMapValue(WildShapeDamageLow)
LevelMapValue : LEVEL_MAP_VALUE '(' NAME ')';

// eg. ClassLevel(Paladin)
ClassLevel : CLASS_LEVEL '(' NAME ')';

// eg. ResourceRoll(BardicInspiration,1)
ResourceRoll : RESOURCE_ROLL '(' NAME ',' INTEGER ')';

// eg. foreach(1+BLADESONG_HEALING_CHARGE.Duration, 1d6)
ForEach : FOR_EACH '(' LExp ',' LExp ')';

Max : MAX_EXPR '(' LExpList ')';

LBinOp : BINOP
       | BIN_OR_UNOP
       ;

LUnOp : BIN_OR_UNOP;
