%namespace LSLib.LS.Osiris
%using QUT.Gppg;
%visibility public
%scannertype GoalScanner
%scanbasetype GoalScanBase
%tokentype GoalTokens

letter [a-zA-Z]
digit [0-9]
hex [0-9a-fA-F]

%x C_COMMENT

%%

/* Reserved words */
"Version"          return (int)GoalTokens.VERSION;
"SubGoalCombiner"  return (int)GoalTokens.SUBGOALCOMBINER;
"SGC_AND"          return (int)GoalTokens.SGC_AND;
"INITSECTION"      return (int)GoalTokens.INITSECTION;
"KBSECTION"        return (int)GoalTokens.KBSECTION;
"EXITSECTION"      return (int)GoalTokens.EXITSECTION;
"ENDEXITSECTION"   return (int)GoalTokens.ENDEXITSECTION;
"IF"               return (int)GoalTokens.IF;
"PROC"             return (int)GoalTokens.PROC;
"QRY"              return (int)GoalTokens.QRY;
"THEN"             return (int)GoalTokens.THEN;
"AND"              return (int)GoalTokens.AND;
"NOT"              return (int)GoalTokens.NOT;

/* Goal completion call -- "GoalCompleted;" */
"GoalCompleted"    return (int)GoalTokens.GOAL_COMPLETED;
"ParentTargetEdge" return (int)GoalTokens.PARENT_TARGET_EDGE;

/* Operators */
"=="               return (int)GoalTokens.EQ_OP;
"!="               return (int)GoalTokens.NE_OP;
"<"                return (int)GoalTokens.LT_OP;
"<="               return (int)GoalTokens.LTE_OP;
">"                return (int)GoalTokens.GT_OP;
">="               return (int)GoalTokens.GTE_OP;

/* Special characters */
"("                return (int)'(';
")"                return (int)')';
";"                return (int)';';
","                return (int)',';
"."                return (int)'.';
[ \t\v\r\n\f]      ;

{letter}({letter}|{digit}|_)*   return (int)GoalTokens.IDENTIFIER;
/* Special case for identifiers with a GUID string at the end */
{letter}({letter}|{digit}|_)*({hex}{8})-({hex}{4})-({hex}{4})-({hex}{4})-({hex}{12}) return (int)GoalTokens.IDENTIFIER;
/* Variables with a leading underscore are local, and are handled differently */
_({letter}|{digit}|_)*          return (int)GoalTokens.LOCAL_VAR;
[+\-]?{digit}({digit})*         return (int)GoalTokens.INTEGER;
[+\-]?{digit}+\.{digit}+        return (int)GoalTokens.FLOAT;
L?\"(\\.|[^\\"])*\"             return (int)GoalTokens.STRING;

/* Comments */
[/][/][^\n]*\n                  ;

"/*"                            { BEGIN(C_COMMENT); }
<C_COMMENT>"*/"                 { BEGIN(INITIAL); }
<C_COMMENT>\n                   { }
<C_COMMENT>.                    { }

. return ((int)GoalTokens.BAD);

%{
	yylloc = new LexLocation(tokLin, tokCol, tokELin, tokECol/*, tokPos, tokEPos, buffer*/);
%}
