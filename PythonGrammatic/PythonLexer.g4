lexer grammar PythonLexer;

options {
	superClass = PythonLexerBase;
}

@header {
namespace Kotlin_plus_plus;
}

tokens {
	INDENT,
	DEDENT
}

// Comments
COMMENT: '#' ~[\r\n]* -> channel(HIDDEN);

// Newlines
NEWLINE: ('\r'? '\n')+;

// Whitespace (spaces and tabs, but not newlines)
WS: [ \t]+ -> channel(HIDDEN);

// Keywords
DEF: 'def';
IF: 'if';
ELIF: 'elif';
ELSE: 'else';
WHILE: 'while';
FOR: 'for';
IN: 'in';
TRY: 'try';
EXCEPT: 'except';
FINALLY: 'finally';
RAISE: 'raise';
RETURN: 'return';
BREAK: 'break';
CONTINUE: 'continue';
PASS: 'pass';
AND: 'and';
OR: 'or';
NOT: 'not';
AS: 'as';

// Literals
TRUE: 'True';
FALSE: 'False';
NONE: 'None';

// Operators & Delimiters
LPAREN: '(';
RPAREN: ')';
LBRACK: '[';
RBRACK: ']';
LCURL: '{';
RCURL: '}';
COLON: ':';
SEMICOLON: ';';
COMMA: ',';
DOT: '.';
ARROW: '->';

ASSIGN: '=';
ADD_ASSIGN: '+=';
SUB_ASSIGN: '-=';
MULT_ASSIGN: '*=';
DIV_ASSIGN: '/=';
MOD_ASSIGN: '%=';

PLUS: '+';
MINUS: '-';
STAR: '*';
SLASH: '/';
DOUBLE_SLASH: '//';
PERCENT: '%';
DOUBLE_STAR: '**';

EQ: '==';
NEQ: '!=';
LT: '<';
GT: '>';
LTE: '<=';
GTE: '>=';

STRING: ('"' (~["\\\r\n] | '\\' .)* '"') | ('\'' (~['\\\r\n] | '\\' .)* '\'');
FLOAT: [0-9]+ '.' [0-9]+;
INT: [0-9]+;
ID: [a-zA-Z_][a-zA-Z0-9_]*;

