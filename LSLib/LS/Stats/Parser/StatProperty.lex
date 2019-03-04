%namespace LSLib.LS.Stats.StatPropertyParser
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
"SELF"       return (int)StatPropertyTokens.CTX_SELF;
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

"Resurrect"    return (int)StatPropertyTokens.ACT_RESURRECT;
"Sabotage"     return (int)StatPropertyTokens.ACT_SABOTAGE;
"Summon"       return (int)StatPropertyTokens.ACT_SUMMON;
"Force"        return (int)StatPropertyTokens.ACT_FORCE;
"CLEANSE"      return (int)StatPropertyTokens.ACT_CLEANSE;
"AOEBOOST"     return (int)StatPropertyTokens.ACT_AOEBOOST;
"SURFACEBOOST" return (int)StatPropertyTokens.ACT_SURFACEBOOST;

(AlwaysBackstab|Unbreakable|CanBackstab|AlwaysHighGround) return (int)StatPropertyTokens.ACT_CUSTOM_PROPERTY;
(Ignite|Melt|Freeze|Electrify|Bless|Curse|Condense|Vaporize|Bloodify|Contaminate|Oilify|Shatter) return (int)StatPropertyTokens.ACT_SURFACE_CHANGE;
(CreateSurface|TargetCreateSurface|CreateConeSurface) return (int)StatPropertyTokens.ACT_CREATE_SURFACE;
"Douse"         return (int)StatPropertyTokens.ACT_GAME_ACTION;
"SwapPlaces"    return (int)StatPropertyTokens.ACT_SWAP_PLACES;
"Equalize"      return (int)StatPropertyTokens.ACT_EQUALIZE;
"Pickup"        return (int)StatPropertyTokens.ACT_PICKUP;

