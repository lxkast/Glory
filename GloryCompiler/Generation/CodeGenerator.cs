using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GloryCompiler.Representation;
using GloryCompiler.Syntax;

namespace GloryCompiler.Generation
{
    public class CodeGenerator
    {
        internal List<Statement> GlobalStatements;
        internal List<Variable> GlobalVariables;
        internal List<Function> GlobalFunctions;
        public CodeOutput CodeOutput;
        public Parser Parser;
        internal RegisterAllocator RegisterPool;
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
                    CodeOutput.EmitDataArray("V" + Parser.GlobalVariables[i].Name, sizeOf((ArrayGloryType)Parser.GlobalVariables[i].Type) / 4);
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

        void CompileStatements(List<Statement> statements)
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
                            CompileNode(returnStatement.Expression, new AllocatedMisc(Operand.ForDerefReg(OperandBase.Ebp, 8 + _currentFunctionParamSize))); // "this is why we don't hardcode numbers in" "what do you put instead of 4" "8" - Alex 31/3/2023 15:05
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

        void CompileNode(Node node, AllocatedSpace destination)
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

                    IndexerNode indexNode = (IndexerNode)node;

                    using (AllocatedRegister indexDest = RegisterPool.Allocate())
                    {
                        // Grab the target at the very base of our index (if we have a[x][y] this will get the "a")
                        (Node baseTarget, int noOfIndexes) = GetTargetAtBaseOfIndexChain(indexNode);

                        // Get the type of the base
                        GloryType type;
                        if (baseTarget.NodeType == NodeType.Variable)
                            type = ((VariableNode)baseTarget).Variable.Type;
                        else if (baseTarget.NodeType == NodeType.Call)
                            type = ((CallNode)baseTarget).Function.ReturnType;
                        else throw new Exception("Code generator does not support indexing this type of node.");

                        // Now, get it.
                        GloryType returnOfIndex;
                        if (baseTarget.NodeType == NodeType.Variable)
                        {
                            Variable v = ((VariableNode)baseTarget).Variable;

                            // Compile the actual index stuff
                            returnOfIndex = CompileIndexerAccessOffset(indexDest, indexNode, type, noOfIndexes);

                            // For indexing a variable, access at the offset of that variable.
                            // For a local variable, perform one add for esp and a subtract for the offset
                            if (_currentFunction != null && _currentFunction.EverySingleVar.Contains(v))
                            {
                                CodeOutput.EmitAdd(indexDest.Access(), Operand.Ebp);
                                CodeOutput.EmitSub(indexDest.Access(), Operand.ForLiteral(v.Offset));
                            }
                            else
                            {
                                CodeOutput.EmitAdd(indexDest.Access(), Operand.ForLabel("V" + v.Name));
                            }

                            // Verify that we can actually move the result of this index into the destination
                            if (returnOfIndex.Type == GloryTypes.Array && destination.IsRegister()) throw new Exception("Cannot move array into register");

                            // Do the move into destination. If our index returns an array, make sure we do a stack-based move on that!
                            if (returnOfIndex.Type == GloryTypes.Array)
                                using (AllocatedRegister intermediateReg19999 = RegisterPool.Allocate())
                                    CompileMoveArrayData(destination, intermediateReg19999, returnOfIndex, new AllocatedDeref(indexDest));
                            else if (!destination.IsDeref())
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
                        
                        // Function call
                        else
                        {
                            // Sanity check: It is a function call right?
                            if (baseTarget.NodeType != NodeType.Call) throw new Exception("Indexer only support indexing variables or function calls.");

                            // Compile a call with the indexing embedded into its return value handling
                            CallNode callNode = (CallNode)baseTarget;

                            using (AllocatedRegister intermediateReg = RegisterPool.Allocate())
                            {
                                bool isDestOnStack = destination.IsOnStack();

                                GloryType returnType = CompileCallStart(callNode, destination, intermediateReg);

                                // Compile the actual index stuff here for the return
                                returnOfIndex = CompileIndexerAccessOffset(indexDest, indexNode, type, noOfIndexes);

                                // Append the offset of the array in memory
                                CodeOutput.EmitAdd(indexDest.Access(), Operand.Esp);

                                // Now move the value at this position into the correct place
                                if (returnOfIndex.Type == GloryTypes.Array)
                                    CompileMoveArrayData(destination, intermediateReg, returnType, indexDest);
                                else if (isDestOnStack)
                                    CodeOutput.EmitMov(intermediateReg.Access(), indexDest.Access().CopyWithDerefSetTo(true));
                                else
                                    CodeOutput.EmitMov(destination.Access(), indexDest.Access().CopyWithDerefSetTo(true));

                                CompileCallEnd(destination, intermediateReg, returnType);

                                if (returnOfIndex.Type != GloryTypes.Array && isDestOnStack)
                                    CodeOutput.EmitMov(destination.Access(), intermediateReg.Access());
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
                        IndexerNode indexNode2 = (IndexerNode)leftNode;

                        // Grab the target variable (reaching through index nodes if necessary)
                        (Node currentIndexNode, int indexerDepth) = GetTargetAtBaseOfIndexChain(indexNode2);
                        Variable baseVariable = ((VariableNode)currentIndexNode).Variable; // We assume that the target at the end of it all will always be a variable because that's all the parser outputs for an assignment.

                        using (AllocatedRegister indexReg = RegisterPool.Allocate())
                        {
                            // Compile the index access into the indexReg given.
                            CompileIndexerAccessOffset(indexReg, indexNode2, baseVariable.Type, indexerDepth);

                            // For indexing a variable, access at the offset of that variable.
                            // For a local variable, perform one add for esp and a subtract for the offset
                            if (_currentFunction != null && _currentFunction.EverySingleVar.Contains(baseVariable))
                            {
                                CodeOutput.EmitAdd(indexReg.Access(), Operand.Ebp);
                                CodeOutput.EmitSub(indexReg.Access(), Operand.ForLiteral(baseVariable.Offset));
                            }
                            else
                                CodeOutput.EmitAdd(indexReg.Access(), Operand.ForLabel("V" + baseVariable.Name));

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

                    if (!destination.IsDeref())
                        CodeOutput.EmitMov(destination.Access(), varGetDestination);
                    else
                    {
                        using AllocatedRegister vintermediateReg = RegisterPool.Allocate();

                        if (vvarType.Type == GloryTypes.Array)
                            CompileMoveArrayData(destination, vintermediateReg, vvarType, new AllocatedMisc(varGetDestination));
                        else
                        {
                            CodeOutput.EmitMov(vintermediateReg.Access(), varGetDestination);
                            CodeOutput.EmitMov(destination.Access(), vintermediateReg.Access());
                        }
                    } 

                    break;
                case NodeType.Call:
                    CompileCall(node, destination);

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

        private void CompileCall(Node node, AllocatedSpace destination)
        {
            CallNode callNode = (CallNode)node;

            using (AllocatedRegister intermediateRegForStackDest = destination?.IsRegister() is true or null ? null : RegisterPool.Allocate())
            {
                GloryType returnType = CompileCallStart(callNode, destination, intermediateRegForStackDest);

                // Move the result into the relevant place
                if (returnType != null && destination != null)
                {
                    if (returnType.Type == GloryTypes.Array)
                        CompileMoveArrayData(destination, intermediateRegForStackDest, returnType, new AllocatedMisc(Operand.Esp));
                    else if (intermediateRegForStackDest != null)
                        CodeOutput.EmitMov(intermediateRegForStackDest.Access(), Operand.Eax);
                    else
                        CodeOutput.EmitMov(destination.Access(), Operand.Eax);
                }

                CompileCallEnd(destination, intermediateRegForStackDest, returnType);

                if (returnType?.Type != GloryTypes.Array && intermediateRegForStackDest != null)
                    CodeOutput.EmitMov(destination.Access(), intermediateRegForStackDest.Access());
            }
        }

        private GloryType CompileCallStart(CallNode node, AllocatedSpace destination, AllocatedRegister intermediateRegForStackDest)
        {
            int paramSize = SizeOfVariables(node.Function.Parameters);
            GloryType returnType = node.Function.ReturnType;

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

            // Make room for the return value
            if (returnType?.Type == GloryTypes.Array)
                CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(sizeOf(returnType)));

            // Push parameters onto stack
            for (int i = node.Args.Count - 1; i >= 0; i--)
            {
                CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(sizeOf(node.Function.Parameters[i].Type))); // TODO: Use the actual size of the parameter
                stackFrameSize += sizeOf(node.Function.Parameters[i].Type);
                CompileNode(node.Args[i], new AllocatedMisc(Operand.ForDerefReg(OperandBase.Esp)));
            }

            // Emit the call
            CodeOutput.EmitCall("F" + node.Function.Name);
            CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(paramSize));
            return returnType;
        }

        private void CompileCallEnd(AllocatedSpace destination, AllocatedRegister intermediateRegForStackDest, GloryType returnType)
        {
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
        }

        private static (Node b, int indexerDepth) GetTargetAtBaseOfIndexChain(IndexerNode root)
        {
            IndexerNode currentIndexNode = root;
            int indexerDepth = 1;
            while (currentIndexNode.Target.NodeType == NodeType.Indexer)
            {
                currentIndexNode = (IndexerNode)currentIndexNode.Target;
                indexerDepth++;
            }

            return (currentIndexNode.Target, indexerDepth);
        }

        // Returns the type of object this depth will give you.
        private GloryType CompileIndexerAccessOffset(AllocatedRegister indexReg, IndexerNode currentNode, GloryType topArrayType, int depth)
        {
            bool isDeepest = false;

            // If we have an indexer on the left of this one, compile that first.
            if (currentNode.Target.NodeType == NodeType.Indexer)
                CompileIndexerAccessOffset(indexReg, (IndexerNode)currentNode.Target, topArrayType, depth - 1);
            else
                isDeepest = true;

            // Get the type at the current depth.
            GloryType type = topArrayType;
            for (int i = 0; i < depth; i++)
                type = ((ArrayGloryType)type).ItemType;

            // If we're on the deepest (and therefore the first to be generated) index access, move directly into our index register instead of having a middle one that adds.
            
            if (isDeepest)
            {
                //bool isMovingUp = currentNode.Target is VariableNode varNode ? varNode.Variable.Offset < 0 : true;

                CompileNode(currentNode.Index, indexReg);
                CodeOutput.EmitImul(indexReg.Access(), Operand.ForLiteral(sizeOf(type)));
            }
            else
                using (AllocatedRegister targetIndexReg = RegisterPool.Allocate())
                {
                    CompileNode(currentNode.Index, targetIndexReg);
                    CodeOutput.EmitImul(targetIndexReg.Access(), Operand.ForLiteral(sizeOf(type)));

                    // Add it to our main one
                    CodeOutput.EmitAdd(indexReg.Access(), targetIndexReg.Access());
                }

            return type;
        }

        private void CompileMoveArrayData(AllocatedSpace destination, AllocatedRegister intermediateReg, GloryType arrayType, AllocatedSpace source, int baseOffset = 0)
        {
            ArrayGloryType arrayTypeAsArray = (ArrayGloryType)arrayType;
            for (int i = 0; i < arrayTypeAsArray._size; i++)
            {
                Operand destOperand = destination.Access().CopyWithOffset(baseOffset + i * sizeOf(arrayTypeAsArray.ItemType));

                if (arrayTypeAsArray.ItemType.Type == GloryTypes.Array)
                    CompileMoveArrayData(destination, intermediateReg, arrayTypeAsArray.ItemType, source, baseOffset + i * sizeOf(arrayTypeAsArray.ItemType));
                else
                {
                    
                    CodeOutput.EmitMov(intermediateReg.Access(), source.Access().CopyWithOffset(baseOffset + i * sizeOf(arrayTypeAsArray.ItemType)).CopyWithDerefSetTo(true));
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

            if (_currentFunction != null && _currentFunction.EverySingleVar.Contains(variable))
                return Operand.ForDerefReg(OperandBase.Ebp, -variable.Offset);
            else
                return Operand.ForDerefLabel("V" + variable.Name);
        }

        void CompileFunction(Function function)
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
                       |---------------|
                       |      b        |   
                       |---------------|
                       |      a        |   
                       |---------------|
                       |return address |   <- ebp+4
                       |---------------|
                       |   old ebp     |   <- ebp
                       |---------------|
                       |      c        |   <- ebp-4
                       +---------------+








                        
            */


            _currentFunction = function;

            // Assign all the local variables (ignoring parameters)
            int size = SizeOfVariablesAndAssignOffsets(function.EverySingleVar.Where(v => !function.Parameters.Contains(v)).ToList());
            _currentFunctionParamSize = SizeOfParametersAndAssignOffsets(function.Parameters);

            CodeOutput.EmitLabel("F" + function.Name);

            CompilePrologue(size);
            stackFrameSize += size;

            for (int i = 0; i < function.Parameters.Count; i++)
                function.EverySingleVar[i].Offset *= -1;

            CompileStatements(function.Code);

            CompileEpilogue(size);
            stackFrameSize -= size;

            _currentFunction = null;
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
            vars[0].Offset = size + 4 - 4; // "+ 4" jumps over old ebp, "- 4" makes sure we're pointing at the "last int" of the thing
            for (int i = 1; i < vars.Count; i++)
            {
                size += sizeOf(vars[i].Type);
                vars[i].Offset = size + 4 - 4;
            }
            return size;
        }

        private int SizeOfParametersAndAssignOffsets(List<Variable> vars)
        {
            if (vars.Count == 0) return 0;
            vars[0].Offset = 8; // "+ 8" jumps over the return address/old ebp
            int size = sizeOf(vars[0].Type);
            for (int i = 1; i < vars.Count; i++)
            {
                vars[i].Offset = size + 8;
                size += sizeOf(vars[i].Type);
            }
            return size;
        }

        int sizeOf(GloryType type)
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
