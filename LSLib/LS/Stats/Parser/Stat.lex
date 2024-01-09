%namespace LSLib.LS.Stats.StatParser
%visibility public
%scannertype StatScanner
%scanbasetype StatScanBase
%tokentype StatTokens

letter [a-zA-Z]
digit [0-9]
namechar [a-zA-Z_]

%%

data([ ]+)\"([^\"]+)\"([ ]+)\"(.*)\" { yylval = MakeDataProperty(tokLin, tokCol, tokELin, tokECol, yytext); return (int)StatTokens.DATA_ITEM; }

/* Reserved words */
"new"        return (int)StatTokens.NEW;
"add"        return (int)StatTokens.ADD;
"entry"      return (int)StatTokens.ENTRY;
"type"       return (int)StatTokens.TYPE;
"data"       return (int)StatTokens.DATA;
"param"      return (int)StatTokens.PARAM;
"using"      return (int)StatTokens.USING;
"key"        return (int)StatTokens.KEY;

/* Stat entry types */

/* Stat entry types */
"ability"     return (int)StatTokens.ABILITY;

/* ItemColors */
"itemcolor"   return (int)StatTokens.ITEMCOLOR;

/* ItemProgressionNames */
"namegroup"   return (int)StatTokens.NAMEGROUP;
"namecool"    return (int)StatTokens.NAMECOOL;
"name"        return (int)StatTokens.NAME;

/* ItemProgressionVisuals */
"itemgroup"   return (int)StatTokens.ITEMGROUP;
"levelgroup"  return (int)StatTokens.LEVELGROUP;
"rootgroup"   return (int)StatTokens.ROOTGROUP;

/* Requirements */
"requirement"   return (int)StatTokens.REQUIREMENT;

/* DeltaModifiers */ 
"deltamod"   return (int)StatTokens.DELTAMOD;
"new boost"  return (int)StatTokens.NEW_BOOST;

/* Equipment */
"equipment"           return (int)StatTokens.EQUIPMENT;
"add equipmentgroup"  return (int)StatTokens.ADD_EQUIPMENTGROUP;
"add equipment entry" return (int)StatTokens.ADD_EQUIPMENT_ENTRY;

/* ItemComboProperties - TODO  */
/* "ItemComboProperty"   return (int)StatTokens.ITEMCOMBOPROPERTY;
"new ItemComboPropertyEntry"  return (int)StatTokens.NEW_ITEMCOMBOPROPERTYENTRY; */

"ItemCombination"     return (int)StatTokens.ITEM_COMBINATION;
"ItemCombinationResult"   return (int)StatTokens.ITEM_COMBINATION_RESULT;

/* ObjectCategoryItemComboPreviewData */
"CraftingPreviewData" return (int)StatTokens.CRAFTING_PREVIEW_DATA;

/* SkillSet */
"skillset"            return (int)StatTokens.SKILLSET;
"skill"               return (int)StatTokens.SKILL;

/* TreasureGroups */
"CategoryMap"         return (int)StatTokens.CATEGORY_MAP;
"WeaponCounter"       return (int)StatTokens.WEAPON_COUNTER;
"SkillbookCounter"    return (int)StatTokens.SKILLBOOK_COUNTER;
"ArmorCounter"        return (int)StatTokens.ARMOR_COUNTER;

/* TreasureTable */
"treasure"            return (int)StatTokens.TREASURE;
"itemtypes"           return (int)StatTokens.ITEMTYPES;
"treasuretable"       return (int)StatTokens.TREASURE_TABLE;
"new subtable"        return (int)StatTokens.NEW_SUBTABLE;
"object category"     return (int)StatTokens.OBJECT_CATEGORY;
"StartLevel"          return (int)StatTokens.START_LEVEL;
"EndLevel"            return (int)StatTokens.END_LEVEL;
"MinLevel"            return (int)StatTokens.MIN_LEVEL;
"MaxLevel"            return (int)StatTokens.MAX_LEVEL;
"CanMerge"            return (int)StatTokens.CAN_MERGE;
"IgnoreLevelDiff"     return (int)StatTokens.IGNORE_LEVEL_DIFF;
"UseTreasureGroupCounters" return (int)StatTokens.USE_TREASURE_GROUPS;

/* Special characters */
","                return (int)',';
[ \t\v\r\n\f]      ;

/* Comments */
^[/][/][^\n]*\n    ;

({namechar})+       { yylval = MakeLiteral(yytext); return (int)StatTokens.NAME; }
{digit}({digit})*   { yylval = MakeLiteral(yytext); return (int)StatTokens.INTEGER; }
L?\"(\\.|[^\\"])*\" { yylval = MakeString(yytext); return (int)StatTokens.STRING; }

. return ((int)StatTokens.BAD);

%{
    yylloc = new LSLib.LS.Story.GoalParser.CodeLocation(fileName, tokLin, tokCol, tokELin, tokECol);
%}
