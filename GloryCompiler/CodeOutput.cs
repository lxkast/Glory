using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal abstract class CodeOutput
    {
        public abstract void EmitMov(Operand operand1, Operand operand2);
    }

    internal class ASMOutput : CodeOutput
    {
        StreamWriter sw;
        public ASMOutput(StreamWriter streamw)
        {
            sw = streamw;
        }
        public override void EmitMov(Operand operand1, Operand operand2)
        {
            sw.Write("    ");
            sw.Write("mov ");
            EmitOperand(operand1);
            sw.Write(", ");
            EmitOperand(operand2);
            sw.WriteLine();
        }

        private void EmitOperand(Operand operand)
        {
            if (operand.isDereferenced == true)
                sw.Write("[");
            if (operand.opBase == OperandBase.literal)
                sw.Write(operand.literalValue);
            else
            {
                sw.Write(operand.opBase switch
                {
                    OperandBase.rax => "rax",
                    OperandBase.rcx => "rcx",
                    OperandBase.rdx => "rdx",
                    OperandBase.rbx => "rbx",
                    OperandBase.rsi => "rsi",
                    OperandBase.rdi => "rdi",
                    OperandBase.rsp => "rsp",
                    OperandBase.rbp => "rbp",
                    OperandBase.r8  => "r8",
                    OperandBase.r9  => "r9",
                    OperandBase.r10 => "r10",
                    OperandBase.r11 => "r11",
                    OperandBase.r12 => "r12",
                    OperandBase.r13 => "r13",
                    OperandBase.e14 => "e14",
                    OperandBase.e15 => "e15",
                    OperandBase.eax => "eax",
                    OperandBase.ecx => "ecx",
                    OperandBase.edx => "edx",
                    OperandBase.ebx => "ebx",
                    OperandBase.esi => "esi",
                    OperandBase.edi => "edi",
                    OperandBase.esp => "esp",
                    OperandBase.ebp => "ebp",
                    _ => throw new Exception("Unkown operand")
                }) ;
                if (operand.offset < 0)
                {
                    sw.Write("-" + -operand.offset);
                }
                else if (operand.offset > 0)
                {
                    sw.Write("+" + operand.offset);
                }
            }
            if (operand.isDereferenced == true)
                sw.Write("]");
        }
    }

    internal class COFFOutput : CodeOutput
    {
        public override void EmitMov(Operand operand1, Operand operand2)
        {
            Console.WriteLine("obj");
            throw new NotImplementedException();
        }
    }
}
