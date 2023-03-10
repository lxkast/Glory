using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Operand
    {
        public bool isDereferenced;
        public OperandBase opBase;
        public int offset;
        public int literalValue;
    
        public Operand(OperandBase Base, bool IsDereferenced, int Offset, int LiteralValue)
        {
            opBase = Base;
            isDereferenced = IsDereferenced;
            offset = Offset;
            literalValue = LiteralValue;
        }
    }

    public enum OperandBase
    {
        rax,
        rcx,
        rdx,
        rbx,
        rsi,
        rdi,
        rsp,
        rbp,
        r8,
        r9,
        r10,
        r11,
        r12,
        r13,
        e14,
        e15,
        eax,
        ecx,
        edx,
        ebx,
        esi,
        edi,
        esp,
        ebp,
        literal
    }
}
