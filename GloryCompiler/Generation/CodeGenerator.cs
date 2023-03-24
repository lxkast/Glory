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
        int _currentFunctionParamSize;
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
                if (Parser.GlobalVariables[i].Type.Type == GloryTypes.Array)
                    CodeOutput.EmitDataArray("V" + Parser.GlobalVariables[i].Name, ((ArrayGloryType)Parser.GlobalVariables[i].Type)._size);
                else
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
                        if (_currentFunction.ReturnType.Type == GloryTypes.Array)
                        {
                            CompileNode(returnStatement.Expression, new AllocatedMisc(Operand.ForDerefReg(OperandBase.Ebp, 4 + _currentFunctionParamSize)));
                        }
                        else
                        {
                            using (AllocatedRegister reg = RegisterPool.AllocateEAX())
                                CompileNode(returnStatement.Expression, reg);
                        }
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

                    // If the destination is eax, just use that.
                    AllocatedRegister eaxReg;
                    if (destination.IsCurrentlyRegister(OperandBase.Eax))
                        eaxReg = (AllocatedRegister)destination;
                    else
                        eaxReg = RegisterPool.AllocateEAX();

                    CompileNode(nlNode4.LeftPtr, eaxReg);

                    using (AllocatedRegister divRight = RegisterPool.Allocate())
                    {
                        CompileNode(nlNode4.RightPtr, divRight);
                        CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                        CodeOutput.EmitDiv(divRight.Access());
                    }

                    // If the destination wasn't eax, move from our eax allocation into whatever the destination is and free our eax
                    if (!destination.IsCurrentlyRegister(OperandBase.Eax))
                    {
                        CodeOutput.EmitMov(destination.Access(), Operand.Eax);
                        RegisterPool.Free(eaxReg);
                    }

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
                case NodeType.Indexer:

                    IndexNode indexNode = (IndexNode)node;

                    using (AllocatedRegister indexDest = RegisterPool.Allocate())
                    {
                        // Compile the index
                        CompileNode(indexNode.Index, indexDest);

                        if (indexNode.Target.NodeType == NodeType.Variable)
                        {
                            Variable variable = ((VariableNode)indexNode.Target).Variable;

                            // For indexing a variable, access at the offset of that variable.
                            // For a local variable, perform one add for esp and a subtract for the offset
                            if (_currentFunction != null && _currentFunction.Vars.Contains(variable))
                            {
                                CodeOutput.EmitAdd(indexDest.Access(), Operand.Ebp);
                                CodeOutput.EmitSub(indexDest.Access(), Operand.ForLiteral(variable.Offset));
                            }
                            else
                                CodeOutput.EmitAdd(indexDest.Access(), Operand.ForLabel("V" + variable.Name));

                            if (destination.IsRegister())
                                CodeOutput.EmitMov(destination.Access(), indexDest.Access().CopyWithDerefSetTo(true));
                            else
                            {
                                using (AllocatedRegister intermediateReg12000 = RegisterPool.Allocate())
                                {
                                    CodeOutput.EmitMov(intermediateReg12000.Access(), indexDest.Access().CopyWithDerefSetTo(true));
                                    CodeOutput.EmitMov(destination.Access(), intermediateReg12000.Access());
                                }
                            }
                        }
                        //else if
                        //{
                        //    CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(sizeOf()));
                        //    CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral());
                        //}
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
                    CompileComparison(node, destination, label => CodeOutput.EmitJl(label));
                    break;
                case NodeType.LessThanEquals:
                    CompileComparison(node, destination, label => CodeOutput.EmitJle(label));
                    break;
                case NodeType.GreaterThan:
                    CompileComparison(node, destination, label => CodeOutput.EmitJg(label));
                    break;
                case NodeType.GreaterThanEquals:
                    CompileComparison(node, destination, label => CodeOutput.EmitJge(label));
                    break;
                case NodeType.Assignment:
                    NonLeafNode nlNode10 = (NonLeafNode)node;
                    Node leftNode = ((NonLeafNode)node).LeftPtr;

                    if (leftNode.NodeType == NodeType.Indexer)
                    {
                        IndexNode indexNode2 = ((IndexNode)leftNode);
                        Variable indexerTargetVariable = ((VariableNode)indexNode2.Target).Variable; // Assuming that the target will always be a variable as that's what the parser outputs.

                        using (AllocatedRegister indexReg = RegisterPool.Allocate())
                        {
                            // Compile the index
                            CompileNode(indexNode2.Index, indexReg);

                            // For indexing a variable, access at the offset of that variable.
                            // For a local variable, perform one add for esp and a subtract for the offset
                            if (_currentFunction != null && _currentFunction.Vars.Contains(indexerTargetVariable))
                            {
                                CodeOutput.EmitAdd(indexReg.Access(), Operand.Ebp);
                                CodeOutput.EmitSub(indexReg.Access(), Operand.ForLiteral(indexerTargetVariable.Offset));
                            }
                            else
                                CodeOutput.EmitAdd(indexReg.Access(), Operand.ForLabel("V" + indexerTargetVariable.Name));

                            CompileNode(nlNode10.RightPtr, new AllocatedDeref(indexReg));
                        }

                    }
                    // Variables
                    else
                    {
                        Operand varDestination = GetOperandForVariableAccess(leftNode, out GloryType avarType);
                        CompileNode(nlNode10.RightPtr, new AllocatedMisc(varDestination));
                    }

                    //using (AllocatedRegister assignRight = RegisterPool.Allocate())
                    //{
                    
                        //CodeOutput.EmitMov(varDestination, assignRight.Access());
                    //}

                    break;
                case NodeType.Variable:
                    Operand varGetDestination = GetOperandForVariableAccess((VariableNode)node, out GloryType vvarType);

                    if (vvarType.Type == GloryTypes.Array && destination.IsRegister()) throw new Exception("Cannot move array into register");

                    if (destination.IsRegister())
                        CodeOutput.EmitMov(destination.Access(), varGetDestination);
                    else
                    {
                        using AllocatedRegister vintermediateReg = RegisterPool.Allocate();

                        if (vvarType.Type == GloryTypes.Array)
                            CompileMoveArrayData(destination, vintermediateReg, vvarType);
                        else
                        {
                            CodeOutput.EmitMov(vintermediateReg.Access(), varGetDestination);
                            CodeOutput.EmitMov(destination.Access(), vintermediateReg.Access());
                        }
                    } 

                    break;
                case NodeType.Call:
                    CallNode callNode = (CallNode)node;
                    int paramSize = SizeOfVariables(callNode.Function.Parameters);
                    GloryType returnType = callNode.Function.ReturnType;

                    if (returnType?.Type == GloryTypes.Array && destination.IsRegister()) throw new Exception("Cannot move array into register");

                    using (AllocatedRegister intermediateRegForStackDest = destination?.IsRegister() is false or null ? null : RegisterPool.Allocate())
                    {
                        // Refactor please
                        if (!destination?.IsCurrentlyRegister(OperandBase.Eax) == true) // Eax isn't a scratch register
                            CodeOutput.EmitPush(Operand.Eax);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Esi) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Esi) is false or null)
                            CodeOutput.EmitPush(Operand.Esi);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Edi) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edi) is false or null)
                            CodeOutput.EmitPush(Operand.Edi);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Ecx) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ecx) is false or null)
                            CodeOutput.EmitPush(Operand.Ecx);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Ebx) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ebx) is false or null)
                            CodeOutput.EmitPush(Operand.Ebx);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Edx) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edx) is false or null)
                            CodeOutput.EmitPush(Operand.Edx);

                        //Return value
                        if (returnType?.Type == GloryTypes.Array)
                            CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(sizeOf(returnType)));

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
                        if (returnType != null)
                        {
                            if (returnType.Type == GloryTypes.Array)
                                CompileMoveArrayData(destination, intermediateRegForStackDest, returnType);
                            else if (intermediateRegForStackDest != null)
                                CodeOutput.EmitMov(intermediateRegForStackDest.Access(), Operand.Eax);
                            else
                                CodeOutput.EmitMov(destination.Access(), Operand.Eax);
                        }

                        if (returnType?.Type == GloryTypes.Array)
                            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(sizeOf(returnType)));

                        if (!destination?.IsCurrentlyRegister(OperandBase.Edx) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edx) is false or null)
                            CodeOutput.EmitPop(Operand.Edx);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Ebx) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ebx) is false or null)
                            CodeOutput.EmitPop(Operand.Ebx);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Ecx) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Ecx) is false or null)
                            CodeOutput.EmitPop(Operand.Ecx);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Edi) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Edi) is false or null)
                            CodeOutput.EmitPop(Operand.Edi);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Esi) == true && intermediateRegForStackDest?.IsCurrentlyRegister(OperandBase.Esi) is false or null)
                            CodeOutput.EmitPop(Operand.Esi);
                        if (!destination?.IsCurrentlyRegister(OperandBase.Eax) == true)
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

        private void CompileMoveArrayData(AllocatedSpace destination, AllocatedRegister intermediateReg, GloryType arrayType)
        {
            ArrayGloryType arrayTypeAsArray = (ArrayGloryType)arrayType;
            for (int i = 0; i < arrayTypeAsArray._size; i++)
            {
                Operand destOperand = destination.Access().CopyWithOffset(i * 4);

                if (arrayTypeAsArray.ItemType.Type == GloryTypes.Array)
                    CompileMoveArrayData(destination, intermediateReg, arrayTypeAsArray.ItemType);
                else
                {
                    CodeOutput.EmitMov(intermediateReg.Access(), Operand.ForDerefReg(OperandBase.Esp, i * 4));
                    CodeOutput.EmitMov(destOperand, intermediateReg.Access());
                }
                
            }
        }

        private void CompileComparison(Node node, AllocatedSpace destination, Action<string> emitJump)
        {
            NonLeafNode nlNode = (NonLeafNode)node;
            CompileNode(nlNode.LeftPtr, destination);

            // Emit conditional jump to true
            using (AllocatedRegister lessthanrightReg = RegisterPool.Allocate())
            {
                CompileNode(nlNode.RightPtr, lessthanrightReg);
                CodeOutput.EmitCmp(destination.Access(), lessthanrightReg.Access());
            }

            string jlLabel = CodeOutput.ReserveNextLabel();
            string jlDoneLabel = CodeOutput.ReserveNextLabel();

            emitJump(jlLabel);

            // Emit false case
            CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(0));
            CodeOutput.EmitJmp(jlDoneLabel);

            // Emit true case
            CodeOutput.EmitLabel(jlLabel);
            CodeOutput.EmitMov(destination.Access(), Operand.ForLiteral(1));

            CodeOutput.EmitLabel(jlDoneLabel);
        }

        private Operand GetOperandForVariableAccess(Node leftNode, out GloryType type)
        {
            Variable variable = ((VariableNode)leftNode).Variable;
            type = variable.Type;

            if (_currentFunction != null && _currentFunction.Vars.Contains(variable))
                return Operand.ForDerefReg(OperandBase.Ebp, -variable.Offset);
            else
                return Operand.ForDerefLabel("V" + variable.Name);
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
              
                       +---------------+
                       |ret array space|
                       +---------------+
                       |      b        |   <- argumemts are pushed onto the stack in reverse order
                       |---------------|
                       |      a        |
                       |---------------|
                       |return address |   <- the return address is automatically pushed when running a "call" instruction
                       |---------------|
                       |   old ebp     |   
                       |---------------|
                       |      c        |   <- local variables
                       +---------------+








                        
            */


            _currentFunction = function;

            int size = SizeOfVariablesAndAssignOffsets(function.Vars);
            _currentFunctionParamSize = SizeOfVariablesAndAssignOffsets(function.Parameters);
            CodeOutput.EmitLabel("F" + function.Name);

            CompilePrologue(size - _currentFunctionParamSize);
            stackFrameSize += size;

            for (int i = 0; i < function.Parameters.Count; i++)
            {
                function.Vars[i].Offset *= -1;
                function.Vars[i].Offset -= 4;
            }
            for (int i = function.Parameters.Count; i < function.Vars.Count; i++)
            {
                function.Vars[i].Offset -= _currentFunctionParamSize;
            }
            CompileStatements(function.Code);

            CompileEpilogue(size - _currentFunctionParamSize);
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
