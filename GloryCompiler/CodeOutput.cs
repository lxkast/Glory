using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal abstract class CodeOutput
    {
        public abstract void EmitPush(Operand operand);
        public abstract void EmitPop(Operand operand);
        public abstract void EmitAdd(Operand operand1, Operand operand2);
        public abstract void EmitSub(Operand operand1, Operand operand2);
        public abstract void EmitMov(Operand operand1, Operand operand2);
    }

    internal class ASMOutput : CodeOutput
    {
        StreamWriter sw;
        public ASMOutput(StreamWriter streamw)
        {
            sw = streamw;
        }
        public override void EmitPush(Operand operand)
        {
            sw.Write("    ");
            sw.Write("push ");
            EmitOperand(operand);
            sw.WriteLine();
        }
        public override void EmitPop(Operand operand)
        {
            sw.Write("    ");
            sw.Write("pop ");
            if (operand.OpBase == OperandBase.Literal)
                throw new Exception("Can only pop from stack into a register, not a literal");
            EmitOperand(operand);
            sw.WriteLine();
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

        public override void EmitAdd(Operand operand1, Operand operand2)
        {
            sw.Write("    ");
            sw.Write("add ");
            EmitOperand(operand1);
            sw.Write(", ");
            EmitOperand(operand2);
            sw.WriteLine();
        }

        public override void EmitSub(Operand operand1, Operand operand2)
        {
            sw.Write("    ");
            sw.Write("sub ");
            EmitOperand(operand1);
            sw.Write(", ");
            EmitOperand(operand2);
            sw.WriteLine();
        }

        private void EmitOperand(Operand operand)
        {
            if (operand.IsDereferenced == true)
                sw.Write("[");
            if (operand.OpBase == OperandBase.Literal)
                sw.Write(operand.LiteralValue);
            else
            {
                sw.Write(operand.OpBase switch
                {
                    OperandBase.Rax => "rax",
                    OperandBase.Rcx => "rcx",
                    OperandBase.Rdx => "rdx",
                    OperandBase.Rbx => "rbx",
                    OperandBase.Rsi => "rsi",
                    OperandBase.Rdi => "rdi",
                    OperandBase.Rsp => "rsp",
                    OperandBase.Rbp => "rbp",
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
                if (operand.Offset < 0)
                {
                    sw.Write("-" + -operand.Offset);
                }
                else if (operand.Offset > 0)
                {
                    sw.Write("+" + operand.Offset);
                }
            }
            if (operand.IsDereferenced == true)
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
        public override void EmitPush(Operand operand)
        {
            Console.WriteLine("obj");
            throw new NotImplementedException();
        }
        public override void EmitPop(Operand operand)
        {
            Console.WriteLine("obj");
            throw new NotImplementedException();
        }
    }
}
