parser grammar PythonParser;

options {
	tokenVocab = PythonLexer;
}

@header {
namespace Kotlin_plus_plus;
}

root: statements EOF;

statements: (NEWLINE | statement)*;

statement:
	compoundStatement
	| simpleStatement;

compoundStatement:
	functionDef
	| ifStatement
	| forStatement
	| whileStatement
	| tryStatement;

simpleStatement:
	smallStatement (SEMICOLON smallStatement)* SEMICOLON? NEWLINE;

smallStatement:
	assignment
	| typeDeclaration
	| jumpStatement
	| PASS
	| expression
	| unparsedStatement;

block:
	NEWLINE INDENT statements DEDENT
	| simpleStatement;

type: ID;

typeDeclaration:
	ID COLON type (ASSIGN expression)?;

assignment:
	ID ASSIGN expression			# NormalAssignment
	| ID ADD_ASSIGN expression		# AddAssignment
	| ID SUB_ASSIGN expression		# SubAssignment
	| ID MULT_ASSIGN expression		# MultAssignment
	| ID DIV_ASSIGN expression		# DivAssignment;

functionDef:
	DEF ID LPAREN parameterList? RPAREN (ARROW type)? COLON block;

parameterList:
	parameter (COMMA parameter)*;

parameter:
	ID (COLON type)? (ASSIGN expression)?;

ifStatement:
	IF expression COLON block
	(ELIF expression COLON block)*
	(ELSE COLON block)?;

forStatement:
	FOR ID IN expression COLON block;

whileStatement:
	WHILE expression COLON block;

tryStatement:
	TRY COLON block (exceptBlock)+ (finallyBlock)?
	| TRY COLON block finallyBlock;

exceptBlock:
	EXCEPT (type (AS ID)?)? COLON block;

finallyBlock:
	FINALLY COLON block;

jumpStatement:
	RETURN expression?
	| RAISE expression?
	| BREAK
	| CONTINUE;

expression:
	LPAREN expression RPAREN												# Parens
	| expression LPAREN (expression (COMMA expression)*)? RPAREN			# FunctionCall
	| expression DOT ID														# MemberAccess
	| MINUS expression														# UnaryMinus
	| NOT expression														# UnaryNot
	| expression (STAR | SLASH | DOUBLE_SLASH | PERCENT) expression			# MulDiv
	| expression (PLUS | MINUS) expression									# AddSub
	| expression (LT | GT | LTE | GTE | EQ | NEQ) expression				# Comparison
	| expression AND expression												# AndLogical
	| expression OR expression												# OrLogical
	| ID																	# Identifier
	| INT																	# IntLiteral
	| FLOAT																	# FloatLiteral
	| STRING																# StringLiteral
	| TRUE																	# TrueLiteral
	| FALSE																	# FalseLiteral
	| NONE																	# NoneLiteral;

unparsedStatement:
	(~(NEWLINE | SEMICOLON | COLON))+;

