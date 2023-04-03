using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler.Generation
{
    // Represents a space in memory that's currently being used.
    // This could be a register, a place on the stack, a global, literally anything that's in place.
    internal abstract class AllocatedSpace : IDisposable
    {
        public bool IsRegister() => IsRegisterGivenDeref(IsDeref());
        public bool IsOnStack() => IsOnStackGivenDeref(IsDeref());
        public abstract bool IsDeref();
        public abstract bool IsOnStackGivenDeref(bool deref);
        public abstract bool IsRegisterGivenDeref(bool deref);
        public abstract bool IsCurrentlyRegister(OperandBase b);

        public abstract Operand Access();
        public abstract void Dispose();
    }

    // Represents an allocated space in memory that's not under the control of any of the pools.
    // E.g. eax or global variables. Literally does nothing but wrap around an Operand.
    internal class AllocatedMisc : AllocatedSpace
    {
        public Operand Operand;
        public AllocatedMisc(Operand operand) => Operand = operand;
        public override bool IsCurrentlyRegister(OperandBase b) => Operand.OpBase == b;
        public override bool IsRegisterGivenDeref(bool deref) => !deref && !IsOnStack() && Operand.OpBase != OperandBase.Label;
        public override bool IsOnStackGivenDeref(bool deref) => deref && Operand.OpBase is OperandBase.Esp or OperandBase.Ebp;
        public override bool IsDeref() => Operand.IsDereferenced;
        public override Operand Access() => Operand;
        public override void Dispose() { }
    }

    internal class AllocatedRegister : AllocatedSpace
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

        public override bool IsRegisterGivenDeref(bool deref) => !deref;
        public override bool IsOnStackGivenDeref(bool deref) => false;
        public override bool IsDeref() => Operand.IsDereferenced;

        public override void Dispose()
        {
            _creator.Free(this);
        }
    }

    internal class AllocatedDeref : AllocatedSpace
    {
        AllocatedSpace _innerSpace;
        public AllocatedDeref(AllocatedSpace innerSpace) => _innerSpace = innerSpace;

        public override Operand Access() => _innerSpace.Access().CopyWithDerefSetTo(true);
        public override bool IsCurrentlyRegister(OperandBase b) => _innerSpace.IsCurrentlyRegister(b);
        public override bool IsOnStackGivenDeref(bool deref) => _innerSpace.IsOnStackGivenDeref(deref);
        public override bool IsRegisterGivenDeref(bool deref) => _innerSpace.IsRegisterGivenDeref(deref);
        public override bool IsDeref() => true;
        public override void Dispose() => _innerSpace.Dispose();
    }
}
