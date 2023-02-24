using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer("int a=b+2");
List<Token> tokens = lexer.Process();

Console.WriteLine("Done!");