﻿using System;
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
                        CompileNode(returnStatement.Expression, Operand.Eax);
                        CodeOutput.EmitJmp("EF" + _currentFunction.Name);
                        break;
                    case IfStatement ifStatement:
                        Operand conditionResult = RegisterPool.Allocate();
                        CompileNode(ifStatement.Condition, conditionResult);
                        CodeOutput.EmitCmp(conditionResult, Operand.ForLiteral(0));
                        RegisterPool.Free(conditionResult);
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
                        break;
                    case WhileStatement whileStatement:
                        string topLabel = CodeOutput.ReserveNextLabel();
                        string whiledoneLabel = CodeOutput.ReserveNextLabel();
                        CodeOutput.EmitLabel(topLabel);
                        Operand whileconditionResult = RegisterPool.Allocate();
                        CompileNode(whileStatement.Condition, whileconditionResult);
                        CodeOutput.EmitPush(Operand.ForLiteral(0));
                        CodeOutput.EmitCmp(whileconditionResult, Operand.ForDerefReg(OperandBase.Esp)); // this WILL break if we run out of registers
                        RegisterPool.Free(whileconditionResult);
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
                    Operand addRight = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, addRight);
                    CodeOutput.EmitAdd(destination, addRight);
                    RegisterPool.Free(addRight);
                    break;
                case NodeType.Minus:
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand minusRight = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, minusRight);
                    CodeOutput.EmitSub(destination, minusRight);
                    RegisterPool.Free(minusRight);
                    break;
                case NodeType.Multiply:
                    CompileNode(((NonLeafNode)node).LeftPtr, Operand.Eax);
                    Operand mulrightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, mulrightReg);
                    if (RegisterPool.IsRegisterAllocated(Operand.Edx))
                    {
                        CodeOutput.EmitPush(Operand.Edx);
                        CodeOutput.EmitMul(mulrightReg);
                        CodeOutput.EmitPop(Operand.Edx);
                    }
                    else
                        CodeOutput.EmitMul(mulrightReg);

                    RegisterPool.Free(mulrightReg);
                    if (destination != Operand.Eax)
                        CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.Divide:
                case NodeType.Div:
                    CompileNode(((NonLeafNode)node).LeftPtr, Operand.Eax);
                    Operand dividerightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, dividerightReg);
                    CodeOutput.EmitXor(Operand.Edx, Operand.Edx);
                    CodeOutput.EmitDiv(dividerightReg);
                    RegisterPool.Free(dividerightReg);
                    if (destination != Operand.Eax)
                        CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.Mod:
                    CompileNode(((NonLeafNode)node).LeftPtr, Operand.Eax);
                    Operand moddividerightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, moddividerightReg);
                    if (moddividerightReg == Operand.Edx)
                    {
                        CodeOutput.EmitPush(Operand.Edx);
                        RegisterPool.Free(moddividerightReg);
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
                        RegisterPool.Free(moddividerightReg);
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
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand doubleequalsrightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, doubleequalsrightReg);
                    CodeOutput.EmitXor(Operand.Eax, Operand.Eax);
                    CodeOutput.EmitCmp(destination, doubleequalsrightReg);
                    RegisterPool.Free(doubleequalsrightReg);
                    CodeOutput.EmitSete(Operand.Al);
                    CodeOutput.EmitMov(destination, Operand.Eax);
                    break;
                case NodeType.LessThan:
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand lessthanrightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, lessthanrightReg);
                    CodeOutput.EmitCmp(destination, lessthanrightReg);
                    RegisterPool.Free(lessthanrightReg);
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
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand lessthanequalsrightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, lessthanequalsrightReg);
                    CodeOutput.EmitCmp(destination, lessthanequalsrightReg);
                    RegisterPool.Free(lessthanequalsrightReg);
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
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand greaterthanrightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, greaterthanrightReg);
                    CodeOutput.EmitCmp(destination, greaterthanrightReg);
                    RegisterPool.Free(greaterthanrightReg);
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
                    CompileNode(((NonLeafNode)node).LeftPtr, destination);
                    Operand greaterthanequalsrightReg = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, greaterthanequalsrightReg);
                    CodeOutput.EmitCmp(destination, greaterthanequalsrightReg);
                    RegisterPool.Free(greaterthanequalsrightReg);
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

                    Operand assignRight = RegisterPool.Allocate();
                    CompileNode(((NonLeafNode)node).RightPtr, assignRight);

                    CodeOutput.EmitMov(varDestination, assignRight);
                    RegisterPool.Free(assignRight);
                    break;
                case NodeType.Variable:
                    Operand varGetDestination = GetOperandForIdentifierAccess((VariableNode)node);

                    if (destination.IsDereferenced)
                    {
                        Operand vintermediateReg = RegisterPool.Allocate();
                        CodeOutput.EmitMov(vintermediateReg, varGetDestination);
                        CodeOutput.EmitMov(destination, vintermediateReg);
                        RegisterPool.Free(vintermediateReg);
                    }
                    else CodeOutput.EmitMov(destination, varGetDestination);

                    break;
                case NodeType.Call:
                    CallNode callNode = (CallNode)node;
                    int paramSize = SizeOfVariables(callNode.Function.Parameters);
                    Operand intermediateReg = null;
                    if (destination.IsDereferenced && destination.OpBase is OperandBase.Esp or OperandBase.Ebp)
                        intermediateReg = RegisterPool.Allocate();
                    // Refactor please
                    if (destination != Operand.Eax) // Eax isn't a scratch register
                        CodeOutput.EmitPush(Operand.Eax);
                    if (destination != Operand.Esi && intermediateReg != Operand.Esi)
                        CodeOutput.EmitPush(Operand.Esi);
                    if (destination != Operand.Edi && intermediateReg != Operand.Edi)
                        CodeOutput.EmitPush(Operand.Edi);
                    if (destination != Operand.Ecx && intermediateReg != Operand.Ecx)
                        CodeOutput.EmitPush(Operand.Ecx);
                    if (destination != Operand.Ebx && intermediateReg != Operand.Ebx)
                        CodeOutput.EmitPush(Operand.Ebx);
                    if (destination != Operand.Edx && intermediateReg != Operand.Eax)
                        CodeOutput.EmitPush(Operand.Edx);

                    /*
                     * Edi
                       Esi
                       Ecx
                       Ebx
                       Edx

                     */

                    for (int i = callNode.Args.Count - 1; i >= 0; i--)
                    {
                        //Operand arg = ScratchRegisterPool.AllocateScratchRegister();
                        CodeOutput.EmitSub(Operand.Esp, Operand.ForLiteral(4)); // TODO: Use the actual size of the parameter
                        stackFrameSize += 4;
                        CompileNode(callNode.Args[i], Operand.ForDerefReg(OperandBase.Esp));
                        //CodeOutput.EmitPush(arg);
                        //ScratchRegisterPool.FreeScratchRegister(arg);
                    }

                    CodeOutput.EmitCall("F" + ((CallNode)node).Function.Name);
                    CodeOutput.EmitAdd(Operand.Esp, Operand.ForLiteral(paramSize));
                    if (intermediateReg != null)
                    {
                        CodeOutput.EmitMov(intermediateReg, Operand.Eax);
                    }
                    else
                        CodeOutput.EmitMov(destination, Operand.Eax);
                    if (destination != Operand.Edx && intermediateReg != Operand.Edx)
                        CodeOutput.EmitPop(Operand.Edx);
                    if (destination != Operand.Ebx && intermediateReg != Operand.Ebx)
                        CodeOutput.EmitPop(Operand.Ebx);
                    if (destination != Operand.Ecx && intermediateReg != Operand.Ecx)
                        CodeOutput.EmitPop(Operand.Ecx);
                    if (destination != Operand.Edi && intermediateReg != Operand.Edi)
                        CodeOutput.EmitPop(Operand.Edi);
                    if (destination != Operand.Esi && intermediateReg != Operand.Esi)
                        CodeOutput.EmitPop(Operand.Esi);
                    if (destination != Operand.Eax)
                        CodeOutput.EmitPop(Operand.Eax);
                    if (intermediateReg != null)
                        CodeOutput.EmitMov(destination, intermediateReg);



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