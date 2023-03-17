using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class ScratchRegisterPool
    {
        private int numScratchRegisters = 5;
        private uint availableRegistersBitmap; // 32-bit bitmap
        public CodeOutput CodeOutput;
        Dictionary<int, Operand> registerNames = new Dictionary<int, Operand> {
            { 0, Operand.Edi },
            { 1, Operand.Esi },
            { 2, Operand.Ecx },
            { 3, Operand.Ebx },
            { 4, Operand.Edx }
        };

        public ScratchRegisterPool()
        {
            availableRegistersBitmap = (1u << numScratchRegisters) - 1u; // init all registers as available
        }

        public Operand AllocateScratchRegister()
        {
            int regNum = -1;
            for (int i = 0; i < numScratchRegisters; i++)
            {
                if ((availableRegistersBitmap & (1u << i)) != 0)
                {
                    regNum = i;
                    availableRegistersBitmap &= ~(1u << i); // mark the register as in use
                    break;
                }
            }
            return GetRegisterName(regNum);
        }

        public void FreeScratchRegister(Operand reg)
        {
            int regNum = reg.OpBase switch
            {
                OperandBase.Edi => 0,
                OperandBase.Esi => 1,
                OperandBase.Ecx => 2,
                OperandBase.Ebx => 3,
                OperandBase.Edx => 4,
                _ => throw new Exception("Cannot free non-register")
            };

            
            if (regNum >= 0 && regNum < numScratchRegisters)
            {
                availableRegistersBitmap |= (1u << regNum); // mark the register as available
            }
        }
        public bool IsRegisterOccupied(Operand reg)
        {
            int regNum = reg.OpBase switch
            {
                OperandBase.Edi => 0,
                OperandBase.Esi => 1,
                OperandBase.Ecx => 2,
                OperandBase.Ebx => 3,
                OperandBase.Edx => 4,
                _ => throw new Exception("Invalid register")
            };
            uint mask = 1u << regNum;
            uint masked = mask & availableRegistersBitmap;
            if (masked == 0)
                return false;
            else
                return true;
        }
        public Operand GetRegisterName(int regNum)
        {
            return registerNames.ContainsKey(regNum) ? registerNames[regNum] : null;
        }

        #region Dont Use
        public uint PushAllocatedScratchRegisters()
        {
            uint savedBitmap = availableRegistersBitmap;
            for (int i = 0; i<numScratchRegisters ; i++)
            {
                if ((availableRegistersBitmap & (1u << i)) == 0)
                {
                    {
                        CodeOutput.EmitPush(registerNames[i]);
                    }
                }
            }
            return savedBitmap;
        }
        public void PopOccupiedRegisters(uint registerBitmap)
        {
            int currentRegister = numScratchRegisters - 1;
            while (currentRegister >= 0)
            {
                if ((availableRegistersBitmap & (1u << currentRegister)) == 0)
                {
                    CodeOutput.EmitPop(registerNames[currentRegister]);
                }
                currentRegister--;
            }
        }

        #endregion


    }
}
