```
outerstatement → function | statement
statement      → (variable ";" | assignment ";" | if | while | call)

variable       → typename IDENTIFIER ("=" expression)?
assignment     → IDENTIFIER "=" expression
if             → "if" expression "{" statement* "}" ( "elif" expression "{" statement* "}" )* ( "else" "{" statement* "}" )?
while          → "while" expression "{" statement* "}" 
function       → typename IDENTIFIER "(" ( (typename identifier ",")* typename identifier )? ")" "{" statement* "}"

typename       → "string" | "int" | "float"

expression     → assignment
compare        → additive (( "==" | ">" | "<" | ">=" | "<=" ) additive)*
additive       → division (("+" | "-") division)*
divide         → multiply ("/" multiply)*
multiply       → index ("*" index)*
index          → negate ("^" negate)*
negate         → "-"* call
call           → IDENTIFIER "(" ( ( expression ",")* expression )? ")" | unary
unary          → STRING_LITERAL | NUM_LITERAL | IDENTIFIER | "(" expression ")"
```
