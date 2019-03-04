%namespace LSLib.DebuggerFrontend.ExpressionParser
%partial 
%visibility public
%parsertype ExpressionParser
%tokentype ExpressionTokens
%YYSTYPE LSLib.DebuggerFrontend.ExpressionParser.ExpressionNode

%start Expression

/* Reserved word */
%token NOT

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

Expression : Statement;

Statement : IDENTIFIER { $$ = MakeStatement($1, false); }
          | IDENTIFIER '(' ParamList ')' { $$ = MakeStatement($1, $3, false); }
          | NOT IDENTIFIER '(' ParamList ')' { $$ = MakeStatement($2, $4, true); }
          ;

ParamList : /* Empty */ { $$ = MakeParamList(); }
          | Param { $$ = MakeParamList($1); }
          | ParamList ',' Param { $$ = MakeParamList($1, $3); }
          ;
                
Param : TypedConstant { $$ = $1; }
      | TypedLocalVar { $$ = $1; }
      ;

TypedConstant : Constant { $$ = $1; }
              | '(' IDENTIFIER ')' Constant { $$ = MakeTypedConstant($2, $4); }
              ;

TypedLocalVar : LOCAL_VAR { $$ = MakeLocalVar($1); }
              | '(' IDENTIFIER ')' LOCAL_VAR { $$ = MakeLocalVar($2, $4); }
              ;

Constant : GUIDSTRING { $$ = MakeConstGuidString($1); }
         | STRING { $$ = MakeConstString($1); }
         | INTEGER { $$ = MakeConstInteger($1); }
         | FLOAT { $$ = MakeConstFloat($1); }
         ;
