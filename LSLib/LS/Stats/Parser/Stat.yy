%namespace LSLib.LS.Stats.StatParser
%partial 
%visibility public
%parsertype StatParser
%tokentype StatTokens
%YYSTYPE System.Object
%YYLTYPE LSLib.LS.Story.GoalParser.CodeLocation

%start StatFile

/* Reserved word */
%token NEW
%token ADD
%token ENTRY
%token TYPE
%token DATA
%token PARAM
%token USING
%token KEY

/* Abilities */
%token ABILITY

/* ItemColors */
%token ITEMCOLOR

/* ItemProgressionNames */
%token NAMEGROUP
%token NAME
%token NAMECOOL

/* ItemProgressionVisuals */
%token ITEMGROUP
%token LEVELGROUP
%token ROOTGROUP

/* Requirements */
%token REQUIREMENT

/* DeltaModifiers */ 
%token DELTAMOD
%token NEW_BOOST

/* Equipment */ 
%token EQUIPMENT
%token ADD_EQUIPMENTGROUP
%token ADD_EQUIPMENT_ENTRY

/* ItemComboProperties */
%token ITEMCOMBOPROPERTY
%token NEW_ITEMCOMBOPROPERTYENTRY

%token ITEM_COMBINATION
%token ITEM_COMBINATION_RESULT

/* ObjectCategoryItemComboPreviewData */
%token CRAFTING_PREVIEW_DATA

/* SkillSet */
%token SKILLSET
%token SKILL

/* TreasureGroups */
%token CATEGORY_MAP
%token WEAPON_COUNTER
%token SKILLBOOK_COUNTER
%token ARMOR_COUNTER

/* TreasureTable */
%token TREASURE
%token ITEMTYPES
%token TREASURE_TABLE
%token NEW_SUBTABLE
%token OBJECT_CATEGORY
%token START_LEVEL
%token END_LEVEL
%token MIN_LEVEL
%token MAX_LEVEL
%token CAN_MERGE
%token IGNORE_LEVEL_DIFF
%token USE_TREASURE_GROUPS

/* Special token for invalid characters */
%token BAD

/* Unquoted name */
%token NAME
/* Integer literal */
%token INTEGER
/* String literal */
%token STRING
/* Nasty hack for bad quoting in data lines */
%token DATA_ITEM

%%

StatFile : Declarations;

Declarations : /* empty */ { $$ = MakeDeclarationList(); }
             | Declarations Declaration { $$ = AddDeclaration($1, $2); }
             | Declarations TreasureTypesDeclaration { $$ = $1; }
             ;

TreasureTypesDeclaration : TREASURE ITEMTYPES STRING ',' STRING ',' STRING ',' STRING ',' STRING ',' STRING ',' STRING;

Declaration : DataDeclaration;

DataDeclaration : EntryHeader EntryProperties { $$ = AddProperty($2, $1); };

EntryProperties : /* empty */ { $$ = MakeDeclaration(@$); }
                | EntryProperties EntryProperty { $$ = AddProperty($1, $2); }
                ;

EntryHeader : EntryStdHeader
            | DataKeyHeader
            | AbilityHeader
            | RequirementsHeader
            | DeltaModHeader
            | ItemColorHeader
			| ItemCombinationHeader
			| ItemCombinationResultHeader
            | ItemProgressionNamesHeader
            | ItemProgressionVisualsHeader
            | EquipmentHeader
            | ItemComboPropertyHeader
            | ObjectCategoryItemComboPreviewDataHeader
            | SkillSetHeader
            | TreasureGroupHeader
            | TreasureTableHeader;

EntryStdHeader : NEW ENTRY STRING { $$ = MakeProperty(@3, "Name", $3); };

DataKeyHeader : KEY STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "Key", $2),
					MakeProperty("EntityType", "Data"),
					MakeProperty(@4, "Value", $4)
				}); };

/* TODO - integer params are unknown */
AbilityHeader : ABILITY NAME ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "Name", $2),
					MakeProperty("EntityType", "Ability")
				}); };

RequirementsHeader : REQUIREMENT STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "Name", $2),
					MakeProperty("EntityType", "Requirement"),
					MakeProperty(@4, "Requirements", $4)
				}); };

DeltaModHeader : NEW DELTAMOD STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "DeltaModifier")
				}); };

