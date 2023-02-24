using GloryCompiler;

Console.WriteLine("Hello, World!");

Lexer lexer = new Lexer("blank ReadPointer() { string ans = \"yer\" }");
List<Token> tokens = lexer.Process();

Console.WriteLine("Done!");