using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
int outside;
int main(int a, string b)
{
    if a > 0
    {
        a = 12;
    }
    else
    {
        b = ""fail"";
    }
}

blank NoReturn(int twelve)
{
    twelve = 12;
}
");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");