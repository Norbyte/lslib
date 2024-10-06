%namespace LSLib.Stats.Requirements
%visibility public
%scannertype RequirementScanner
%scanbasetype RequirementScanBase
%tokentype RequirementTokens

letter [a-zA-Z_]
digit [0-9]
namechar [a-zA-Z0-9_]
nonseparator [^,;:()\[\]! ]

%%

/* Special characters */
";"          return (int)';';
"!"          return (int)'!';
[ \t]        ;

{letter}({namechar})+ { yylval = yytext; return (int)RequirementTokens.NAME; }
(-)?{digit}({digit})* { yylval = yytext; return (int)RequirementTokens.INTEGER; }
({nonseparator})+     { yylval = yytext; return (int)RequirementTokens.TEXT; }

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
