lexer grammar KotlinLexer;

@header {
namespace Kotlin_plus_plus;
}

DelimitedComment:
	'/*' (DelimitedComment | .)*? '*/' -> channel(HIDDEN);
LineComment: '//' ~[\r\n]* -> channel(HIDDEN);
WS: [\u0020\u0009\u000C] -> channel(HIDDEN);
NL: '\n' | '\r' '\n'?;

fragment Hidden: DelimitedComment | LineComment | WS;

IF: 'if';
ELSE: 'else';
FOR: 'for';
AND: 'and';
OR: 'or';
IN: 'in';
VAL: 'val';
VAR: 'var';
WHILE: 'while';
THROW: 'throw';
RETURN: 'return';
CONTINUE: 'continue';
BREAK: 'break';
FUN: 'fun';
TRY: 'try';
CATCH: 'catch';
FINALLY: 'finally';
DO: 'do';

LPAREN: '(';
RPAREN: ')';
LCURL: '{';
RCURL: '}';
MULT: '*';
MOD: '%';
DIV: '/';
ADD: '+';
SUB: '-';
INCR: '++';
DECR: '--';
CONJ: '&&';
DISJ: '||';
EXCL_NO_WS: '!';
COLON: ':';
SEMICOLON: ';';
ASSIGNMENT: '=';
ADD_ASSIGNMENT: '+=';
SUB_ASSIGNMENT: '-=';
MULT_ASSIGNMENT: '*=';
DIV_ASSIGNMENT: '/=';
HASH: '#';
DOT: '.';
COMMA: ',';
DOTDOT: '..';

LANGLE: '<';
RANGLE: '>';
LE: '<=';
GE: '>=';
EXCL_EQ: '!=';
EQEQ: '==';

STRING_LITERAL
    : '"' ( ~["\\] | '\\' . )* '"'
    ;
ID: [a-zA-Z_][a-zA-Z0-9_]*;
INT: [0-9]+;
DOUBLE: [0-9]+ '.' [0-9]+;
