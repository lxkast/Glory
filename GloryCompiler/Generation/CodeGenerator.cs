using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloryCompiler.Representation;
using GloryCompiler.Syntax;

namespace GloryCompiler.Generation
{
    internal class CodeGenerator
    {
        public List<Statement> GlobalStatements;
        public List<Variable> GlobalVariables;
        public List<Function> GlobalFunctions;
        public CodeOutput CodeOutput;
        public Parser Parser;
        public RegisterAllocator RegisterPool;
        Function _currentFunction;
        public int stackFrameSize;

        public CodeGenerator(Parser parser, CodeOutput codeOutput)
        {
            Parser = parser;
            CodeOutput = codeOutput;
            RegisterPool = new RegisterAllocator(codeOutput, this);
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
            for (int i = 0; i < Parser._GlobalFunctions.Count; i++)
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
            for (int i = 0; i < statements.Count; i++)
            {
                switch (statements[i])
                {
                    case SingleLineStatement single:
                        CompileNode(single.Expression, null);
                        break;
                    case ReturnStatement returnStatement:
                        CompileNode(returnStatement.Expression, new AllocatedMisc(Operand.Eax));
                        CodeOutput.EmitJmp("EF" + _currentFunction.Name);
                        break;
                    case IfStatement ifStatement:

                        // Compile conditional jump to false
                        using (AllocatedRegister conditionResult = RegisterPool.Allocate())
                        {
                            CompileNode(ifStatement.Condition, conditionResult);
                            CodeOutput.EmitCmp(conditionResult.Access(), Operand.ForLiteral(0));
                        }

                        string falseLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitJe(falseLabel);

                        // Compile code for true
                        CompileStatements(ifStatement.Code);
                        string doneLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitJmp(doneLabel);

                        // Compile code for false
                        CodeOutput.EmitLabel(falseLabel);
                        if (ifStatement.Else != null)
                            CompileStatements(ifStatement.Else.Code);

                        CodeOutput.EmitLabel(doneLabel);
                        break;

                    case WhileStatement whileStatement:

                        // Create begin/end labels
                        string topLabel = CodeOutput.ReserveNextLabel();
                        string whiledoneLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitLabel(topLabel);

                        // Compile conditional jump to end
                        using (AllocatedRegister whileResult = RegisterPool.Allocate())
                        {
                            CompileNode(whileStatement.Condition, whileResult);
                            CodeOutput.EmitCmp(whileResult.Access(), Operand.ForLiteral(0));
                        }

                        CodeOutput.EmitJe(whiledoneLabel);

                        // Compile body
                        if (whileStatement.Code != null)
                            CompileStatements(whileStatement.Code);

                        CodeOutput.EmitJmp(topLabel);

                        // Compile end
                        CodeOutput.EmitLabel(whiledoneLabel);
                        break;
                }
            }
        }

        public void CompileNode(Node node, AllocatedSpace destination)
        {
            switch (node.NodeType)
            {
                case NodeType.Plus:

                    // TODO: Potential optimization, put the "more complicated" thing on the left (since order doesn't matter) so the more complex thing happens when there's one more reg free
                    NonLeafNode nlNode = (NonLeafNode)node;
                    CompileNode(nlNode.LeftPtr, destination);

                    using (AllocatedRegister addRight = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode.RightPtr, addRight);
                        CodeOutput.EmitAdd(destination.Access(), addRight.Access());
                    }

                    break;
                case NodeType.Minus:

                    NonLeafNode nlNode2 = (NonLeafNode)node;
                    CompileNode(nlNode2.LeftPtr, destination);

                    using (AllocatedRegister addRight = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode2.RightPtr, addRight);
                        CodeOutput.EmitSub(destination.Access(), addRight.Access());
                    }

                    break;
                case NodeType.Multiply:
                    NonLeafNode nlNode3 = (NonLeafNode)node;
                    CompileNode(nlNode3.LeftPtr, new AllocatedMisc(Operand.Eax));

