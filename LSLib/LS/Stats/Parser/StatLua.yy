%namespace LSLib.LS.Stats.Lua
%partial 
%visibility public
%parsertype StatLuaParser
%tokentype StatLuaTokens
%YYSTYPE System.Object

%start Root

/* Special token for invalid characters */
%token BAD

/* Integer literal */
%token INTEGER
/* Floating point literal */
%token FLOAT
/* Text-like (unquoted) literal */
%token NAME
/* eg. 1d10 */
%token DICE_ROLL

/* Lua binary operator */
%token BINOP
/* Lua unary operator */
%token UNOP
/* Lua binary or unary operator */
%token BIN_OR_UNOP
/* nil, true, false */
%token LUA_RESERVED_VAL
/* quoted strings */
%token LITERAL_STRING

%%

Root : LExp;

LName : NAME;

LVar : LName 
     | LPrefixExp '[' LExp ']' 
     | LPrefixExp '.' LName
     ;

LOptionalExpList : /* empty */
                 | LExpList
                 ;

LExpList : LExp 
         | LExpList ',' LExp
         ;

LExp : LExpNoUnOp
     | LUnOp LExp
     ;

LExpNoUnOp : LUA_RESERVED_VAL
     | INTEGER 
     | FLOAT 
     | LITERAL_STRING
     | DICE_ROLL
     | LPrefixExp 
     | LTableConstructor 
     | LExpNoUnOp LBinOp LExp 
     ;

LPrefixExp : LVar 
           | LFunctionCall 
           | '(' LExp ')'
           ;

LFunctionCall : LPrefixExp LArgs 
               | LPrefixExp ':' LName LArgs
               ;

LArgs : '(' LOptionalExpList ')' 
      | LTableConstructor 
      | LITERAL_STRING
      ;

LTableConstructor : '{' LOptionalFieldList '}';

LOptionalFieldList : /* empty */
                   | LFieldList
                   ;

LFieldList : LField 
           | LFieldList LFieldSep LField
           ;

LField : '[' LExp ']' '=' LExp 
       | LName '=' LExp 
       | LExp
       ;

LFieldSep : ',';
// Why was there ';' ?

LBinOp : BINOP
       | BIN_OR_UNOP
       ;

LUnOp : UNOP
      | BIN_OR_UNOP
      ;
