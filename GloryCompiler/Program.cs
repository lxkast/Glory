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

            #int[3] array(int[3] returnarray){return returnarray;}
            int[3][2] a; 
            a[0][0] = 1; 
            a[0][1] = 2;
            a[0][2] = 3;
            #int[3] b = array(a[0]);
            printInt(a[0][1]);









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