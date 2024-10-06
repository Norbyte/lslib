%namespace LSLib.Stats.Requirements
%partial 
%visibility public
%parsertype RequirementParser
%tokentype RequirementTokens
%YYSTYPE System.Object

%start Root

/* Status/Tag name */
%token NAME
/* Integer literal */
%token INTEGER
/* Text-like (unquoted) literal */
%token TEXT

%%

/******************************************************************
 *
 *                      REQUIREMENTS PARSING
 *
 ******************************************************************/

Root : Requirements;

Requirements : /* empty */ { $$ = MakeRequirements(); }
             | UnaryRequirement { $$ = AddRequirement(MakeRequirements(), $1); }
             | Requirements ';'
             | Requirements ';' UnaryRequirement { $$ = AddRequirement($1, $3); }
             ;

UnaryRequirement : Requirement
                 | '!' Requirement { $$ = MakeNotRequirement($2); }
                 ;

Requirement : NAME { $$ = MakeRequirement($1); }
            | NAME INTEGER { $$ = MakeIntRequirement($1, $2); }
            ;