(Fire|Water|Blood|Poison|Oil|Lava|Source|Web|Deepwater|FireCloud|WaterCloud|BloodCloud|PoisonCloud|SmokeCloud|ExplosionCloud|FrostCloud) return (int)StatPropertyTokens.SURFACE_TYPE_OR_STATE;
(FireBlessed|FireCursed|FirePurified|WaterElectrified|WaterFrozen|WaterBlessed|WaterElectrifiedBlessed|WaterFrozenBlessed|WaterCursed|WaterElectrifiedCursed|WaterFrozenCursed|WaterPurified|WaterElectrifiedPurified|WaterFrozenPurified|BloodElectrified|BloodFrozen|BloodBlessed|BloodElectrifiedBlessed|BloodFrozenBlessed|BloodCursed|BloodElectrifiedCursed|BloodFrozenCursed|BloodPurified|BloodElectrifiedPurified|BloodFrozenPurified|PoisonBlessed|PoisonCursed|PoisonPurified|OilBlessed|OilCursed|OilPurified|WebBlessed|WebCursed|WebPurified|FireCloudBlessed|FireCloudCursed|FireCloudPurified|WaterCloudElectrified|WaterCloudBlessed|WaterCloudElectrifiedBlessed|WaterCloudCursed|WaterCloudElectrifiedCursed|WaterCloudPurified|WaterCloudElectrifiedPurified|BloodCloudElectrified|BloodCloudBlessed|BloodCloudElectrifiedBlessed|BloodCloudCursed|BloodCloudElectrifiedCursed|BloodCloudPurified|BloodCloudElectrifiedPurified|PoisonCloudBlessed|PoisonCloudCursed|PoisonCloudPurified|SmokeCloudBlessed|SmokeCloudCursed|SmokeCloudPurified|Deathfog|Sentinel|ShockwaveCloud) return (int)StatPropertyTokens.SURFACE_TYPE;
(Blessed|Cursed|Purified|Electrified|Frozen|BlessedCloud|CursedCloud|PurifiedCloud|ElectrifiedCloud|FrozenCloud|DeathfogCloud) return (int)StatPropertyTokens.SURFACE_STATE;
(SurfaceFire|SurfaceFireBlessed|SurfaceFireCursed|SurfaceFirePurified|SurfaceWater|SurfaceWaterElectrified|SurfaceWaterFrozen|SurfaceWaterBlessed|SurfaceWaterElectrifiedBlessed|SurfaceWaterFrozenBlessed|SurfaceWaterCursed|SurfaceWaterElectrifiedCursed|SurfaceWaterFrozenCursed|SurfaceWaterPurified|SurfaceWaterElectrifiedPurified|SurfaceWaterFrozenPurified|SurfaceBlood|SurfaceBloodElectrified|SurfaceBloodFrozen|SurfaceBloodBlessed|SurfaceBloodElectrifiedBlessed|SurfaceBloodFrozenBlessed|SurfaceBloodCursed|SurfaceBloodElectrifiedCursed|SurfaceBloodFrozenCursed|SurfaceBloodPurified|SurfaceBloodElectrifiedPurified|SurfaceBloodFrozenPurified|SurfacePoison|SurfacePoisonBlessed|SurfacePoisonCursed|SurfacePoisonPurified|SurfaceOil|SurfaceOilBlessed|SurfaceOilCursed|SurfaceOilPurified|SurfaceLava|SurfaceSource|SurfaceWeb|SurfaceWebBlessed|SurfaceWebCursed|SurfaceWebPurified|SurfaceDeepwater|SurfaceFireCloud|SurfaceFireCloudBlessed|SurfaceFireCloudCursed|SurfaceFireCloudPurified|SurfaceWaterCloud|SurfaceWaterCloudElectrified|SurfaceWaterCloudBlessed|SurfaceWaterCloudElectrifiedBlessed|SurfaceWaterCloudCursed|SurfaceWaterCloudElectrifiedCursed|SurfaceWaterCloudPurified|SurfaceWaterCloudElectrifiedPurified|SurfaceBloodCloud|SurfaceBloodCloudElectrified|SurfaceBloodCloudBlessed|SurfaceBloodCloudElectrifiedBlessed|SurfaceBloodCloudCursed|SurfaceBloodCloudElectrifiedCursed|SurfaceBloodCloudPurified|SurfaceBloodCloudElectrifiedPurified|SurfacePoisonCloud|SurfacePoisonCloudBlessed|SurfacePoisonCloudCursed|SurfacePoisonCloudPurified|SurfaceSmokeCloud|SurfaceSmokeCloudBlessed|SurfaceSmokeCloudCursed|SurfaceSmokeCloudPurified|SurfaceExplosionCloud|SurfaceFrostCloud|SurfaceDeathfog|SurfaceSentinel|SurfaceShockwaveCloud) return (int)StatPropertyTokens.SURFACE_TYPE_EX;
(Locked|Ally|Enemy|Character|Item|Dead|Summon|NonSummon|Self|NonSelf|Party|LowHP|CanExplode|CanGiveSP|Grounded|FacingMe|AllowDead|Player|CanPickup|Owner|MySummon|Spirit|DamagedOnHeal|Undead|CanBeSabotaged|PhysicalArmourUp|MagicArmourUp) return (int)StatPropertyTokens.SKILL_CONDITION;
(Tagged|HasStatus) return (int)StatPropertyTokens.SKILL_CONDITION_1ARG;
"InSurface" return (int)StatPropertyTokens.SKILL_CONDITION_IN_SURFACE;
"Surface"   return (int)StatPropertyTokens.SKILL_CONDITION_SURFACE;