ItemCombinationHeader : NEW ITEM_COMBINATION STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "ItemCombination")
				}); };

ItemCombinationResultHeader : NEW ITEM_COMBINATION_RESULT STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "ItemCombinationResult")
				}); };

ItemColorHeader : NEW ITEMCOLOR STRING ',' STRING ',' STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "ItemColorName", $3),
					MakeProperty("EntityType", "ItemColor"),
					MakeProperty(@5, "Primary Color", $5),
					MakeProperty(@7, "Secondary Color", $7),
					MakeProperty(@9, "Tertiary Color", $9),
				}); };

ItemProgressionNamesHeader : NEW NAMEGROUP STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "ItemProgressionNames")
				}); };

ItemProgressionVisualsHeader : NEW ITEMGROUP STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "ItemProgressionVisuals")
				}); };

EquipmentHeader : NEW EQUIPMENT STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "Equipment")
				}); };

ItemComboPropertyHeader : NEW ITEMCOMBOPROPERTY STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "ItemComboProperties")
				}); };

ObjectCategoryItemComboPreviewDataHeader : NEW CRAFTING_PREVIEW_DATA STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3)
				}); };

SkillSetHeader : NEW SKILLSET STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@3, "Name", $3),
					MakeProperty("EntityType", "SkillSet")
				}); };

TreasureGroupHeader : CATEGORY_MAP STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "Name", $2),
					MakeProperty(@4, "TreasureGroup", $4),
					MakeProperty("EntityType", "TreasureGroups")
				}); };

TreasureTableHeader : NEW TREASURE_TABLE STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty("Name", $3),
					MakeProperty("EntityType", "TreasureTable")
				}); };

EntryProperty : EntryType
              | EntryUsing
              | EntryData
              | EntryDataHack
              | EntryParam
              | ItemProgressionName
              | ItemProgressionNameCool
              | ItemProgressionVisualLevel
              | ItemProgressionVisualName
              | ItemProgressionVisualRoot
              | DeltaModifierBoost
              | EquipmentGroup
              | ItemComboPropertyEntry
              | SkillSetSkill
              | TreasureGroupWeaponCounter
              | TreasureGroupSkillbookCounter
              | TreasureGroupArmorCounter
              | TreasureSubtable
              | TreasureTableMinLevel
              | TreasureTableMaxLevel
              | TreasureTableCanMerge
              | TreasureTableIgnoreLevelDiff
              | TreasureTableUseTreasureGroups
              ;

EntryType : TYPE STRING { $$ = MakeProperty(@$, "EntityType", $2); };

EntryUsing : USING STRING { $$ = MakeProperty(@$, "Using", $2); };

EntryData : DATA STRING STRING { $$ = MakeProperty(@$, $2, $3); };

EntryDataHack : DATA_ITEM;

EntryParam : PARAM STRING STRING { $$ = MakeProperty(@$, $2, $3); };

ItemProgressionName : ADD NAME STRING ',' STRING
                { $$ = MakeElement("Names", new Dictionary<String, object> {
						{"Name", Unwrap($3)},
						{"Description", Unwrap($5)}
				}); };

ItemProgressionNameCool : ADD NAMECOOL STRING ',' STRING
                { $$ = MakeElement("NamesCool", new Dictionary<String, object> {
						{"Name", Unwrap($3)},
						{"Description", Unwrap($5)}
				}); };

ItemProgressionVisualLevel : ADD LEVELGROUP INTEGER ',' INTEGER ',' STRING
                { $$ = MakeElement("LevelGroups", new Dictionary<String, object> {
						{"MinLevel", Unwrap($3)},
						{"MaxLevel", Unwrap($5)},
						{"Rarity", Unwrap($7)},
				}); };

ItemProgressionVisualName : ADD ROOTGROUP STRING ',' STRING
                { $$ = MakeElement("NameGroups", new Dictionary<String, object> {
						{"RootTemplate", Unwrap($3)},
						{"ItemColor", Unwrap($5)},
				}); };

ItemProgressionVisualRoot : ADD NAMEGROUP STRING ',' STRING ',' STRING
                { $$ = MakeElement("RootGroups", new Dictionary<String, object> {
						{"NameGroup", Unwrap($3)},
						{"AffixType", Unwrap($5)},
						{"Icon", Unwrap($7)},
				}); };

