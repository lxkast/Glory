using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer("bool randomBool");
List<Token> tokens = lexer.Process();

Console.WriteLine("Done!");