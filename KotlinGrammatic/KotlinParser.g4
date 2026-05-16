parser grammar KotlinParser;

options {
	tokenVocab = KotlinLexer;
}

@header {
namespace Kotlin_plus_plus;
}

root: statements EOF;

statements: (
		statement ((SEMICOLON | NL)+ statement)* (SEMICOLON | NL)*
	)?;

statement:
	variableDeclaration
	| ifExpression
	| forStatement
	| block
	| expression (SEMICOLON | NL)*;

block: LCURL NL* statements NL* RCURL;

controlStructureBody: block | statement;

ifExpression:
	IF NL* LPAREN NL* expression NL* RPAREN NL* controlStructureBody (
		NL* ELSE NL* controlStructureBody
	)?;

forStatement:
	FOR NL* LPAREN NL* ID IN expression DOTDOT expression NL* RPAREN NL* controlStructureBody;

variableDeclaration: (VAL | VAR) ID ASSIGNMENT expression;

expression:
	ID
	| INT
	| expression (LANGLE | RANGLE | LE | GE | EQEQ | EXCL_EQ) expression
	| LPAREN expression RPAREN;