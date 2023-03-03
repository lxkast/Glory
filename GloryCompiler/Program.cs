using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"




blank add(int a, int b)
{
    int c = a+ b;
}









");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");