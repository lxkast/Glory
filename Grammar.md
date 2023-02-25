```
variable -> typename IDENTIFIER ("=" expression)?
typename -> "string" | "int" | "float"
assignment -> IDENTIFIER "=" expression
if -> "if" condition "{" statement* "}" ( "elif" condition "{" statement* "}" )* ( "else" "{" statement* "}" )?
while -> "while" condition "{" statement* "}" 
function -> typename identifier "(" ( (typename identifier ",")* typename identifier )? ")" "{" statement* "}"
```
