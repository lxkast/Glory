using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"

int [] FUnction()
{
    int a = 2;
    int b = a + 2;
}
");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");