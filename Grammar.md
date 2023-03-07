```
outerstatement → function | statement
statement      → (variable ";" | assignment ";" | if | while | call | return ";")

variable       → typename IDENTIFIER ("=" expression)?
assignment     → IDENTIFIER "=" expression
if             → "if" expression "{" statement* "}" ( "elif" expression "{" statement* "}" )* ( "else" "{" statement* "}" )?
while          → "while" expression "{" statement* "}" 
function       → (typename | "blank") IDENTIFIER "(" ( (typename identifier ",")* typename identifier )? ")" "{" statement* "}"
return         → "return" expression

typename       → "string" | "int" | "float" | typename ("[" (NUM_LITERAL)? "]")+

expression     → compare
compare        → additive (( "==" | ">" | "<" | ">=" | "<=" ) additive)*
additive       → division (("+" | "-") division)*
divide         → multiply ("/" multiply)*
multiply       → index ("*" index)*
index          → negate ("^" negate)*
negate         → "-"* call
call           → IDENTIFIER "(" ( ( expression ",")* expression )? ")" | array
array          → unary ("[" expression "]")*
unary          → STRING_LITERAL | NUM_LITERAL | IDENTIFIER | "(" expression ")"
```
