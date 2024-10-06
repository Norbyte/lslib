%namespace LSLib.Stats.RollConditions
%partial 
%visibility public
%parsertype RollConditionParser
%tokentype RollConditionTokens
%YYSTYPE System.Object

%start Root

/* Name-like expression */
%token NAME
/* Text-like (unquoted) literal */
%token TEXT

%%

Root : RollConditions;

RollConditions : RollConditionOrEmpty { $$ = AddCondition(MakeConditions(), $1); }
               | RollConditions ';' RollConditionOrEmpty { $$ = AddCondition($1, $3); }
               ;

RollConditionOrEmpty : /* empty */
                     | RollCondition
                     ;

RollCondition : NAME '[' Expression ']' { $$ = MakeCondition($1, $3); }
              | NAME
              | TEXT
              | NAME Expression { $$ = ConcatExpression($1, $2); }
              | TEXT Expression { $$ = ConcatExpression($1, $2); }
              ;

Expression : NAME
           | TEXT
           | Expression NAME { $$ = ConcatExpression($1, $2); }
           | Expression TEXT { $$ = ConcatExpression($1, $2); }
           ;
