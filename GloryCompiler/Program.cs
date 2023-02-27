using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
while a
{
    int a = 1;
    string b = ""cool string"";
}
");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");