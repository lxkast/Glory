using System;
using System.Diagnostics;
using GloryCompiler.Generation;
using GloryCompiler.Syntax;

namespace GloryCompiler
{
    class Program
    {
        static void Main(string[] args)
        {

            Lexer lexer = new Lexer(@"
# bubble sort algorithm
int factorial(int n)
{
    int answer = 1;
    while n > 0
    {
        answer *= n;
        n -=1 ;
    }
    return answer;
}
printInt(factorial(6));
}");
            List<Token> tokens = lexer.Process();
            Parser parser = new Parser(tokens);



            bool error = false;


            //try
            //{
            //    if (args.Length != 1) throw new Exception("Glory compiler takes one argument");
            //
            //
            //    Lexer lexer = new Lexer(System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".glr")));
            //    List<Token> tokens = lexer.Process();
            //    Parser parser = new Parser(tokens);
            //
            //
            //    using (StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".asm")))
            //    {
            //        CodeOutput CodeOutput = new ASMOutput(sw);
            //        CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
            //    }
            //
            //    Process nasm = new Process();
            //    nasm.StartInfo.FileName = "C:/MinGW/bin/nasm.exe";
            //    nasm.StartInfo.Arguments = "-f win32 " + Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".asm");
            //    nasm.Start();
            //
            //    Process gcc = new Process();
            //    gcc.StartInfo.FileName = "C:/MinGW/bin/gcc.exe";
            //    gcc.StartInfo.Arguments = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".obj") + " -o " + Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".exe");
            //    gcc.Start();
            //}
            //catch (Exception ex)
            //{
            //    ConsoleColor old = Console.ForegroundColor;
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine(ex.Message);
            //    Console.ForegroundColor = old;
            //    error = true;
            //}
            //if (!error) Console.WriteLine("Compiled to " + Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".exe"));

           using (StreamWriter sw = new StreamWriter("program.asm"))
           {
               CodeOutput CodeOutput = new ASMOutput(sw);
               CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
           }
        }
    }
}

/*
 * #int[5] moveArr()
            #{
            #    int[5] a;
            #    return a;
            #}
            #
            #int[5] moveArrTwo()
            #{
            #    return moveArr();
            #}
            #int[5] b = moveArrTwo();

            int a(int n)
            {
                n += 1;
                return n / 2 + n / 3;
            }
            
            int t = a(6);

            #int factorial(int n)
            #{
            #    int answer = 1;
            #    while n > 0
            #    {
            #        answer *= n;
            #        n -=1 ;
            #    }
            #    return answer;
            #}
            #printInt(factorial(3));
*/
