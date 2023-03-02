using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"




int add(int a, int b)
{
    if a > 0
    {
        return a;
    }
    else
    {
        a = 0;
    }
}









");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");