```
statement      → variable | assignment | if | while | function ";"

variable       → typename IDENTIFIER ("=" expression)?
assignment     → (IDENTIFIER "=")? expression
if             → "if" expression "{" statement* "}" ( "elif" condition "{" statement* "}" )* ( "else" "{" statement* "}" )?
while          → "while" expression "{" statement* "}" 
function       → typename IDENTIFIER "(" ( (typename identifier ",")* typename identifier )? ")" "{" statement* "}"

typename       → "string" | "int" | "float"

expression     → assignment
compare        → additive (( "==" | ">" | "<" | ">=" | "<=" ) additive)*
additive       → division (("+" | "-") division)*
divide         → term ("/" term)*
multiply       → index ("*" index)*
index          → unary ("^" unary)*
negate         → "-"* unary
call           → IDENTIFIER "(" ( ( expression ",")* expression )? ")"
unary          → STRING_LITERAL | NUM_LITERAL | IDENTIFIER | "(" expression ")"
```
