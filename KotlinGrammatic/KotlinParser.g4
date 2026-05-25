parser grammar KotlinParser;

options {
	tokenVocab = KotlinLexer;
}

@header {
namespace Kotlin_plus_plus;
}

root: statements EOF;

statements:
	(SEMICOLON | NL)* (statement ((SEMICOLON | NL)+ statement)*)? (
		SEMICOLON
		| NL
	)*;

statement:
	variableDeclaration
	| assignment
	| ifExpression
	| forStatement
	| block
	| whileStatement
	| doWhileStatement
	| tryExpression
	| jumpExpression
	| expression (SEMICOLON | NL)*
	| functionDeclaration
	| unparsedStatement;

expression:
	LPAREN expression RPAREN												# Parens
	| expression LPAREN (expression (COMMA expression)*)? RPAREN			# FunctionCall
	| SUB expression														# UnaryMinus
	| expression (MULT | DIV | MOD) expression								# MulDiv
	| expression (ADD | SUB) expression										# AddSub
	| expression (LANGLE | RANGLE | LE | GE | EQEQ | EXCL_EQ) expression	# Comparison
	| ID																	# Identifier
	| INT																	# IntLiteral
	| anyUnknownBlock														# UnknownBlock
	| ID expression unknownTail												# UnknownPrefixExpr
	| ID anyUnknownBlock													# UnknownBlockKeyword
	| fallbackToken+														# DynamicFallbackExpr;


fallbackToken: 
    ~(SEMICOLON | NL | LCURL | RCURL | VAL | VAR | FUN);

unknownTail: anyUnknownBlock? ;

anyUnknownBlock: LCURL unknownBlockContent* RCURL;

unknownBlockContent:
    anyUnknownBlock 
    | ~(LCURL | RCURL);

unparsedStatement:
	(~(NL | SEMICOLON | LCURL | RCURL))+ (SEMICOLON | NL)*;

block: LCURL NL* statements NL* RCURL;

controlStructureBody: block | statement;

ifExpression:
	IF NL* LPAREN NL* expression NL* RPAREN NL* controlStructureBody (
		NL* ELSE NL* controlStructureBody
	)?;

forStatement:
	FOR NL* LPAREN NL* ID IN expression DOTDOT expression NL* RPAREN NL* controlStructureBody;

variableDeclaration: (VAL | VAR) ID ASSIGNMENT expression;

whileStatement:
	WHILE NL* LPAREN NL* expression RPAREN NL* (
		controlStructureBody
		| SEMICOLON
	);

doWhileStatement:
	DO NL* controlStructureBody? NL* WHILE NL* LPAREN NL* expression RPAREN;

jumpExpression:
	THROW NL* expression
	| RETURN NL* expression?
	| CONTINUE
	| BREAK;

tryExpression:
	TRY NL* block (
		(NL* catchBlock)+ (NL* finallyBlock)?
		| NL* finallyBlock
	);

catchBlock: CATCH NL* LPAREN ID COLON ID RPAREN NL* block;

finallyBlock: FINALLY NL* block;

functionDeclaration:
	FUN NL* ID NL* LPAREN NL* (
		parameter (NL* COMMA NL* parameter)*
	)? NL* RPAREN (NL* COLON NL* ID)? NL* functionBody;

parameter: ID NL* COLON NL* ID;

functionBody: block | ASSIGNMENT NL* expression;

assignment: ID ASSIGNMENT expression;