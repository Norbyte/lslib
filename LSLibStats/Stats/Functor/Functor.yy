%namespace LSLib.Stats.Functors
%partial 
%visibility public
%parsertype FunctorParser
%tokentype FunctorTokens
%YYSTYPE System.Object

%start Root

/* Trigger Lexemes */
%token EXPR_FUNCTORS
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
Root : EXPR_FUNCTORS Functors { $$ = $2; }
     | EXPR_DESCRIPTION_PARAMS OptionalArgs { $$ = $2; }
     ;

/******************************************************************
 *
 *                        FUNCTOR PARSING
 *
 ******************************************************************/
 
Functors : /* empty */ { $$ = MakeFunctorList(); }
         | Functor { $$ = AddFunctor(MakeFunctorList(), $1); }
         | Functors ';'
         | Functors ';' Functor { $$ = AddFunctor($1, $3); }
         ;

TextKeyFunctors : TEXT_KEY '[' Functors ']' { $$ = SetTextKey($3, $1); };

Functor : Contexts Condition Call { $$ = MakeFunctor($1, $2, $3); }
        | TextKeyFunctors
        ;

Contexts : /* empty */
         | ContextList { $$ = $1; }
         ;

ContextList : Context { $$ = $1; }
            | ContextList Context { $$ = $1; }
            ;

Context : CONTEXT ':' { $$ = $1; };

Condition : /* empty */
          | IF '(' NonEmptyArg ')' ':' { $$ = $3; }
          ;

Call : FunctorName OptionalArgList { $$ = MakeAction($1, $2); };

FunctorName : NAME { $$ = $1; MarkActionStart(); };

OptionalArgList : /* empty */ { $$ = MakeArgumentList(); }
                | '(' OptionalArgs ')' { $$ = $2; }
                ;

OptionalArgs : /* empty */ { $$ = MakeArgumentList(); }
             | Args
             ;

Args : NonEmptyArg { $$ = AddArgument(MakeArgumentList(), $1); }
     | Args ',' Arg { $$ = AddArgument($1, $3); }
     ;

Arg : /* empty */
    | NonEmptyArg
    ;

NonEmptyArg : ArgStart LuaRoot ArgEnd { $$ = $3; };

ArgStart : /* empty */ { InitLiteral(); };

ArgEnd : /* empty */ { $$ = MakeLiteral(); };

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
