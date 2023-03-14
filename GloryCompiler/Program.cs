using System;
    
namespace GloryCompiler
{
    class Program
    {
        static void Main(string[] args)
        {

            //Lexer lexer = new Lexer(@"

            //    int add(int a, int b)
            //    {
            //        int c = a+b;
            //        return c;
            //    }

            //    string name(int[15] a)
            //    {
            //        return ""lol"";
            //    }

            //");

            Lexer lexer = new Lexer(System.IO.File.ReadAllText(args[0]));
            List<Token> tokens = lexer.Process();
            Parser parser = new Parser(tokens);
            Console.WriteLine(Environment.CurrentDirectory);
            Console.WriteLine(Path.Combine(Path.GetFileNameWithoutExtension(args[0]),args[0].Split('.')[0] + ".asm"));

            using (StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".asm")))
            {
                CodeOutput CodeOutput = new ASMOutput(sw);
                CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
            }

            Console.WriteLine("Done!");
        }
    }
}

