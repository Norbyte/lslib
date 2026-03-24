%namespace LSLib.Stats.Expression
%visibility public
%scannertype ExpressionScanner
%scanbasetype ExpressionScanBase
%tokentype ExpressionTokens

letter [a-zA-Z_]
digit [0-9]
namechar [a-zA-Z0-9_]
nonseparator [^,;:()\[\]{}!+*/^&%~|><=.# ]

%%

/* Special characters */
"("          return (int)'(';
")"          return (int)')';
","          return (int)',';
"."          return (int)'.';
[ \t]        ;


"+"     return (int)ExpressionTokens.BINOP;
"/"     return (int)ExpressionTokens.BINOP;
"*"     return (int)ExpressionTokens.BINOP;

"-"     return (int)ExpressionTokens.BIN_OR_UNOP;

"Placeholder"{digit} return (int)ExpressionTokens.VARIABLE_REF;
"LevelMapValue" return (int)ExpressionTokens.LEVEL_MAP_VALUE;
"ClassLevel" return (int)ExpressionTokens.CLASS_LEVEL;
"ResourceRoll" return (int)ExpressionTokens.RESOURCE_ROLL;
"foreach" return (int)ExpressionTokens.FOR_EACH;
"max" return (int)ExpressionTokens.MAX_EXPR;

"Target" return (int)ExpressionTokens.CONTEXT_TYPE;
"Owner" return (int)ExpressionTokens.CONTEXT_TYPE;
"Cause" return (int)ExpressionTokens.CONTEXT_TYPE;

"Amount" return (int)ExpressionTokens.STATUS_PROPERTY;
"Duration" return (int)ExpressionTokens.STATUS_PROPERTY;

// Attributes & VariableData with modifiers
(Strength|Dexterity|Constitution|Intelligence|Wisdom|Charisma|SpellCastingAbility|UnarmedMeleeAbility)(Modifier|Flat|a{0})(SavingThrow|a{0})(DialogueCheck|a{0})(Advantage|Distadvantage|a{0}) return (int)ExpressionTokens.VARIABLE_REF;
// Skills with modifiers
(Deception|Intimidation|Performance|Persuasion|Acrobatics|SleightOfHand|Stealth|Arcana|History|Investigation|Nature|Religion|Athletics|AnimalHandling|Insight|Medicine|Perception|Survival)(DialogueCheck|a{0})(Advantage|Distadvantage|a{0}) return (int)ExpressionTokens.VARIABLE_REF;
// VariableData
(ProficiencyBonus|Level|SpellDC|WeaponActionDC|CurrentHP|MaxHP|SpellPowerLevel|TadpolePowersCount|DamageDone) return (int)ExpressionTokens.VARIABLE_REF;

{letter}({namechar})+ { yylval = yytext; return (int)ExpressionTokens.NAME; }
{digit}({digit})* { yylval = yytext; return (int)ExpressionTokens.INTEGER; }
// Technically not valid, compatibility with vanilla parser garbo
{digit}({digit})*\.{digit}({digit})* { yylval = yytext; return (int)ExpressionTokens.INTEGER; }
{digit}{digit}*d{digit}{digit}* { yylval = yytext; return (int)ExpressionTokens.DICE_ROLL; }

. return ((int)ExpressionTokens.BAD);

%{
    yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
%}
