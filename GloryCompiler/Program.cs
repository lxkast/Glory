using System;
    
namespace GloryCompiler
{
    class Program
    {
        static void Main(string[] args)
        {

            Lexer lexer = new Lexer(@"
                
                int c = 135 + 45135;
                int multiply (int a, int b)
                {
                    return a * b;
                }
                printInt( multiply(multiply(c,5) + 5 - 2 + 150 * 4 - 34 + 124 - 6435 * 12 * 12,2));
            ");

            //Lexer lexer = new Lexer(System.IO.File.ReadAllText(args[0]));
            List<Token> tokens = lexer.Process();
            Parser parser = new Parser(tokens);
            
            
            //using (StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".asm")))
            //{
            //    CodeOutput CodeOutput = new ASMOutput(sw);
            //    CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
            //}

            using (StreamWriter sw = new StreamWriter("program.asm"))
            {
                CodeOutput CodeOutput = new ASMOutput(sw);
                CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
            }

            Console.WriteLine("Done!");
        }
    }
}