%namespace LSLib.DebuggerFrontend.ExpressionParser
%visibility public
%scannertype ExpressionScanner
%scanbasetype ExpressionScanBase
%tokentype ExpressionTokens

/* Expression lexer - essentially a simplified version of Goal.yy */

letter [a-zA-Z]
digit [0-9]
hex [0-9a-fA-F]

%%

/* Reserved words */
"NOT"              return (int)ExpressionTokens.NOT;

/* Special characters */
"("                return (int)'(';
")"                return (int)')';
","                return (int)',';
"."                return (int)'.';
[ \t\v\r\n\f]      ;

{letter}({letter}|{digit}|_)*   { yylval = MakeLiteral(yytext); return (int)ExpressionTokens.IDENTIFIER; }
/* GUID strings */
({hex}{8})-({hex}{4})-({hex}{4})-({hex}{4})-({hex}{12}) { yylval = MakeLiteral(yytext); return (int)ExpressionTokens.GUIDSTRING; }
/* Special case for identifiers with a GUID string at the end */
{letter}({letter}|{digit}|_|-)*({hex}{8})-({hex}{4})-({hex}{4})-({hex}{4})-({hex}{12}) { yylval = MakeLiteral(yytext); return (int)ExpressionTokens.GUIDSTRING; }
/* Variables with a leading underscore are local, and are handled differently */
_({letter}|{digit}|_)*          { yylval = MakeLiteral(yytext); return (int)ExpressionTokens.LOCAL_VAR; }
[+\-]?{digit}({digit})*         { yylval = MakeLiteral(yytext); return (int)ExpressionTokens.INTEGER; }
[+\-]?{digit}+\.{digit}+        { yylval = MakeLiteral(yytext); return (int)ExpressionTokens.FLOAT; }
L?\"(\\.|[^\\"])*\"             { yylval = MakeString(yytext); return (int)ExpressionTokens.STRING; }

. return ((int)ExpressionTokens.BAD);
