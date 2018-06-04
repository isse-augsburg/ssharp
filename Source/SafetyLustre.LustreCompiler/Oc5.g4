grammar Oc5;

// Parser
ocfile: VERSION module;

module:
	MODULE IDENTIFIER constants? signals? variables? actions? automaton ENDMODULE;

constants: CONSTANTS NUMBER (constant)* ENDTABLE;
constant: LIST_INDEX IDENTIFIER index;

signals: SIGNALS NUMBER (signal)* ENDTABLE;
signal: LIST_INDEX nature channel bool?;
nature: input | output;
input: INPUT IDENTIFIER presAction;
presAction: index | HYPHEN;
output: OUTPUT IDENTIFIER outAction;
outAction: index | HYPHEN;
channel: pure | single | multiple;
pure: PURE;
single: SINGLE index;
multiple: MULTIPLE index index;
bool: BOOL index;

variables: VARIABLES NUMBER (variable)* ENDTABLE;
variable: LIST_INDEX index;

actions: ACTIONS NUMBER (action)* ENDTABLE;
action: LIST_INDEX (testAction | linearAction);
testAction: presentAction | ifAction | dszAction;
presentAction: PRESENT index;
ifAction: IF expression;
dszAction: DSZ index;
linearAction: callAction | outputAction;
callAction:
	CALL index OPEN_PARENTHESIS index CLOSE_PARENTHESIS OPEN_PARENTHESIS expression
		CLOSE_PARENTHESIS;
outputAction: OUTPUT index;

automaton: states startpoint calls (state)* ENDTABLE;
states: STATES NUMBER;
startpoint: STARTPOINT index;
calls: CALLS NUMBER;
state: LIST_INDEX actionTree;
actionTree: closedDag | openTestList closedDag;
closedDag: newState | closedTest /*| newClosedDag*/;
newState:
	linearActionList LESS_THAN_SIGN index GREATER_THAN_SIGN
	/* pragmaList */;
closedTest:
	linearActionList index OPEN_PARENTHESIS actionTree CLOSE_PARENTHESIS
		OPEN_PARENTHESIS actionTree CLOSE_PARENTHESIS;
//newClosedDag: linearActionList '[' NUMBER ']' pragmaList;
openTestList: (openTest)+;
openTest:
	/* linearActionList '{' NUMBER '}' pragmaList |*/
	linearActionList index OPEN_PARENTHESIS openDag CLOSE_PARENTHESIS
		OPEN_PARENTHESIS actionTree CLOSE_PARENTHESIS
	| linearActionList index OPEN_PARENTHESIS actionTree CLOSE_PARENTHESIS
		OPEN_PARENTHESIS openDag CLOSE_PARENTHESIS
	| linearActionList index OPEN_PARENTHESIS openDag CLOSE_PARENTHESIS
		OPEN_PARENTHESIS openDag CLOSE_PARENTHESIS;
openDag: openTestList? linearActionList;
linearActionList: (NUMBER)*;
// pragmaList: (pragmaList pragma)?;

expression:
	atomExpression
	| constantExpression
	| variableExpression
	| functionCallExpression;
atomExpression: HASHTAG atomValue;
atomValue: NUMBER | DECIMAL | STRING;
constantExpression: AT_SIGN index;
variableExpression: index;
functionCallExpression:
	index OPEN_PARENTHESIS expressionList CLOSE_PARENTHESIS;
expressionList: expression (COMMA expression)*;

index:
	NUMBER
	| DOLLAR_SIGN NUMBER;

// Lexer
VERSION: 'oc5:';
MODULE: 'module:';
ENDMODULE: 'endmodule:';
ENDTABLE: 'end:';
CONSTANTS: 'constants:';
SIGNALS: 'signals:';
INPUT: 'input:';
OUTPUT: 'output:';
PURE: 'pure:';
SINGLE: 'single:';
MULTIPLE: 'multiple:';
BOOL: 'bool:';
VARIABLES: 'variables:';
ACTIONS: 'actions:';
PRESENT: 'present:';
IF: 'if:';
DSZ: 'dsz';
CALL: 'call:';
STATES: 'states:';
STARTPOINT: 'startpoint:';
CALLS: 'calls:';

IDENTIFIER: LETTER (LETTER | UNDERSCORE | NUMBER)*;
LIST_INDEX: NUMBER COLON;
POINT: '.';
UNDERSCORE: '_';
COLON: ':';
HYPHEN: '-';
OPEN_PARENTHESIS: '(';
CLOSE_PARENTHESIS: ')';
LESS_THAN_SIGN: '<';
GREATER_THAN_SIGN: '>';
DOLLAR_SIGN: '$';
HASHTAG: '#';
AT_SIGN: '@';
COMMA: ',';

NUMBER: [0-9]+;
DECIMAL: NUMBER* POINT NUMBER+;
STRING: (LETTER | NUMBER)+; //TODO

WS: [ \t\r\n] -> skip;

// Lexer fragments
fragment LOWERCASE: [a-z];
fragment UPPERCASE: [A-Z];
fragment LETTER: (UPPERCASE | LOWERCASE);
fragment DIGIT: [0-9];