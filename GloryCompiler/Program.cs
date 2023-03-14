using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"

int add(int a, int b)
{
    int c = a+b;
    return c;
}

string name(int[15] a)
{
    return ""lol"";
}

");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

using (StreamWriter sw = new StreamWriter("program.asm"))
{
    CodeOutput CodeOutput = new ASMOutput(sw);
    CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
}

Console.WriteLine("Done!");