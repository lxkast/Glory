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
        public Parser Parser;

        public CodeGenerator(Parser parser, CodeOutput codeOutput)
        {
            Parser = parser;
            CodeOutput = codeOutput;
            Compile();
        }

        public void Compile()
        {
            // Compile literals?
            // Compile each function
            for(int i = 0; i < Parser._GlobalFunctions.Count; i++)
            {
                CompileFunction(Parser._GlobalFunctions[i]);
                CompileStatements(Parser._GlobalFunctions[i].Code);
            }
            // Compile global stuff


        }

        public void CompileStatements(List<Statement> statements)
        {
            for(int i = 0; i < statements.Count; i++)
            {
                switch (statements[i])
                {
                    case SingleLineStatement single:
                        CompileNode(single.Expression);
                        break;
                }
            }
        }

        public void CompileNode(Node node)
        {
            switch (node.NodeType)
            {
                case NodeType.Plus:
                    CompileNode(((NonLeafNode)node).LeftPtr);
                    CodeOutput.EmitMov(Operand.Ebx, Operand.Eax);
                    CompileNode(((NonLeafNode)node).RightPtr);
                    CodeOutput.EmitAdd(Operand.Eax, Operand.Ebx);
                    break;
                case NodeType.Minus:
                    CompileNode(((NonLeafNode)node).LeftPtr);
                    CodeOutput.EmitMov(Operand.Ebx, Operand.Eax);
                    CompileNode(((NonLeafNode)node).RightPtr);
                    CodeOutput.EmitSub(Operand.Eax, Operand.Ebx);
                    break;
                case NodeType.NumberLiteral:
                    CodeOutput.EmitMov(Operand.Eax, Operand.ForLiteral(((IntNode)node).Int));
                    break;
                case NodeType.Assignment:
                    CompileNode(((NonLeafNode)node).RightPtr);
                    Node leftNode = ((NonLeafNode)node).LeftPtr;
                    if (leftNode.NodeType == NodeType.Variable)
                    {
                        Variable variable = ((VariableNode)leftNode).Variable;
                        //if ()
                        //{
                        //    // Check local scope(s)
                        //}
                        //else 
                        //{
                        //    // Global variables
                        //    CodeOutput.EmitMov(Operand.ForDerefReg(OperandBase.E15, variable.Offset), Operand.Eax);
                        //}
                    }
                    break;
            }
        }

        public void CompileFunction(Function function)
        {
            int size = SizeOfVariablesAndAssignOffsets(function.Vars);
            CodeOutput.EmitLabel(function.Name);
            CompilePrologue(size);
            CompileEpilogue(size);
        }

        private int SizeOfVariablesAndAssignOffsets(List<Variable> vars)
        {
            int size = 0;
            for (int i = 0; i < vars.Count; i++)
            {
                vars[i].Offset = size;
                size += sizeOf(vars[i].Type);
            }

            return size;
        }

        public int sizeOf(GloryType type)
        {
            // Int - 4 bytes
            // Bool - 4 bytes
            // String - 4 bytes (pointer)
            // List - 4 bytes (pointer)
            // Array - Size of array * corresponding data type size
            switch (type.Type)
            {
                case GloryTypes.Int:
                case GloryTypes.String:
                case GloryTypes.List:
                case GloryTypes.Bool:
                    return 4;
                case GloryTypes.Array:
                    return ((ArrayGloryType)type)._size * sizeOf(((ArrayGloryType)type).ItemType);
                default:
                    throw new Exception("WtfII");
            }
        }

        public void CompilePrologue(int size)
        {
            CodeOutput.EmitPush(Operand.Ebp);
            CodeOutput.EmitMov(Operand.Esp, Operand.Ebp);
            CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(size));
        }

        public void CompileEpilogue(int size)
        {
            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(size));
            CodeOutput.EmitMov(Operand.Ebp, Operand.Esp);
            CodeOutput.EmitPop(Operand.Ebp);
            CodeOutput.EmitRet();
        }
    }
}
