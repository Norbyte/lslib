%namespace LSLib.LS.Stats.Properties
%visibility public
%scannertype StatPropertyScanner
%scanbasetype StatPropertyScanBase
%tokentype StatPropertyTokens

letter [a-zA-Z_]
digit [0-9]
namechar [a-zA-Z0-9_]
nonseparator [^,;:()\[\]! ]

%%

/* Special trigger words to determine expression type */
"__TYPE_Properties__" return (int)StatPropertyTokens.EXPR_PROPERTIES;
"__TYPE_DescriptionParams__" return (int)StatPropertyTokens.EXPR_DESCRIPTION_PARAMS;

/* Reserved words */
"IF"         return (int)StatPropertyTokens.IF;

/* Text keys */
"CastOffhand" return (int)StatPropertyTokens.TEXT_KEY;
"Cast2" return (int)StatPropertyTokens.TEXT_KEY;
"Cast3" return (int)StatPropertyTokens.TEXT_KEY;

/* Stats contexts */
"ABILITY_CHECK"     return (int)StatPropertyTokens.CONTEXT;
"ACTION_RESOURCES_CHANGED"     return (int)StatPropertyTokens.CONTEXT;
"AI_IGNORE"     return (int)StatPropertyTokens.CONTEXT;
"AI_ONLY"     return (int)StatPropertyTokens.CONTEXT;
"AOE"     return (int)StatPropertyTokens.CONTEXT;
"ATTACK"     return (int)StatPropertyTokens.CONTEXT;
"ATTACKED"     return (int)StatPropertyTokens.CONTEXT;
"ATTACKED_IN_MELEE_RANGE"     return (int)StatPropertyTokens.CONTEXT;
"ATTACKING_IN_MELEE_RANGE"     return (int)StatPropertyTokens.CONTEXT;
"CAST"     return (int)StatPropertyTokens.CONTEXT;
"CAST_RESOLVED"     return (int)StatPropertyTokens.CONTEXT;
"COMBAT_ENDED"     return (int)StatPropertyTokens.CONTEXT;
"CREATE_2"     return (int)StatPropertyTokens.CONTEXT;
"DAMAGE"     return (int)StatPropertyTokens.CONTEXT;
"DAMAGED"     return (int)StatPropertyTokens.CONTEXT;
"DAMAGE_PREVENTED"     return (int)StatPropertyTokens.CONTEXT;
"DAMAGED_PREVENTED"     return (int)StatPropertyTokens.CONTEXT;
"ENTER_ATTACK_RANGE"     return (int)StatPropertyTokens.CONTEXT;
"EQUIP"     return (int)StatPropertyTokens.CONTEXT;
"LOCKPICKING_SUCCEEDED"     return (int)StatPropertyTokens.CONTEXT;
"GROUND"     return (int)StatPropertyTokens.CONTEXT;
"HEAL"     return (int)StatPropertyTokens.CONTEXT;
"HEALED"     return (int)StatPropertyTokens.CONTEXT;
"INTERRUPT_USED"     return (int)StatPropertyTokens.CONTEXT;
"INVENTORY_CHANGED"     return (int)StatPropertyTokens.CONTEXT;
"LEAVE_ATTACK_RANGE"     return (int)StatPropertyTokens.CONTEXT;
"LONG_REST"     return (int)StatPropertyTokens.CONTEXT;
"MOVED_DISTANCE"     return (int)StatPropertyTokens.CONTEXT;
"OBSCURITY_CHANGED"     return (int)StatPropertyTokens.CONTEXT;
"PROFICIENCY_CHANGED"     return (int)StatPropertyTokens.CONTEXT;
"PROJECTILE"     return (int)StatPropertyTokens.CONTEXT;
"PUSH"     return (int)StatPropertyTokens.CONTEXT;
"PUSHED"     return (int)StatPropertyTokens.CONTEXT;
"SELF"     return (int)StatPropertyTokens.CONTEXT;
"SHORT_REST"     return (int)StatPropertyTokens.CONTEXT;
"STATUS_APPLIED"     return (int)StatPropertyTokens.CONTEXT;
"STATUS_APPLY"     return (int)StatPropertyTokens.CONTEXT;
"STATUS_REMOVE"     return (int)StatPropertyTokens.CONTEXT;
"STATUS_REMOVED"     return (int)StatPropertyTokens.CONTEXT;
"SURFACE_ENTER"     return (int)StatPropertyTokens.CONTEXT;
"TARGET"     return (int)StatPropertyTokens.CONTEXT;
"TURN"     return (int)StatPropertyTokens.CONTEXT;

/* Special characters */
":"          return (int)':';
"("          return (int)'(';
")"          return (int)')';
"["          return (int)'[';
"]"          return (int)']';
","          return (int)',';
";"          return (int)';';
"!"          return (int)'!';
"-"          return (int)'-';
"."          return (int)'.';
[ ]          ;

{letter}({namechar})+ { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.NAME; }
(-)?{digit}({digit})* { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.INTEGER; }
{digit}{digit}*d{digit}{digit}* { yylval = yytext; return (int)StatPropertyTokens.DICE_ROLL; }
({nonseparator})+     { yylval = MakeLiteral(yytext); return (int)StatPropertyTokens.TEXT; }

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
