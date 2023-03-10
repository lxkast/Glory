using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class CodeGenerator
    {
        public List<Statement> GlobalStatements;
        public List<Variable> GlobalVariables;
        public List<Function> GlobalFunctions;
        public CodeOutput CodeOutput;

        public void Compile()
        {
            // Compile literals?
            // Compile each function
            // Compile global stuff

        }



        public void CompileFunction(Function function)
        {
            List<Variable> function.Parameters();
        }

        public void CompilePrologue(int size)
        {
            CodeOutput.EmitPush(new Operand(OperandBase.rbp, false, 0, 0));
            CodeOutput.EmitMov(new Operand(OperandBase.rsp, false, 0, 0), new Operand(OperandBase.rbp, false, 0, 0));
            CodeOutput.EmitSub(new Operand(OperandBase.rsp, false, 0, 0), new Operand(OperandBase.literal, false, 0, size));
        }

        public void CompileEpilogue(int size)
        {
            CodeOutput.EmitAdd(new Operand(OperandBase.rsp, false, 0, 0), new Operand(OperandBase.literal, false, 0, size));
            CodeOutput.EmitMov(new Operand(OperandBase.rbp, false, 0, 0), new Operand(OperandBase.rsp, false, 0, 0));
            CodeOutput.EmitPop(new Operand(OperandBase.rbp, false, 0, 0));
        }
    }
}
