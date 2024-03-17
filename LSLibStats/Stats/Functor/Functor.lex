%namespace LSLib.Stats.Functors
%visibility public
%scannertype FunctorScanner
%scanbasetype FunctorScanBase
%tokentype FunctorTokens

letter [a-zA-Z_]
digit [0-9]
namechar [a-zA-Z0-9_]
nonseparator [^,;:()\[\]! ]

%%

/* Special trigger words to determine expression type */
"__TYPE_Functors__" return (int)FunctorTokens.EXPR_FUNCTORS;
"__TYPE_DescriptionParams__" return (int)FunctorTokens.EXPR_DESCRIPTION_PARAMS;

/* Reserved words */
"IF"         return (int)FunctorTokens.IF;

/* Text keys */
"CastOffhand" return (int)FunctorTokens.TEXT_KEY;
"Cast2" return (int)FunctorTokens.TEXT_KEY;
"Cast3" return (int)FunctorTokens.TEXT_KEY;

/* Stats contexts */
"ABILITY_CHECK"     return (int)FunctorTokens.CONTEXT;
"ACTION_RESOURCES_CHANGED"     return (int)FunctorTokens.CONTEXT;
"AI_IGNORE"     return (int)FunctorTokens.CONTEXT;
"AI_ONLY"     return (int)FunctorTokens.CONTEXT;
"AOE"     return (int)FunctorTokens.CONTEXT;
"ATTACK"     return (int)FunctorTokens.CONTEXT;
"ATTACKED"     return (int)FunctorTokens.CONTEXT;
"ATTACKED_IN_MELEE_RANGE"     return (int)FunctorTokens.CONTEXT;
"ATTACKING_IN_MELEE_RANGE"     return (int)FunctorTokens.CONTEXT;
"CAST"     return (int)FunctorTokens.CONTEXT;
"CAST_RESOLVED"     return (int)FunctorTokens.CONTEXT;
"COMBAT_ENDED"     return (int)FunctorTokens.CONTEXT;
"CREATE_2"     return (int)FunctorTokens.CONTEXT;
"DAMAGE"     return (int)FunctorTokens.CONTEXT;
"DAMAGED"     return (int)FunctorTokens.CONTEXT;
"DAMAGE_PREVENTED"     return (int)FunctorTokens.CONTEXT;
"DAMAGED_PREVENTED"     return (int)FunctorTokens.CONTEXT;
"ENTER_ATTACK_RANGE"     return (int)FunctorTokens.CONTEXT;
"EQUIP"     return (int)FunctorTokens.CONTEXT;
"LOCKPICKING_SUCCEEDED"     return (int)FunctorTokens.CONTEXT;
"GROUND"     return (int)FunctorTokens.CONTEXT;
"HEAL"     return (int)FunctorTokens.CONTEXT;
"HEALED"     return (int)FunctorTokens.CONTEXT;
"INTERRUPT_USED"     return (int)FunctorTokens.CONTEXT;
"INVENTORY_CHANGED"     return (int)FunctorTokens.CONTEXT;
"LEAVE_ATTACK_RANGE"     return (int)FunctorTokens.CONTEXT;
"LONG_REST"     return (int)FunctorTokens.CONTEXT;
"MOVED_DISTANCE"     return (int)FunctorTokens.CONTEXT;
"OBSCURITY_CHANGED"     return (int)FunctorTokens.CONTEXT;
"PROFICIENCY_CHANGED"     return (int)FunctorTokens.CONTEXT;
"PROJECTILE"     return (int)FunctorTokens.CONTEXT;
"PUSH"     return (int)FunctorTokens.CONTEXT;
"PUSHED"     return (int)FunctorTokens.CONTEXT;
"SELF"     return (int)FunctorTokens.CONTEXT;
"SHORT_REST"     return (int)FunctorTokens.CONTEXT;
"STATUS_APPLIED"     return (int)FunctorTokens.CONTEXT;
"STATUS_APPLY"     return (int)FunctorTokens.CONTEXT;
"STATUS_REMOVE"     return (int)FunctorTokens.CONTEXT;
"STATUS_REMOVED"     return (int)FunctorTokens.CONTEXT;
"SURFACE_ENTER"     return (int)FunctorTokens.CONTEXT;
"TARGET"     return (int)FunctorTokens.CONTEXT;
"TURN"     return (int)FunctorTokens.CONTEXT;

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

{letter}({namechar})+ { yylval = MakeLiteral(yytext); return (int)FunctorTokens.NAME; }
(-)?{digit}({digit})* { yylval = MakeLiteral(yytext); return (int)FunctorTokens.INTEGER; }
{digit}{digit}*d{digit}{digit}* { yylval = yytext; return (int)FunctorTokens.DICE_ROLL; }
({nonseparator})+     { yylval = MakeLiteral(yytext); return (int)FunctorTokens.TEXT; }

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
