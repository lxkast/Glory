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
            CodeOutput.EmitPush(Operand.Rbp);
            CodeOutput.EmitMov(Operand.Rsp, Operand.Rbp);
            CodeOutput.EmitSub(Operand.Rsp, Operand.ForLiteral(size));
        }

        public void CompileEpilogue(int size)
        {
            CodeOutput.EmitAdd(Operand.Rsp, Operand.ForLiteral(size));
            CodeOutput.EmitMov(Operand.Rbp, Operand.Rsp);
            CodeOutput.EmitPop(Operand.Rbp);
        }
    }
}
