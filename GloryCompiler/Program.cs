using System;
using GloryCompiler.Generation;
using GloryCompiler.Syntax;

namespace GloryCompiler
{
    class Program
    {
        static void Main(string[] args)
        {

            Lexer lexer = new Lexer(@"

            int a = 5; int b = 2;



int factorial()
{
    return a/b;
}

printInt(factorial());































            ")
            {

            };

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