using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
int a = 123;
int b = 123;
if a == b
{
    int c = 35;
}
elif b == b
{

}
else
{

}

");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");