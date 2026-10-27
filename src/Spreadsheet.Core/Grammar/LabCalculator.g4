grammar LabCalculator;

// Регістр літер не важливий: MAX(a1, 2) = max(A1, 2)
options { caseInsensitive = true; }

// Точка входу: текст клітинки - один вираз
program : expression EOF ;

// Порядок альтернатив задає пріоритет: порівняння виконується останнім
expression
    : LPAREN expression RPAREN                                      # Parens
    | op=(INC | DEC) LPAREN expression RPAREN                       # IncDec
    | op=(MAX | MIN) LPAREN expression COMMA expression RPAREN      # MaxMin
    | op=(MMAX | MMIN) LPAREN expression (COMMA expression)* RPAREN # MmaxMmin
    | expression op=(EQ | NEQ | LE | GE | LT | GT) expression       # Compare
    | NUMBER                                                        # Number
    | CELL                                                          # CellRef
    | IDENT                                                         # Variable
    ;

// Ключові слова стоять раніше за IDENT, тому "max" - це MAX, а не ім'я
INC  : 'inc' ;
DEC  : 'dec' ;
MMAX : 'mmax' ;
MMIN : 'mmin' ;
MAX  : 'max' ;
MIN  : 'min' ;

NEQ : '<>' ;
LE  : '<=' ;
GE  : '>=' ;
EQ  : '=' ;
LT  : '<' ;
GT  : '>' ;

LPAREN : '(' ;
RPAREN : ')' ;
COMMA  : ',' ;

NUMBER : [0-9]+ ;
CELL   : [A-Z]+ [0-9]+ ;           // A1, AB12
IDENT  : [A-Z_] [A-Z_0-9]* ;       // невідоме ім'я -> #NAME?

WS : [ \t\r\n]+ -> skip ;
