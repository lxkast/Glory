using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal abstract class CodeOutput
    {
        public abstract void StartTextSection();
        public abstract void StartDataSection();
        public abstract void EmitPush(Operand operand);
        public abstract void EmitPop(Operand operand);
        public abstract void EmitAdd(Operand operand1, Operand operand2);
        public abstract void EmitSub(Operand operand1, Operand operand2);

        public abstract void EmitMul(Operand operand);
        public abstract void EmitDiv(Operand operand);
        public abstract void EmitMov(Operand operand1, Operand operand2);
        public abstract void EmitCall(string func);
        public abstract void EmitRet();
        public abstract void EmitLabel(string name);
        public abstract void EmitGlobal(string name);
        public abstract void EmitExtern(string name);
        public abstract void EmitData(string name, string data);
    }

    internal class ASMOutput : CodeOutput
    {
        StreamWriter sw;
        public ASMOutput(StreamWriter streamw)
        {
            sw = streamw;
        }
        public override void StartTextSection()
        {
            sw.WriteLine("section .text");
        }
        public override void StartDataSection()
        {
            sw.WriteLine("section .data");
        }
        public override void EmitGlobal(string name)
        {
            sw.WriteLine("global " + name);
        }
        public override void EmitData(string name, string str)
        {
            if (str == null)
                sw.WriteLine("    " + name + ": dd 0");
            else
                sw.WriteLine("    " + name + ": dd '" + str + "'");
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

        public override void EmitMul(Operand operand)
        {
            sw.Write("    mul ");
            EmitOperand(operand);
            sw.WriteLine();
        }
        public override void EmitDiv(Operand operand)
        {
            sw.Write("    div ");
            EmitOperand(operand);
            sw.WriteLine();
        }

        public override void EmitCall(string func)
        {
            sw.WriteLine("    call " + func);
        }

        public override void EmitRet()
        {
            sw.WriteLine("    ret");
        }

        public override void EmitLabel(string name)
        {
            sw.WriteLine(name + ":");
        }
        public override void EmitExtern(string name)
        {
            sw.WriteLine("extern " + name);
        }

        private void EmitOperand(Operand operand)
        {
            if (operand.IsDereferenced == true)
                sw.Write("[");
            if (operand.OpBase == OperandBase.Literal)
                sw.Write(operand.LiteralValue);
            else if (operand.OpBase == OperandBase.Label)
                sw.Write(operand.LabelName);
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
                    OperandBase.R8  => "r8",
                    OperandBase.R9  => "r9",
                    OperandBase.R10 => "r10",
                    OperandBase.R11 => "r11",
                    OperandBase.R12 => "r12",
                    OperandBase.R13 => "r13",
                    OperandBase.E14 => "e14",
                    OperandBase.E15 => "e15",
                    OperandBase.Eax => "eax",
                    OperandBase.Ecx => "ecx",
                    OperandBase.Edx => "edx",
                    OperandBase.Ebx => "ebx",
                    OperandBase.Esi => "esi",
                    OperandBase.Edi => "edi",
                    OperandBase.Esp => "esp",
                    OperandBase.Ebp => "ebp",
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

    //internal class COFFOutput : CodeOutput
    //{
    //    public override void EmitMov(Operand operand1, Operand operand2)
    //    {
    //        Console.WriteLine("obj");
    //        throw new NotImplementedException();
    //    }
    //    public override void EmitPush(Operand operand)
    //    {
    //        Console.WriteLine("obj");
    //        throw new NotImplementedException();
    //    }
    //    public override void EmitPop(Operand operand)
    //    {
    //        Console.WriteLine("obj");
    //        throw new NotImplementedException();
    //    }
    //}
}