"Tag" return (int)StatPropertyTokens.REQUIREMENT_TAG;
(Level|Strength|Finesse|Intelligence|Constitution|Memory|Wits|WarriorLore|RangerLore|RogueLore|SingleHanded|TwoHanded|PainReflection|Ranged|Shield|Reflexes|PhysicalArmorMastery|MagicArmorMastery|Vitality|Sourcery|Telekinesis|FireSpecialist|WaterSpecialist|AirSpecialist|EarthSpecialist|Necromancy|Summoning|Polymorph|Repair|Sneaking|Pickpocket|Thievery|Loremaster|Crafting|Barter|Charm|Intimidate|Reason|Persuasion|Leadership|Luck|DualWielding|Wand|Perseverance|MinKarma|MaxKarma) return (int)StatPropertyTokens.REQUIREMENT_INT_ARG;
(TALENT_ItemMovement|TALENT_ItemCreation|TALENT_Flanking|TALENT_AttackOfOpportunity|TALENT_Backstab|TALENT_Trade|TALENT_Lockpick|TALENT_ChanceToHitRanged|TALENT_ChanceToHitMelee|TALENT_Damage|TALENT_ActionPoints|TALENT_ActionPoints2|TALENT_Criticals|TALENT_IncreasedArmor|TALENT_Sight|TALENT_ResistFear|TALENT_ResistKnockdown|TALENT_ResistStun|TALENT_ResistPoison|TALENT_ResistSilence|TALENT_ResistDead|TALENT_Carry|TALENT_Kinetics|TALENT_Repair|TALENT_ExpGain|TALENT_ExtraStatPoints|TALENT_ExtraSkillPoints|TALENT_Durability|TALENT_Awareness|TALENT_Vitality|TALENT_FireSpells|TALENT_WaterSpells|TALENT_AirSpells|TALENT_EarthSpells|TALENT_Charm|TALENT_Intimidate|TALENT_Reason|TALENT_Luck|TALENT_Initiative|TALENT_InventoryAccess|TALENT_AvoidDetection|TALENT_AnimalEmpathy|TALENT_Escapist|TALENT_StandYourGround|TALENT_SurpriseAttack|TALENT_LightStep|TALENT_ResurrectToFullHealth|TALENT_Scientist|TALENT_Raistlin|TALENT_MrKnowItAll|TALENT_WhatARush|TALENT_FaroutDude|TALENT_Leech|TALENT_ElementalAffinity|TALENT_FiveStarRestaurant|TALENT_Bully|TALENT_ElementalRanger|TALENT_LightningRod|TALENT_Politician|TALENT_WeatherProof|TALENT_LoneWolf|TALENT_Zombie|TALENT_Demon|TALENT_IceKing|TALENT_Courageous|TALENT_GoldenMage|TALENT_WalkItOff|TALENT_FolkDancer|TALENT_SpillNoBlood|TALENT_Stench|TALENT_Kickstarter|TALENT_WarriorLoreNaturalArmor|TALENT_WarriorLoreNaturalHealth|TALENT_WarriorLoreNaturalResistance|TALENT_RangerLoreArrowRecover|TALENT_RangerLoreEvasionBonus|TALENT_RangerLoreRangedAPBonus|TALENT_RogueLoreDaggerAPBonus|TALENT_RogueLoreDaggerBackStab|TALENT_RogueLoreMovementBonus|TALENT_RogueLoreHoldResistance|TALENT_NoAttackOfOpportunity|TALENT_WarriorLoreGrenadeRange|TALENT_RogueLoreGrenadePrecision|TALENT_ExtraWandCharge|TALENT_DualWieldingDodging|TALENT_Human_Civil|TALENT_Human_Inventive|TALENT_Dwarf_Sneaking|TALENT_Dwarf_Sturdy|TALENT_Elf_CorpseEater|TALENT_Elf_Lore|TALENT_Lizard_Persuasion|TALENT_Lizard_Resistance|TALENT_Perfectionist|TALENT_Executioner|TALENT_QuickStep|TALENT_ViolentMagic|TALENT_Memory|TALENT_LivingArmor|TALENT_Torturer|TALENT_Ambidextrous|TALENT_Unstable|TALENT_Sourcerer|TRAIT_Forgiving|TRAIT_Vindictive|TRAIT_Bold|TRAIT_Timid|TRAIT_Altruistic|TRAIT_Egotistical|TRAIT_Independent|TRAIT_Obedient|TRAIT_Pragmatic|TRAIT_Romantic|TRAIT_Spiritual|TRAIT_Materialistic|TRAIT_Righteous|TRAIT_Renegade|TRAIT_Blunt|TRAIT_Considerate|TRAIT_Compassionate|TRAIT_Heartless|Immobile) return (int)StatPropertyTokens.REQUIREMENT_NO_ARG;

"Combat" return (int)StatPropertyTokens.COMBAT;

{letter}({namechar})+ return (int)StatPropertyTokens.NAME;
(-)?{digit}({digit})* return (int)StatPropertyTokens.INTEGER;
({nonseparator})+     return (int)StatPropertyTokens.TEXT;

. return ((int)StatPropertyTokens.BAD);
