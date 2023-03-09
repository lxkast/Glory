using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer(@"

print(""Hello World"");
");
List<Token> tokens = lexer.Process();
Parser parser = new Parser(tokens);

Console.WriteLine("Done!");