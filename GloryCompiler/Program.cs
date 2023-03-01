using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
bool boolean = true;
int i = 0;
int a = 0;

while boolean
{
    if i < 0
    {
        a = a + i;
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