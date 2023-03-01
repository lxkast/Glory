using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
int a = 0;
a /= 1;
");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");