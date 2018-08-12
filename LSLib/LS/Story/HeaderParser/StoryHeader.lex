%namespace LSLib.LS.Story.HeaderParser
%visibility public
%scannertype HeaderScanner
%scanbasetype HeaderScanBase
%tokentype HeaderTokens

letter [a-zA-Z]
digit [0-9]

%x C_COMMENT

%%

/* Reserved words */
"option"     return (int)HeaderTokens.OPTION;
"type"       return (int)HeaderTokens.TYPE;
"alias_type" return (int)HeaderTokens.ALIAS_TYPE;
"syscall"    return (int)HeaderTokens.SYSCALL;
"sysquery"   return (int)HeaderTokens.SYSQUERY;
"query"      return (int)HeaderTokens.QUERY;
"call"       return (int)HeaderTokens.CALL;
"event"      return (int)HeaderTokens.EVENT;
"in"         return (int)HeaderTokens.IN;
"out"        return (int)HeaderTokens.OUT;

/* Special characters */
"{"                return (int)'{';
"}"                return (int)'}';
"("                return (int)'(';
")"                return (int)')';
"["                return (int)'[';
"]"                return (int)']';
","                return (int)',';
[ \t\v\r\n\f]      ;

({letter}|_)({letter}|{digit}|_)+   { yylval = MakeLiteral(yytext); return (int)HeaderTokens.IDENTIFIER; }
{digit}({digit})*                   { yylval = MakeLiteral(yytext); return (int)HeaderTokens.INTEGER; }

/* Comments */
[/][/][^\n]*\n          ;

"/*"                    { BEGIN(C_COMMENT); }
<C_COMMENT>"*/"         { BEGIN(INITIAL); }
<C_COMMENT>\n           { }
<C_COMMENT>.            { }

. return ((int)HeaderTokens.BAD);
