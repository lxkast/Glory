﻿using System;
    
namespace GloryCompiler
{
    class Program
    {
        static void Main(string[] args)
        {

            Lexer lexer = new Lexer(@"
            int function()
            {
                int a = 25;
                if a < 0
                {
                    return -1;
                }
                else
                {
                    return 120;
                }
                printInt(5243);
            }
            printInt(function());
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