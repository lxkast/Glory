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
        public ScratchRegisterPool ScratchRegisterPool = new ScratchRegisterPool();
        Function _currentFunction;

        public CodeGenerator(Parser parser, CodeOutput codeOutput)
        {
            Parser = parser;
            CodeOutput = codeOutput;
            Compile();
        }

        public void Compile()
        {
            // Declare variables and functions as global
            CodeOutput.EmitGlobal("_main");
            CodeOutput.EmitGlobal("PRINTINT");
            CodeOutput.EmitExtern("_printf");
            for (int i = 0; i < Parser.GlobalVariables.Count; i++)
            {
                CodeOutput.EmitGlobal("V" + Parser.GlobalVariables[i].Name);
            }

            // Compile literals?
            CodeOutput.StartDataSection();
            CodeOutput.EmitData("PRINTINT");
            for (int i = 0; i < Parser.GlobalVariables.Count; i++)
            {
                CodeOutput.EmitData("V" + Parser.GlobalVariables[i].Name);
            }

            CodeOutput.StartTextSection();
            // Compile each function
            for(int i = 0; i < Parser._GlobalFunctions.Count; i++)
            {
                CompileFunction(Parser._GlobalFunctions[i]);
                
            }
            // Compile global stuff
            CodeOutput.EmitLabel("_main");
            CompileStatements(Parser.GlobalStatements);
            CodeOutput.EmitRet();

        }

        public void CompileStatements(List<Statement> statements)
        {
            for(int i = 0; i < statements.Count; i++)
            {
                switch (statements[i])
                {
                    case SingleLineStatement single:
                        CompileNode(single.Expression, null);
                        break;
                    case ReturnStatement returnStatement:
                        CompileNode(returnStatement.Expression, Operand.Eax);
                        break;
                }
            }
        }

        public void CompileNode(Node node, Operand destination)
        {
            switch (node.NodeType)
            {
                // We want to add which scratch register the node is being stored in, to the node
                // so a node can see which registers its children are stored in.
                case NodeType.Plus:
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand addRight = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode(((NonLeafNode)node).RightPtr, addRight);
                    CodeOutput.EmitAdd(destination, addRight);
                    ScratchRegisterPool.FreeScratchRegister(addRight);
                    break;
                case NodeType.Minus:
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand minusRight = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode(((NonLeafNode)node).RightPtr, minusRight);
                    CodeOutput.EmitAdd(destination, minusRight);
                    ScratchRegisterPool.FreeScratchRegister(minusRight);
                    break;
                case NodeType.NumberLiteral:
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(((IntNode)node).Int));
                    break;
                case NodeType.Assignment:

                    Node leftNode = ((NonLeafNode)node).LeftPtr;

                    Operand varDestination = GetOperandForIdentifierAccess(leftNode);

                    Operand assignRight = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode(((NonLeafNode)node).RightPtr, assignRight);
                    CodeOutput.EmitMov(varDestination, assignRight);
                    ScratchRegisterPool.FreeScratchRegister(assignRight);
                    break;
                case NodeType.Variable:
                    Operand varGetDestination = GetOperandForIdentifierAccess((VariableNode)node);
                    CodeOutput.EmitMov(destination, varGetDestination);
                    break;
                case NodeType.Call:
                    CallNode callNode = (CallNode)node;
                    int paramSize = SizeOfVariablesAndAssignOffsets(callNode.Function.Parameters);
                    for (int i = callNode.Args.Count - 1; i >= 0; i--)
                    {
                        CodeOutput.EmitPush(Operand.ForLiteral(((IntNode)callNode.Args[i]).Int));
                    }
                    CodeOutput.EmitCall("F" + ((CallNode)node).Function.Name);
                    CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(paramSize));
                    CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.NativeCall:
                    NativeCallNode nativeCallNode = (NativeCallNode)node;
                    switch (nativeCallNode.Function.Name)
                    {
                        case "printInt":
                            Operand intermediateReg = ScratchRegisterPool.AllocateScratchRegister();
                            CompileNode(nativeCallNode.Args[0], intermediateReg);

                            CodeOutput.EmitMov(Operand.ForDerefLabel("PRINTINT"), intermediateReg);

                            // CDECL calling convention messes with EAX, ECX and EDX
                            CodeOutput.EmitPush(Operand.Eax);
                            CodeOutput.EmitPush(Operand.Ecx);
                            CodeOutput.EmitPush(Operand.Edx);

                            CodeOutput.EmitPush(Operand.ForLabel("PRINTINT"));
                            ScratchRegisterPool.FreeScratchRegister(intermediateReg);
                            CodeOutput.EmitCall("_printf");
                            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(4));

                            CodeOutput.EmitPop(Operand.Edx);
                            CodeOutput.EmitPop(Operand.Ecx);
                            CodeOutput.EmitPop(Operand.Eax);

                            break;
                    }
                    break;
            }
        }

        private Operand GetOperandForIdentifierAccess(Node leftNode)
        {
            Operand varDestination = null;
            if (leftNode.NodeType == NodeType.Variable)
            {
                Variable variable = ((VariableNode)leftNode).Variable;
                if (_currentFunction != null && _currentFunction.Vars.Contains(variable))
                {
                    varDestination = Operand.ForDerefReg(OperandBase.Ebp, -variable.Offset);
                }
                else
                {
                    varDestination = Operand.ForDerefLabel("V" + variable.Name);
                }
            }

            return varDestination;
        }

        public void CompileFunction(Function function)
        {
            _currentFunction = function;

            int size = SizeOfVariablesAndAssignOffsets(function.Vars);
            int paramSize = SizeOfVariablesAndAssignOffsets(function.Parameters);
            CodeOutput.EmitLabel("F" + function.Name);
            CompilePrologue(size);
            // Insert here: moving parameters into assigned stack space.
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                Variable parameter = function.Parameters[i];
                Operand intermediateRegister = ScratchRegisterPool.AllocateScratchRegister();
                CodeOutput.EmitMov(intermediateRegister, Operand.ForDerefReg(OperandBase.Ebp, parameter.Offset + 4));
                CodeOutput.EmitMov(Operand.ForDerefReg(OperandBase.Ebp, -parameter.Offset), intermediateRegister);
                ScratchRegisterPool.FreeScratchRegister(intermediateRegister);
            }
            CompileStatements(function.Code);
            CompileEpilogue(size);

            _currentFunction = null;
        }

        private int SizeOfVariablesAndAssignOffsets(List<Variable> vars)
        {
            int size = sizeOf(vars[0].Type);
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
            CodeOutput.EmitMov(Operand.Ebp, Operand.Esp);
            CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(size));
        }

        public void CompileEpilogue(int size)
        {
            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(size));
            CodeOutput.EmitMov(Operand.Esp, Operand.Ebp);
            CodeOutput.EmitPop(Operand.Ebp);
            CodeOutput.EmitRet();
        }
    }
}