DeltaModifierBoost : NEW_BOOST STRING ',' INTEGER
                { $$ = MakeElement("Boosts", new Dictionary<String, object> {
						{"Boost", Unwrap($2)},
						{"Multiplier", Unwrap($4)},
				}); };

EquipmentGroup : ADD_EQUIPMENTGROUP EquipmentEntries { $$ = MakeElement("EquipmentGroups", $2, @2); };

EquipmentEntries : /* empty */ { $$ = MakeCollection(); }
                | EquipmentEntries EquipmentEntry { $$ = AddElement($1, $2); }
                ;

EquipmentEntry : ADD_EQUIPMENT_ENTRY STRING { $$ = $2; };

ItemComboPropertyEntry: NEW_ITEMCOMBOPROPERTYENTRY EntrySubProperties { $$ = MakeElement("Entries", $2, @2); };

EntrySubProperties : /* empty */ { $$ = MakeDeclaration(); }
                | EntrySubProperties EntryData { $$ = AddProperty($1, $2); }
                ;

SkillSetSkill : ADD SKILL STRING { $$ = MakeElement("NameGroups", $3, @3); };

TreasureGroupWeaponCounter : WEAPON_COUNTER STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "WeaponTreasureGroup", $2),
					MakeProperty(@4, "WeaponDefaultCounter", $4)
				}); };

TreasureGroupSkillbookCounter : SKILLBOOK_COUNTER STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "SkillbookTreasureGroup", $2),
					MakeProperty(@4, "SkillbookDefaultCounter", $4)
				}); };

TreasureGroupArmorCounter : ARMOR_COUNTER STRING ',' STRING
                { $$ = MakeDeclaration(@$, new [] {
					MakeProperty(@2, "ArmorTreasureGroup", $2),
					MakeProperty(@4, "ArmorDefaultCounter", $4)
				}); };
				
TreasureSubtable : NEW_SUBTABLE STRING TreasureTableObjects
                { $$ = MakeElement("Subtables", 
					AddProperty(
						MakeDeclaration(@$, new [] { MakeProperty(@2, "DropCount", $2) }),
						$3
					), @$);
				};

TreasureTableMinLevel : MIN_LEVEL STRING { $$ = MakeProperty(@$, "MinLevel", $2); };

TreasureTableMaxLevel : MAX_LEVEL STRING { $$ = MakeProperty(@$, "MaxLevel", $2); };

TreasureTableCanMerge : CAN_MERGE STRING { $$ = MakeProperty(@$, "CanMerge", $2); }
                      | CAN_MERGE INTEGER { $$ = MakeProperty(@$, "CanMerge", $2); };

TreasureTableIgnoreLevelDiff : IGNORE_LEVEL_DIFF INTEGER { $$ = MakeProperty(@$, "IgnoreLevelDiff", $2); };

TreasureTableUseTreasureGroups : USE_TREASURE_GROUPS INTEGER { $$ = MakeProperty(@$, "UseTreasureGroupCounters", $2); };

TreasureTableObjects : /* empty */ { $$ = MakeDeclaration(); }
                | TreasureTableObjects TreasureTableEntry { $$ = AddProperty($1, $2); }
                ;

TreasureTableEntry : TreasureTableObject
                   | TreasureTableObjectStartLevel
                   | TreasureTableObjectEndLevel
				   ;

TreasureTableObjectStartLevel : START_LEVEL STRING { $$ = MakeProperty(@$, "StartLevel", $2); };

TreasureTableObjectEndLevel : END_LEVEL STRING { $$ = MakeProperty(@$, "EndLevel", $2); };

TreasureTableObject : OBJECT_CATEGORY STRING ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER ',' INTEGER
                { $$ = MakeElement("Objects", MakeDeclaration(@$, new [] {
					MakeProperty(@2, "Drop", $2),
					MakeProperty(@4, "Frequency", $4),
					MakeProperty(@6, "Common", $6),
					MakeProperty(@8, "Uncommon", $8),
					MakeProperty(@10, "Rare", $10),
					MakeProperty(@12, "Epic", $12),
					MakeProperty(@14, "Legendary", $14),
					MakeProperty(@16, "Divine", $16),
					MakeProperty(@18, "Unique", $18),
					}), @$);
				};