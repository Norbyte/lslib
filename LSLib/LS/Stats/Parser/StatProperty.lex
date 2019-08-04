%namespace LSLib.LS.Stats.Properties
%visibility public
%scannertype StatPropertyScanner
%scanbasetype StatPropertyScanBase
%tokentype StatPropertyTokens

letter [a-zA-Z_]
digit [0-9]
namechar [a-zA-Z0-9_]
nonseparator [^,;:()|!& ]

%%

/* Special trigger words to determine expression type */
"__TYPE_Properties__"   return (int)StatPropertyTokens.EXPR_PROPERTIES;
"__TYPE_Conditions__"   return (int)StatPropertyTokens.EXPR_CONDITIONS;
"__TYPE_Requirements__" return (int)StatPropertyTokens.EXPR_REQUIREMENTS;

/* Property Contexts */
[Ss][Ee][Ll][Ff] return (int)StatPropertyTokens.CTX_SELF;
"TARGET"     return (int)StatPropertyTokens.CTX_TARGET;
"AOE"        return (int)StatPropertyTokens.CTX_AOE;
"OnHit"      return (int)StatPropertyTokens.CTX_ON_HIT;
"OnEquip"    return (int)StatPropertyTokens.CTX_ON_EQUIP;

/* Reserved words */
"IF"         return (int)StatPropertyTokens.IF;

/* Special characters */
":"          return (int)':';
"("          return (int)'(';
")"          return (int)')';
","          return (int)',';
";"          return (int)';';
"|"          return (int)'|';
"&"          return (int)'&';
"!"          return (int)'!';
[ ]          ;

"Resurrect"    { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_RESURRECT; }
"Sabotage"     { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_SABOTAGE; }
"Summon"       { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_SUMMON; }
"Force"        { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_FORCE; }
"CLEANSE"      { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_CLEANSE; }
"AOEBOOST"     { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_AOEBOOST; }
"SURFACEBOOST" { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_SURFACEBOOST; }

(AlwaysBackstab|Unbreakable|CanBackstab|AlwaysHighGround) { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_CUSTOM_PROPERTY; }
(Ignite|Melt|Freeze|Electrify|Bless|Curse|Condense|Vaporize|Bloodify|Contaminate|Oilify|Shatter) { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_SURFACE_CHANGE; }
(CreateSurface|TargetCreateSurface|CreateConeSurface) { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_CREATE_SURFACE; }
"Douse"         { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_GAME_ACTION; }
"SwapPlaces"    { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_SWAP_PLACES; }
"Equalize"      { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_EQUALIZE; }
"Pickup"        { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.ACT_PICKUP; }

"Tag" { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.REQUIREMENT_TAG; }

{letter}({namechar})+ { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.NAME; }
(-)?{digit}({digit})* { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.INTEGER; }
({nonseparator})+     { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.TEXT; }

. return ((int)StatPropertyTokens.BAD);

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
