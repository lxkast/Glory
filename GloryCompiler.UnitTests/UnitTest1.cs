using GloryCompiler.Generation;
using GloryCompiler.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace GloryCompiler.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestAdd()
        {
            TestEntireSystem(@"
            int add(int a, int b)
            {
                return a + b;
            }
            printInt(add(2,5));

            ",
            @"global _main
global PRINTINTASSTRING
extern _printf
section .data
    PRINTINTASSTRING: dd '%d'
section .text
Fadd:
    push ebp
    mov ebp, esp
    sub esp, 0
    mov eax, [ebp+8]
    mov edi, [ebp+12]
    add eax, edi
    jmp EFadd
EFadd:
    add esp, 0
    mov esp, ebp
    pop ebp
    ret
_main:
    push eax
    push ecx
    push edx
    sub esp, 4
    push eax
    push esi
    push ecx
    push ebx
    push edx
    sub esp, 4
    mov DWORD [esp], 5
    sub esp, 4
    mov DWORD [esp], 2
    call Fadd
    add esp, 8
    mov edi, eax
    pop edx
    pop ebx
    pop ecx
    pop esi
    pop eax
    mov DWORD [esp], edi
    push PRINTINTASSTRING
    call _printf
    add esp, 8
    pop edx
    pop ecx
    pop eax
    ret
"
            );
        }

        private static void TestEntireSystem(string input, string output)
        {
            Lexer lexer = new Lexer(input);

            //Lexer lexer = new Lexer(System.IO.File.ReadAllText(args[0]));
            List<Token> tokens = lexer.Process();
            Parser parser = new Parser(tokens);

            using (StreamWriter sw = new StreamWriter("program.asm"))
            {
                CodeOutput CodeOutput = new ASMOutput(sw);
                CodeGenerator generator = new CodeGenerator(parser, CodeOutput);
            }

            string fileContents = File.ReadAllText("program.asm");
            Assert.AreEqual(output, fileContents);
        }
    }
}