using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
int a = 123;
int b = 12412;
while a < b
{

}

");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");