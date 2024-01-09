%namespace LSLib.LS.Stats.Lua
%visibility public
%scannertype StatLuaScanner
%scanbasetype StatLuaScanBase
%tokentype StatLuaTokens

letter [a-zA-Z_]
digit [0-9]
namechar [a-zA-Z0-9_]
nonseparator [^,;:()\[\]!+*/^&%~|><=.# ]

%%

/* Special characters */
":"          return (int)':';
"("          return (int)'(';
")"          return (int)')';
"["          return (int)'[';
"]"          return (int)']';
","          return (int)',';
";"          return (int)';';
"."          return (int)'.';
[ ]          ;


"nil"   return (int)StatLuaTokens.LUA_RESERVED_VAL;
"false" return (int)StatLuaTokens.LUA_RESERVED_VAL;
"true"  return (int)StatLuaTokens.LUA_RESERVED_VAL;
"..."   return (int)StatLuaTokens.LUA_RESERVED_VAL;

"+"     return (int)StatLuaTokens.BINOP;
"*"     return (int)StatLuaTokens.BINOP;
"/"     return (int)StatLuaTokens.BINOP;
"//"    return (int)StatLuaTokens.BINOP;
"^"     return (int)StatLuaTokens.BINOP;
"%"     return (int)StatLuaTokens.BINOP;
"&"     return (int)StatLuaTokens.BINOP;
"|"     return (int)StatLuaTokens.BINOP;
">>"    return (int)StatLuaTokens.BINOP;
"<<"    return (int)StatLuaTokens.BINOP;
".."    return (int)StatLuaTokens.BINOP;
"<"     return (int)StatLuaTokens.BINOP;
"<="    return (int)StatLuaTokens.BINOP;
">"     return (int)StatLuaTokens.BINOP;
">="    return (int)StatLuaTokens.BINOP;
"=="    return (int)StatLuaTokens.BINOP;
"~="    return (int)StatLuaTokens.BINOP;
"and"   return (int)StatLuaTokens.BINOP;
"or"    return (int)StatLuaTokens.BINOP;

"not"   return (int)StatLuaTokens.UNOP;
"#"     return (int)StatLuaTokens.UNOP;
"!"     return (int)StatLuaTokens.UNOP;

"~"     return (int)StatLuaTokens.BIN_OR_UNOP;
"-"     return (int)StatLuaTokens.BIN_OR_UNOP;

\"[^']*\" { yylval = yytext; return (int)StatLuaTokens.LITERAL_STRING; }
'[^']*' { yylval = yytext; return (int)StatLuaTokens.LITERAL_STRING; }
{letter}({namechar})+ { yylval = yytext; return (int)StatLuaTokens.NAME; }
{digit}({digit})* { yylval = yytext; return (int)StatLuaTokens.INTEGER; }
{digit}({digit})*\.{digit}({digit})* { yylval = yytext; return (int)StatLuaTokens.FLOAT; }
{digit}{digit}*d{digit}{digit}* { yylval = yytext; return (int)StatLuaTokens.DICE_ROLL; }

. return ((int)StatLuaTokens.BAD);

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
