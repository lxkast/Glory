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
    CodeOutput.EmitMov(new Operand(OperandBase.rcx, true, 12, 0), new Operand(OperandBase.literal, false, 0, 12));
}

Console.WriteLine("Done!");