                    using (AllocatedRegister mulRight = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode3.RightPtr, mulRight);

                        // Back up edx if necessary.
                        if (RegisterPool.IsRegisterAllocated(Operand.Edx))
                        {
                            CodeOutput.EmitPush(Operand.Edx);
                            CodeOutput.EmitMul(mulRight.Access());
                            CodeOutput.EmitPop(Operand.Edx);
                        }
                        else
                            CodeOutput.EmitMul(mulRight.Access());
                    }

                    // If the destination is not already eax, output to eax.
                    if (!destination.IsCurrentlyRegister(OperandBase.Eax))
                        CodeOutput.EmitMov(destination.Access(), Operand.Eax);

                    break;
                case NodeType.Divide:
                case NodeType.Div:

                    NonLeafNode nlNode4 = (NonLeafNode)node;
                    CompileNode(nlNode4.LeftPtr, new AllocatedMisc(Operand.Eax));

                    using (AllocatedRegister divRight = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode4.RightPtr, divRight);
                        CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                        CodeOutput.EmitDiv(divRight.Access());
                    }

                    // If the destiantion is not already eax, output to eax.
                    if (!destination.IsCurrentlyRegister(OperandBase.Eax))
                        CodeOutput.EmitMov(destination.Access(), Operand.Eax);

                    break;

                case NodeType.Mod:

                    CompileNode(((NonLeafNode)node).LeftPtr, new AllocatedMisc(Operand.Eax));

                    using (AllocatedRegister modRight = RegisterPool.Allocate())
                    {
                        CompileNode(((NonLeafNode)node).RightPtr, modRight);

                        // !!! As long as we make no pool changes from this point onwards, we can access once like this. !!!
                        Operand op = modRight.Access();

                        // Spill edx onto the stack if that's what the allocator picked for this.
                        if (op == Operand.Edx)
                        {
                            CodeOutput.EmitPush(Operand.Edx);
                            CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                            CodeOutput.EmitDiv(Operand.ForDerefReg(OperandBase.Esp, 0));
                            CodeOutput.EmitMov(destination.Access(), Operand.Edx);
                            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(4));
                        }
                        else
                        {
                            // TODO: What happens if EDX is in-use by something else??
                            CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                            CodeOutput.EmitDiv(modRight.Access());                            

                            if (!destination.IsCurrentlyRegister(OperandBase.Edx))
                                CodeOutput.EmitMov(destination.Access(), Operand.Edx);
                        }
                    }

                    break;
                case NodeType.NumberLiteral:
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(((IntNode)node).Int));
                    break;
                case NodeType.BoolLiteral:
                    if (((BoolNode)node).Bool == true)
                        CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(1));
                    else
                        CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(0));
                    break;

                // note for alex: put all of these into a function or something (maybe not double equals i did that one differently when i started)
                case NodeType.DoubleEquals:
                    NonLeafNode nlNode5 = (NonLeafNode)node;

                    CompileNode(nlNode5.LeftPtr, destination);

                    using (AllocatedRegister doubleequalsrightReg = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode5.RightPtr, doubleequalsrightReg);
                        CodeOutput.EmitXor(Operand.Eax, Operand.Eax);
                        CodeOutput.EmitCmp(destination.Access(), doubleequalsrightReg.Access());
                    }

                    CodeOutput.EmitSete(Operand.Al);
                    CodeOutput.EmitMov(destination.Access(), Operand.Eax);
                    break;
                case NodeType.LessThan:
                    NonLeafNode nlNode6 = (NonLeafNode)node;

                    CompileNode(nlNode6.LeftPtr, destination);

                    // Emit conditional jump to true
                    using (AllocatedRegister lessthanrightReg = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode6.RightPtr, lessthanrightReg);
                        CodeOutput.EmitCmp(destination.Access(), lessthanrightReg.Access());
                    }
                    
                    string jlLabel = CodeOutput.ReserveNextLabel();
                    string jlDoneLabel = CodeOutput.ReserveNextLabel();

                    CodeOutput.EmitJl(jlLabel);

                    // Emit false case
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(0));
                    CodeOutput.EmitJmp(jlDoneLabel);

                    // Emit true case
                    CodeOutput.EmitLabel(jlLabel);
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(1));

                    CodeOutput.EmitLabel(jlDoneLabel);
                    break;

                case NodeType.LessThanEquals:
                    NonLeafNode nlNode7 = (NonLeafNode)node;

                    CompileNode(nlNode7.LeftPtr, destination);

                    // Emit conditional jump to true
                    using (AllocatedRegister lessthanrightReg = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode7.RightPtr, lessthanrightReg);
                        CodeOutput.EmitCmp(destination.Access(), lessthanrightReg.Access());
                    }

                    string jleLabel = CodeOutput.ReserveNextLabel();
                    string jleDoneLabel = CodeOutput.ReserveNextLabel();

                    CodeOutput.EmitJl(jleLabel);

                    // Emit false case
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(0));
                    CodeOutput.EmitJmp(jleDoneLabel);

                    // Emit true case
                    CodeOutput.EmitLabel(jleLabel);
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(1));

                    CodeOutput.EmitLabel(jleDoneLabel);
                    break;
                case NodeType.GreaterThan:
                    NonLeafNode nlNode8 = (NonLeafNode)node;

                    CompileNode(nlNode8.LeftPtr, destination);

                    // Emit conditional jump to true
                    using (AllocatedRegister lessthanrightReg = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode8.RightPtr, lessthanrightReg);
                        CodeOutput.EmitCmp(destination.Access(), lessthanrightReg.Access());
                    }

                    string gtLabel = CodeOutput.ReserveNextLabel();
                    string gtDoneLabel = CodeOutput.ReserveNextLabel();

                    CodeOutput.EmitJl(gtLabel);

                    // Emit false case
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(1));
                    CodeOutput.EmitJmp(gtDoneLabel);

                    // Emit true case
                    CodeOutput.EmitLabel(gtLabel);
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(0));

                    CodeOutput.EmitLabel(gtDoneLabel);
                    break;
                case NodeType.GreaterThanEquals:
                    NonLeafNode nlNode9 = (NonLeafNode)node;

                    CompileNode(nlNode9.LeftPtr, destination);

                    // Emit conditional jump to true
                    using (AllocatedRegister lessthanrightReg = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode9.RightPtr, lessthanrightReg);
                        CodeOutput.EmitCmp(destination.Access(), lessthanrightReg.Access());
                    }

                    string gteLabel = CodeOutput.ReserveNextLabel();
                    string gteDoneLabel = CodeOutput.ReserveNextLabel();

                    CodeOutput.EmitJle(gteLabel);

                    // Emit false case
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(1));
                    CodeOutput.EmitJmp(gteDoneLabel);

                    // Emit true case
                    CodeOutput.EmitLabel(gteLabel);
                    CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(0));

                    CodeOutput.EmitLabel(gteDoneLabel);
                    break;
                case NodeType.Assignment:
                    NonLeafNode nlNode10 = (NonLeafNode)node;
                    Node leftNode = ((NonLeafNode)node).LeftPtr;

                    Operand varDestination = GetOperandForIdentifierAccess(leftNode);

                    using (AllocatedRegister assignRight = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode10.RightPtr, assignRight);
                        CodeOutput.EmitMov(varDestination, assignRight.Access());
                    }

                    break;
                case NodeType.Variable:
                    Operand varGetDestination = GetOperandForIdentifierAccess((VariableNode)node);

                    Operand destinationLoc = destination.Access();

                    if (destinationLoc.IsDereferenced)
                    {
                        using AllocatedRegister vintermediateReg = RegisterPool.Allocate();
                        CodeOutput.EmitMov(vintermediateReg.Access(), varGetDestination);
                        CodeOutput.EmitMov(destinationLoc, vintermediateReg.Access());
                    }
                    else CodeOutput.EmitMov(destinationLoc, varGetDestination);

                    break;
                case NodeType.Call:

                    CallNode callNode = (CallNode)node;
                    int paramSize = SizeOfVariables(callNode.Function.Parameters);

                    using (AllocatedRegister intermediateRegForStackDest = destination.IsOnStack() ? RegisterPool.Allocate() : null)
                    {
                        // Refactor please
                        if (!destination.IsCurrentlyRegister(OperandBase.Eax)) // Eax isn't a scratch register
                            CodeOutput.EmitPush(Operand.Eax);
                        if (!destination.IsCurrentlyRegister(OperandBase.Esi) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Esi) is false or null)
                            CodeOutput.EmitPush(Operand.Esi);
                        if (!destination.IsCurrentlyRegister(OperandBase.Edi) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edi) is false or null)
                            CodeOutput.EmitPush(Operand.Edi);
                        if (!destination.IsCurrentlyRegister(OperandBase.Ecx) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ecx) is false or null)
                            CodeOutput.EmitPush(Operand.Ecx);
                        if (!destination.IsCurrentlyRegister(OperandBase.Ebx) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ebx) is false or null)
                            CodeOutput.EmitPush(Operand.Ebx);
                        if (!destination.IsCurrentlyRegister(OperandBase.Edx) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edx) is false or null)
                            CodeOutput.EmitPush(Operand.Edx);

                        // Push parameters onto stack
                        for (int i = callNode.Args.Count - 1; i >= 0; i--)
                        {
                            CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(4)); // TODO: Use the actual size of the parameter
                            stackFrameSize += 4;
                            CompileNode(callNode.Args[i], new AllocatedMisc(Operand.ForDerefReg(OperandBase.Esp)));
                        }

                        // Edit the call
                        CodeOutput.EmitCall("F" + ((CallNode)node).Function.Name);
                        CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(paramSize));

                        // Move the result into the relevant place
                        if (intermediateRegForStackDest != null)
                            CodeOutput.EmitMov(intermediateRegForStackDest.Access(), Operand.Eax);
                        else
                            CodeOutput.EmitMov(destination.Access(), Operand.Eax);

                        if (!destination.IsCurrentlyRegister(OperandBase.Edx) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edx) is false or null)
                            CodeOutput.EmitPop(Operand.Edx);
                        if (!destination.IsCurrentlyRegister(OperandBase.Ebx) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ebx) is false or null)
                            CodeOutput.EmitPop(Operand.Ebx);
                        if (!destination.IsCurrentlyRegister(OperandBase.Ecx) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ecx) is false or null)
                            CodeOutput.EmitPop(Operand.Ecx);
                        if (!destination.IsCurrentlyRegister(OperandBase.Edi) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edi) is false or null)
                            CodeOutput.EmitPop(Operand.Edi);
                        if (!destination.IsCurrentlyRegister(OperandBase.Esi) && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Esi) is false or null)
                            CodeOutput.EmitPop(Operand.Esi);
                        if (!destination.IsCurrentlyRegister(OperandBase.Eax))
                            CodeOutput.EmitPop(Operand.Eax);

                        if (intermediateRegForStackDest != null)
                            CodeOutput.EmitMov(destination.Access(), intermediateRegForStackDest.Access());
                    }

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
                            CompileNode(nativeCallNode.Args[0], new AllocatedMisc(Operand.ForDerefReg(OperandBase.Esp)));
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

            CompilePrologue(size - paramsize);
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

            CompileEpilogue(size - paramsize);
            stackFrameSize -= size;

            _currentFunction = null;
        }

        private void CompileValueMove(GloryType type, Operand src, Operand dest)
        {

        }

        private int SizeOfVariables(List<Variable> vars)
        {
            int size = 0;
            for (int i = 0; i < vars.Count; i++)
                size += sizeOf(vars[i].Type);
            return size;
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
