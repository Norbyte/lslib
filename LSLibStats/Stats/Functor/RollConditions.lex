%namespace LSLib.Stats.RollConditions
%visibility public
%scannertype RollConditionScanner
%scanbasetype RollConditionScanBase
%tokentype RollConditionTokens

namechar [a-zA-Z0-9_]
nonseparator [^;\[\] ]

%%

/* Special characters */
";"          return (int)';';
"["          return (int)'[';
"]"          return (int)']';
[ \t]        ;

({namechar})+ { yylval = yytext; return (int)RollConditionTokens.NAME; }
({nonseparator})+ { yylval = yytext; return (int)RollConditionTokens.TEXT; }

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
