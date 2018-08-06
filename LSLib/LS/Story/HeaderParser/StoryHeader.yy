%namespace LSLib.LS.Story.HeaderParser
%partial 
%visibility public
%parsertype HeaderParser
%tokentype HeaderTokens
%YYSTYPE LSLib.LS.Story.HeaderParser.ASTNode

%start StoryHeaderFile

/* Reserved word */
%token OPTION
%token TYPE
%token ALIAS_TYPE
%token SYSCALL
%token SYSQUERY
%token QUERY
%token CALL
%token EVENT
%token IN
%token OUT

/* Special token for invalid characters */
%token BAD

/* Function, type or variable identifier */
%token IDENTIFIER
/* Integer literal */
%token INTEGER

%%

StoryHeaderFile: StoryHeader;

StoryHeader : Declarations;

Declarations : /* empty */ { $$ = MakeDeclarationList(); }
             | Declarations Declaration { $$ = MakeDeclarationList($1, $2); }
             ;

Declaration : Option
            | Alias
            | Function
            ;

Option : OPTION IDENTIFIER { $$ = MakeOption($2); };

Alias  : ALIAS_TYPE '{' IDENTIFIER ',' INTEGER ',' INTEGER '}'
       { $$ = MakeAlias($3, $5, $7); };

Function : InOutFunctionType IDENTIFIER '(' InOutFunctionParams ')' FunctionMetadata { $$ = MakeFunction($1, $2, $4, $6); }
         | InFunctionType IDENTIFIER '(' InFunctionParams ')' FunctionMetadata { $$ = MakeFunction($1, $2, $4, $6); }
         ;

FunctionMetadata : '(' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ')'
                 { $$ = MakeFunctionMetadata($2, $4, $6, $8); };

InOutFunctionType : SYSQUERY { $$ = MakeFunctionType(ASTFunctionType.SysQuery); }
                  | QUERY { $$ = MakeFunctionType(ASTFunctionType.Query); }
                  ;

InFunctionType : SYSCALL { $$ = MakeFunctionType(ASTFunctionType.SysCall); }
               | CALL { $$ = MakeFunctionType(ASTFunctionType.Call); }
               | EVENT { $$ = MakeFunctionType(ASTFunctionType.Event); }
               ;

InOutFunctionParams : /* empty */ { $$ = MakeFunctionParamList(); }
                    | InOutFunctionParam { $$ = MakeFunctionParamList($1); }
                    | InOutFunctionParams ',' InOutFunctionParam { $$ = MakeFunctionParamList($1, $3); }
                    ;

InFunctionParams : /* empty */ { $$ = MakeFunctionParamList(); }
                 | InFunctionParam { $$ = MakeFunctionParamList($1); }
                 | InFunctionParams ',' InFunctionParam { $$ = MakeFunctionParamList($1, $3); }
                 ;

InOutFunctionParam : '[' IN ']' '(' IDENTIFIER ')' IDENTIFIER { $$ = MakeParam(ASTFunctionParamDirection.In, $5, $7); }
                   | '[' OUT ']' '(' IDENTIFIER ')' IDENTIFIER { $$ = MakeParam(ASTFunctionParamDirection.Out, $5, $7); }
                   ;

InFunctionParam : '(' IDENTIFIER ')' IDENTIFIER { $$ = MakeParam($2, $4); };
