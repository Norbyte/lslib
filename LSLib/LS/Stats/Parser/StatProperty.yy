%namespace LSLib.LS.Stats.Properties
%partial 
%visibility public
%parsertype StatPropertyParser
%tokentype StatPropertyTokens
%YYSTYPE System.Object

%start Root

/* Trigger Lexemes */
%token EXPR_PROPERTIES
%token EXPR_DESCRIPTION_PARAMS

/* Reserved words */
%token IF

/* Functor Context */
%token CONTEXT
/* Status/Tag name */
%token NAME
/* Known text keys */
%token TEXT_KEY
/* Integer literal */
%token INTEGER
/* Text-like (unquoted) literal */
%token TEXT
/* eg. 1d10 */
%token DICE_ROLL

%%

/* A special "trigger word" is prepended to support parsing multiple types from the same lexer/parser */
Root : EXPR_PROPERTIES Properties { $$ = $2; }
     | EXPR_DESCRIPTION_PARAMS OptionalFunctorArgs { $$ = $2; }
     ;

/******************************************************************
 *
 *                        PROPERTY PARSING
 *
 ******************************************************************/
 
Properties : /* empty */ { $$ = MakePropertyList(); }
           | Property { $$ = AddProperty(MakePropertyList(), $1); }
           | Properties ';'
           | Properties ';' Property { $$ = AddProperty($1, $3); }
           ;

TextKeyProperties : TEXT_KEY '[' Properties ']' { $$ = SetTextKey($3, $1); };

Property : PropContexts PropCondition FunctorCall { $$ = MakeProperty($1, $2, $3); }
         | TextKeyProperties
         ;

PropContexts : /* empty */
             | PropContextList { $$ = $1; }
             ;

PropContextList : PropContext { $$ = $1; }
                | PropContextList PropContext { $$ = $1; }
                ;

PropContext : CONTEXT ':' { $$ = $1; };

PropCondition : /* empty */
              | IF '(' NonEmptyFunctorArg ')' ':' { $$ = $3; }
              ;

FunctorCall : FunctorName OptionalFunctorArgList { $$ = MakeAction($1, $2); };

FunctorName : NAME { $$ = $1; MarkActionStart(); };

OptionalFunctorArgList : /* empty */ { $$ = MakeArgumentList(); }
                       | '(' OptionalFunctorArgs ')' { $$ = $2; }
                       ;

OptionalFunctorArgs : /* empty */ { $$ = MakeArgumentList(); }
                    | FunctorArgs
                    ;

FunctorArgs : NonEmptyFunctorArg { $$ = AddArgument(MakeArgumentList(), $1); }
            | FunctorArgs ',' FunctorArg { $$ = AddArgument($1, $3); }
            ;

FunctorArg : /* empty */
           | NonEmptyFunctorArg
           ;

NonEmptyFunctorArg : FunctorArgStart LuaRoot FunctorArgEnd { $$ = $3; };

FunctorArgStart : /* empty */ { InitLiteral(); };

FunctorArgEnd : /* empty */ { $$ = MakeLiteral(); };

LuaRoot : LuaRootSymbol
        | LuaRoot LuaRootSymbol
        | LuaRoot '(' LuaExpr ')'
        | LuaRoot '(' ')'
        | '(' LuaExpr ')'
        ;

LuaExpr : LuaSymbol
        | LuaExpr LuaSymbol
        | LuaExpr '(' LuaExpr ')'
        | '(' LuaExpr ')'
        | LuaExpr '(' ')'
        ;

LuaRootSymbol : NAME
              | INTEGER
              | TEXT
              | CONTEXT
              | DICE_ROLL
              | ':'
              | '!'
              | ';'
              | '-'
              ;

LuaSymbol : LuaRootSymbol
          | ','
          ;
