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

//            Lexer lexer = new Lexer(@"
//# bubble sort algorithm

//int[8] function(int[8] myArr){
//    int i = 0;
//    printInt(myArr[2]);

//    while (i < 8)
//    {
//        int j = 0;
//        while (j < 8 - i - 1)
//        {
//		    # change > to < to sort in descending order
//            if (myArr[j] > myArr[j + 1])
//            {
//                int temp = myArr[j];
//                myArr[j] = myArr[j + 1];
//                myArr[j + 1] = temp;
//            }
//            j += 1;  # increment j inside the inner loop
//        }
//        i += 1;  # increment i inside the outer loop
//    }

//    return myArr;
//}

//int[8] idfk; # = [10,2,5,3,0,10,120,25];
//# The pain of having no array literals
//idfk[0] = 10; idfk[1] = 2; idfk[2] = 5; idfk[3] = 3;
//idfk[4] = 0; idfk[5] = 10; idfk[6] = 120; idfk[7] = 25;

//printInt(function(idfk)[2]);
//");
//            List<Token> tokens = lexer.Process();
//            Parser parser = new Parser(tokens);



            bool error = false;


            try
            {
                if (args.Length != 1) throw new Exception("Glory compiler takes one argument");
            
            
                Lexer lexer = new Lexer(System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".glr")));
                List<Token> tokens = lexer.Process();
                Parser parser = new Parser(tokens);
            
            
                using (StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".asm")))
                {
                    CodeOutput CodeOutput = new ASMOutput(sw);
                    CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
                }
            
                Process nasm = new Process();
                nasm.StartInfo.FileName = "C:/MinGW/bin/nasm.exe";
                nasm.StartInfo.Arguments = "-f win32 " + Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".asm");
                nasm.Start();
            
                Process gcc = new Process();
                gcc.StartInfo.FileName = "C:/MinGW/bin/gcc.exe";
                gcc.StartInfo.Arguments = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".obj") + " -o " + Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".exe");
                gcc.Start();
            }
            catch (Exception ex)
            {
                ConsoleColor old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = old;
                error = true;
            }
            if (!error) Console.WriteLine("Compiled to " + Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".exe"));

           //using (StreamWriter sw = new StreamWriter("program.asm"))
           //{
           //    CodeOutput CodeOutput = new ASMOutput(sw);
           //    CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
           //}
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
