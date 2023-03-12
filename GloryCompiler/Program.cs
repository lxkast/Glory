using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"
int abc()
{
    return 2;
}
");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

using (StreamWriter sw = new StreamWriter("program.asm"))
{
    CodeOutput CodeOutput = new ASMOutput(sw);
    CodeOutput.EmitMov(Operand.Rcx, Operand.ForLiteral(12));
}

Console.WriteLine("Done!");