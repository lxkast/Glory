using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler.Generation
{
    // Represents a space in memory that's currently being used.
    // This could be a register, a place on the stack, a global, literally anything that's in place.
    internal abstract class AllocatedSpace
    {
        public abstract bool IsOnStack();
        public abstract bool IsRegister();
        public abstract bool IsCurrentlyRegister(OperandBase b);
        public abstract Operand Access();
    }

    internal abstract class AllocatedIndexAccess
    {
        public AllocatedRegister Register;
    }

    // Represents an allocated space in memory that's not under the control of any of the pools.
    // E.g. eax or global variables. Literally does nothing but wrap around an Operand.
    internal class AllocatedMisc : AllocatedSpace
    {
        public Operand Operand;
        public AllocatedMisc(Operand operand) => Operand = operand;
        public override bool IsCurrentlyRegister(OperandBase b) => Operand.OpBase == b;
        public override bool IsRegister() => !IsOnStack() && Operand.OpBase != OperandBase.Label;
        public override bool IsOnStack() => Operand.IsDereferenced && Operand.OpBase is OperandBase.Esp or OperandBase.Ebp;
        public override Operand Access() => Operand;
    }

    internal class AllocatedRegister : AllocatedSpace, IDisposable
    {
        RegisterAllocator _creator;
        public Operand Operand;

        // EAX allocation
        public bool WasEaxInUse;

        public AllocatedRegister(RegisterAllocator creator, Operand operand, bool wasEaxInUse)
        {
            _creator = creator;
            WasEaxInUse = wasEaxInUse;
            Operand = operand;
        }

        public override bool IsCurrentlyRegister(OperandBase b)
        {
            // Temporary
            return Operand.OpBase == b;
        }

        public override Operand Access()
        {
            return Operand;
        }

        public override bool IsRegister() => true;
        public override bool IsOnStack() => false;

        public void Dispose()
        {
            _creator.Free(this);
        }
    }
}
