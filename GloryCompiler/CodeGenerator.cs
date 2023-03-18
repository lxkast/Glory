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
        public ScratchRegisterPool ScratchRegisterPool;
        Function _currentFunction;
        public int stackFrameSize;

        public CodeGenerator(Parser parser, CodeOutput codeOutput)
        {
            Parser = parser;
            CodeOutput = codeOutput;
            ScratchRegisterPool = new ScratchRegisterPool(codeOutput, this);
            Compile();
        }

        public void Compile()
        {
            // Declare variables and functions as global
            CodeOutput.EmitGlobal("_main");
            CodeOutput.EmitGlobal("PRINTINTASSTRING");
            CodeOutput.EmitExtern("_printf");
            for (int i = 0; i < Parser.GlobalVariables.Count; i++)
            {
                CodeOutput.EmitGlobal("V" + Parser.GlobalVariables[i].Name);
            }

            // Compile literals?
            CodeOutput.StartDataSection();
            CodeOutput.EmitData("PRINTINTASSTRING", "%d");
            for (int i = 0; i < Parser.GlobalVariables.Count; i++)
            {
                CodeOutput.EmitData("V" + Parser.GlobalVariables[i].Name, null);
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
                        CodeOutput.EmitJmp("EF" + _currentFunction.Name);
                        break;
                    case IfStatement ifStatement:
                        Operand conditionResult = ScratchRegisterPool.AllocateScratchRegister();
                        CompileNode(ifStatement.Condition, conditionResult);
                        CodeOutput.EmitPush(Operand.ForLiteral(0)); 
                        CodeOutput.EmitCmp(conditionResult, Operand.ForDerefReg(OperandBase.Esp)); // this WILL break if we run out of registers
                        ScratchRegisterPool.FreeScratchRegister(conditionResult);
                        string falseLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitJe(falseLabel);
                        CompileStatements(ifStatement.Code);
                        string doneLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitJmp(doneLabel);
                        CodeOutput.EmitLabel(falseLabel);
                        if (ifStatement.Else != null)
                        {
                        CompileStatements(ifStatement.Else.Code);
                        }
                        CodeOutput.EmitLabel(doneLabel);
                        CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(4));
                        break;
                    case WhileStatement whileStatement:
                        string topLabel = CodeOutput.ReserveNextLabel();
                        string whiledoneLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitLabel(topLabel);
                        Operand whileconditionResult = ScratchRegisterPool.AllocateScratchRegister();
                        CompileNode(whileStatement.Condition, whileconditionResult);
                        CodeOutput.EmitPush(Operand.ForLiteral(0));
                        CodeOutput.EmitCmp(whileconditionResult, Operand.ForDerefReg(OperandBase.Esp)); // this WILL break if we run out of registers
                        ScratchRegisterPool.FreeScratchRegister(whileconditionResult);
                        CodeOutput.EmitJe(whiledoneLabel);
                        if (whileStatement.Code != null)
                        {
                            CompileStatements(whileStatement.Code);
                        }
                        CodeOutput.EmitJmp(topLabel);
                        CodeOutput.EmitLabel(whiledoneLabel);
                        CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(4));
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
                    CodeOutput.EmitSub(destination, minusRight);
                    ScratchRegisterPool.FreeScratchRegister(minusRight);
                    break;
                case NodeType.Multiply:
                    CompileNode((((NonLeafNode)node).LeftPtr), Operand.Eax);
                    Operand mulrightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), mulrightReg);
                    if (ScratchRegisterPool.IsRegisterOccupied(Operand.Edx))
                    {
                        CodeOutput.EmitPush(Operand.Edx);
                        CodeOutput.EmitMul(mulrightReg);
                        CodeOutput.EmitPop(Operand.Edx);
                    }
                    else
                        CodeOutput.EmitMul(mulrightReg);
                    
                    ScratchRegisterPool.FreeScratchRegister(mulrightReg);
                    if (destination != Operand.Eax)
                        CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.Divide:
                case NodeType.Div:
                    CompileNode((((NonLeafNode)node).LeftPtr), Operand.Eax);
                    Operand dividerightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), dividerightReg);
                    CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                    CodeOutput.EmitDiv(dividerightReg);
                    ScratchRegisterPool.FreeScratchRegister(dividerightReg);
                    if (destination != Operand.Eax)
                        CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.Mod:
                    CompileNode((((NonLeafNode)node).LeftPtr), Operand.Eax);
                    Operand moddividerightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), moddividerightReg);
                    if (moddividerightReg == Operand.Edx)
                    {
                        CodeOutput.EmitPush(Operand.Edx);
                        ScratchRegisterPool.FreeScratchRegister(moddividerightReg);
                        CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                        CodeOutput.EmitDiv(Operand.ForDerefReg(OperandBase.Esp, 0));
                        if (destination != Operand.Edx)
                            CodeOutput.EmitMov(destination, Operand.Edx);
                        CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(4));
                    }
                    else
                    {
                        CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                        CodeOutput.EmitDiv(moddividerightReg);
                        ScratchRegisterPool.FreeScratchRegister(moddividerightReg);
                        if (destination != Operand.Edx)
                            CodeOutput.EmitMov(destination, Operand.Edx);
                    }
                    break;
                case NodeType.NumberLiteral:
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(((IntNode)node).Int));
                    break;
                case NodeType.BoolLiteral:
                    if (((BoolNode)node).Bool == true)
                        CodeOutput.EmitMov(destination, Operand.ForLiteral(1));
                    else
                        CodeOutput.EmitMov(destination, Operand.ForLiteral(0));
                    break;
                // note for alex: put all of these into a function or something (maybe not double equals i did that one differently when i started)
                case NodeType.DoubleEquals:
                    CompileNode((((NonLeafNode)node).LeftPtr), destination);
                    Operand doubleequalsrightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), doubleequalsrightReg);
                    CodeOutput.EmitXor(Operand.Eax, Operand.Eax);
                    CodeOutput.EmitCmp(destination, doubleequalsrightReg);
                    ScratchRegisterPool.FreeScratchRegister(doubleequalsrightReg);
                    CodeOutput.EmitSete(Operand.Al);
                    CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.LessThan:
                    CompileNode((((NonLeafNode)node).LeftPtr), destination);
                    Operand lessthanrightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), lessthanrightReg);
                    CodeOutput.EmitCmp(destination, lessthanrightReg);
                    ScratchRegisterPool.FreeScratchRegister(lessthanrightReg);
                    string jlLabel = CodeOutput.ReserveNextLabel();
                    string jlDoneLabel = CodeOutput.ReserveNextLabel();
                    CodeOutput.EmitJl(jlLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(0));
                    CodeOutput.EmitJmp(jlDoneLabel);
                    CodeOutput.EmitLabel(jlLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(1));
                    CodeOutput.EmitLabel(jlDoneLabel);
                    break;
                case NodeType.LessThanEquals:
                    CompileNode((((NonLeafNode)node).LeftPtr), destination);
                    Operand lessthanequalsrightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), lessthanequalsrightReg);
                    CodeOutput.EmitCmp(destination, lessthanequalsrightReg);
                    ScratchRegisterPool.FreeScratchRegister(lessthanequalsrightReg);
                    string jleLabel = CodeOutput.ReserveNextLabel();
                    string jleDoneLabel = CodeOutput.ReserveNextLabel();
                    CodeOutput.EmitJle(jleLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(0));
                    CodeOutput.EmitJmp(jleDoneLabel);
                    CodeOutput.EmitLabel(jleLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(1));
                    CodeOutput.EmitLabel(jleDoneLabel);
                    break;
                case NodeType.GreaterThan:
                    CompileNode((((NonLeafNode)node).LeftPtr), destination);
                    Operand greaterthanrightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), greaterthanrightReg);
                    CodeOutput.EmitCmp(destination, greaterthanrightReg);
                    ScratchRegisterPool.FreeScratchRegister(greaterthanrightReg);
                    string jgLabel = CodeOutput.ReserveNextLabel();
                    string jgDoneLabel = CodeOutput.ReserveNextLabel();
                    CodeOutput.EmitJg(jgLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(0));
                    CodeOutput.EmitJmp(jgDoneLabel);
                    CodeOutput.EmitLabel(jgLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(1));
                    CodeOutput.EmitLabel(jgDoneLabel);
                    break;
                case NodeType.GreaterThanEquals:
                    CompileNode((((NonLeafNode)node).LeftPtr), destination);
                    Operand greaterthanequalsrightReg = ScratchRegisterPool.AllocateScratchRegister();
                    CompileNode((((NonLeafNode)node).RightPtr), greaterthanequalsrightReg);
                    CodeOutput.EmitCmp(destination, greaterthanequalsrightReg);
                    ScratchRegisterPool.FreeScratchRegister(greaterthanequalsrightReg);
                    string jgeLabel = CodeOutput.ReserveNextLabel();
                    string jgeDoneLabel = CodeOutput.ReserveNextLabel();
                    CodeOutput.EmitJge(jgeLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(0));
                    CodeOutput.EmitJmp(jgeDoneLabel);
                    CodeOutput.EmitLabel(jgeLabel);
                    CodeOutput.EmitMov(destination, Operand.ForLiteral(1));
                    CodeOutput.EmitLabel(jgeDoneLabel);
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

                    if (destination.IsDereferenced)
                    {
                        Operand intermediateReg = ScratchRegisterPool.AllocateScratchRegister();
                        CodeOutput.EmitMov(intermediateReg, varGetDestination);
                        CodeOutput.EmitMov(destination, intermediateReg);
                    }
                    else CodeOutput.EmitMov(destination, varGetDestination);

                    break;
                case NodeType.Call:
                    CallNode callNode = (CallNode)node;
                    int paramSize = SizeOfVariablesAndAssignOffsets(callNode.Function.Parameters);

                    for (int i = callNode.Args.Count - 1; i >= 0; i--)
                    {
                        //Operand arg = ScratchRegisterPool.AllocateScratchRegister();
                        CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(4)); // TODO: Use the actual size of the parameter
                        CompileNode(callNode.Args[i], Operand.ForDerefReg(OperandBase.Esp));
                        stackFrameSize += 4;
                        //CodeOutput.EmitPush(arg);
                        //ScratchRegisterPool.FreeScratchRegister(arg);
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
                            

                            // CDECL calling convention messes with EAX, ECX and EDX
                            CodeOutput.EmitPush(Operand.Eax);
                            CodeOutput.EmitPush(Operand.Ecx);
                            CodeOutput.EmitPush(Operand.Edx);

                            stackFrameSize += 12;

                            //Operand intermediateReg = ScratchRegisterPool.AllocateScratchRegister();
                            CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(4));
                            CompileNode(nativeCallNode.Args[0], Operand.ForDerefReg(OperandBase.Esp));
                            //CodeOutput.EmitPush(intermediateReg);
                            //ScratchRegisterPool.FreeScratchRegister(intermediateReg);

                            CodeOutput.EmitPush(Operand.ForLabel("PRINTINTASSTRING"));
                            CodeOutput.EmitCall("_printf");
                            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(8));

                            CodeOutput.EmitPop(Operand.Edx);
                            CodeOutput.EmitPop(Operand.Ecx);
                            CodeOutput.EmitPop(Operand.Eax);

                            stackFrameSize -= 12;

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
            /*Lets say we have an add function:
              
              int add(int a, int b)
              {
                   int c = a + b;
                   return c;
              }
                 
              The stack is created like this:
              
                       +--------------+
                       |      b       |   <- argumemts are pushed onto the stack in reverse order
                       |--------------|
                       |      a       |
                       |--------------|
                       |return address|   <- the return address is automatically pushed when running a "call" instruction
                       |--------------|
                       |   old ebp    |   
                       |--------------|
                       |      c       |   <- local variables
                       +--------------+

                        +--------------+
                        |      b       |
                        |--------------|
                        |      a       | 
                        |--------------|
                        |return address|  <- stack pointer (ESP register)
                        |--------------|
                        |   old ebp    |  
                        |--------------|
                        |      c       |  
                        +--------------+








                        
            */


            _currentFunction = function;

            int size = SizeOfVariablesAndAssignOffsets(function.Vars);
            int paramsize = SizeOfVariablesAndAssignOffsets(function.Parameters);
            CodeOutput.EmitLabel("F" + function.Name);

            CompilePrologue(size);
            stackFrameSize += size;

            for (int i = 0; i < function.Parameters.Count; i++)
            {
                function.Vars[i].Offset *= -1;
                function.Vars[i].Offset -= 4;
            }
            for (int i = function.Parameters.Count; i < function.Vars.Count; i++)
            {
                function.Vars[i].Offset -= paramsize;
            }
            CompileStatements(function.Code);

            CompileEpilogue(size);
            stackFrameSize -= size;

            _currentFunction = null;
        }

        private int SizeOfVariablesAndAssignOffsets(List<Variable> vars)
        {
            if (vars.Count == 0) return 0;
            int size = sizeOf(vars[0].Type);
            for (int i = 0; i < vars.Count; i++)
            {
                vars[i].Offset = size;
                size += sizeOf(vars[i].Type);
            }
            return size - 4;
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
            CodeOutput.EmitLabel("EF" + _currentFunction.Name);
            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(size));
            CodeOutput.EmitMov(Operand.Esp, Operand.Ebp);
            CodeOutput.EmitPop(Operand.Ebp);
            CodeOutput.EmitRet();
        }
    }
}
