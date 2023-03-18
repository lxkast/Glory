using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Parser
    {
        int _currentIndex;
        List<Token> _tokens;
        public List<Statement> GlobalStatements;
        public List<Variable> GlobalVariables;
        public List<Function> _GlobalFunctions;
        List<Variable> _currentVariables;
        private Function _currentFunction;

        BlockStatement _currentBlock;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _currentIndex = 0;
            GlobalStatements = new List<Statement>();
            GlobalVariables = new List<Variable>();
            _currentVariables = new List<Variable>();
            _GlobalFunctions = new List<Function>();
            ParseOuterStatements();
        }

        private Token ReadToken()
        {
            if (_currentIndex < _tokens.Count)
                return _tokens[_currentIndex];
            else
                return new Token(TokenType.Null);
        }

        private Token PeekToken(int amount)
        {
            if (_currentIndex + amount < _tokens.Count)
                return _tokens[_currentIndex + amount];
            else
                return new Token(TokenType.Null);
        }

        private void AddStatementToList(Statement statement)
        {
            if (_currentBlock == null)
                GlobalStatements.Add(statement);
            else
            {
                _currentBlock.Code.Add(statement);
            }
        }

        private void AddVariableToList(Variable variable)
        {
            if (_currentBlock == null)
                GlobalVariables.Add(variable);
            else
                _currentBlock.Vars.Add(variable);

            _currentVariables.Add(variable);
        }


        public void ParseOuterStatements()
        {
            bool isStatement = true;
            while (ReadToken().Type != TokenType.Null && isStatement)
            {
                isStatement = ParseOuterStatement();
            }
        }
        public bool ParseOuterStatement()
        {
            if (ReadToken().Type is TokenType.IntType or TokenType.StringType or TokenType.BoolType or TokenType.Blank)
            {
                GloryType type;
                if (ReadToken().Type == TokenType.Blank)
                {
                    _currentIndex++;
                    ParseFunction(null); // If we saw blank it's definitely a function
                    return true;
                }
                else
                {
                    type = ParseType();

                    if (ReadToken().Type == TokenType.Identifier)
                    {
                        if (PeekToken(1).Type == TokenType.OpenBracket)
                            ParseFunction(type);
                        else
                        {
                            ParseVariable(type);

                            if (ReadToken().Type != TokenType.Semicolon) throw new Exception("Expected semicolon");
                            _currentIndex++;
                        }

                        return true;
                    }
                    else throw new Exception("Expected identifier");
                }
            }
            return ParseStatement();
        }

        public void ParseStatements()
        {
            bool isStatement = true;
            while (ReadToken().Type != TokenType.Null && isStatement)
            {
                isStatement = ParseStatement();
            }
        }

        public bool ParseStatement()
        {
            // Variables (type identifier)
            if (ReadToken().Type is TokenType.IntType or TokenType.StringType or TokenType.FloatType or TokenType.BoolType)
            {
                GloryType type = ParseType();
                ParseVariable(type);
                if (ReadToken().Type != TokenType.Semicolon) throw new Exception("Expected semicolon");
                _currentIndex++;
            }

            // Assignments (a = ...)
            else if (ReadToken().Type == TokenType.Identifier)
            {
                if (PeekToken(1).Type == TokenType.OpenBracket)
                {
                    Node node = ParseCall();
                    SingleLineStatement call = new SingleLineStatement(node);
                    AddStatementToList(call);
                }
                else
                {
                    ParseAssignment();
                }
                if (ReadToken().Type != TokenType.Semicolon) throw new Exception("Expected semicolon");
                _currentIndex++;
            }

            // While (while ...)
            else if (ReadToken().Type == TokenType.While)
                ParseWhile();

            // If (if ...)
            else if (ReadToken().Type == TokenType.If)
                ParseIf();

            else if (ReadToken().Type == TokenType.Return)
            {
                ParseReturn();
                if (ReadToken().Type != TokenType.Semicolon) throw new Exception("Expected semicolon");
                _currentIndex++;
            }

            else
                return false; // Return false if we had no idea what to do with this

            return true; // Return true if we were happy with what we parsed
        }

        public void ParseReturn()
        {
            _currentIndex++;

            // Read expression to return
            Node returnExpression = ParseExpression();

            // Ensure return is valid
            if (_currentFunction == null) throw new Exception("Cannot return outside of a function");
            if (_currentFunction.ReturnType == null) throw new Exception("Cannot return from a blank function");

            if (VerifyAndGetTypeOf(returnExpression) == _currentFunction.ReturnType)
            {
                ReturnStatement statement = new ReturnStatement(returnExpression);
                AddStatementToList(statement);
            }
            else
            {
                throw new Exception("Incorrect return type");
            }
        }

        public void ParseFunction(GloryType returnType)
        {
            string name = ((IdentifierLiteralToken)ReadToken()).Val;
            _currentIndex += 2;
            List<Variable> parameters = new List<Variable>();

            // Make sure there's no functions with this name already.
            if (TryFindFunction(name) != null) throw new Exception("Function name already exists");

            // Read arguments
            while (ReadToken().Type != TokenType.CloseBracket)
            {
                if (ReadToken().Type is not TokenType.IntType and not TokenType.StringType and not TokenType.FloatType)
                    throw new Exception("Expected type");

                // Parse argument type
                GloryType paramType = ParseType();

                // Ensure identifier
                if (ReadToken().Type != TokenType.Identifier) throw new Exception("Expected identifier");

                // Handle argument name
                string paramName = ((IdentifierLiteralToken)ReadToken()).Val;
                _currentIndex++;
                parameters.Add(new Variable(paramType, paramName));

                // Handle comma or argument list end
                if (ReadToken().Type == TokenType.CloseBracket) break;

                if (ReadToken().Type == TokenType.Comma)
                    _currentIndex++;
                else
                    throw new Exception("Expected comma");
            }

            _currentIndex++; // Eat the closing bracket
            _currentVariables.AddRange(parameters);

            Function func = new Function(parameters, name, returnType);

            if (ReadToken().Type == TokenType.OpenCurly)
            {
                _currentIndex++;

                // Add function to list
                _GlobalFunctions.Add(func);
                _currentFunction = func;

                // Parse the function body
                BlockStatement previousBlock = EnterBlock(func);
                ParseStatements();
                ExitBlock(previousBlock);

                _currentFunction = null;

                // Ensure } is present
                if (ReadToken().Type == TokenType.CloseCurly)
                    _currentIndex++;
                else
                    throw new Exception("Expected }");
            }
            else throw new Exception("Expected {");

            // Verify all code paths return if return type is present
            if (returnType != null)
            {
                if (!VerifyReturn(func.Code))
                    throw new Exception("Not all code paths return a value");
            }
        }

        public GloryType ParseType()
        {
            TokenType type = ReadToken().Type;
            _currentIndex++;
            GloryType currentType = new(type);

            // Handle arrays + lists if "[" is present
            while (ReadToken().Type == TokenType.OpenSquare)
            {
                _currentIndex++;

                // Handle arrays
                if (ReadToken().Type == TokenType.NumberLiteral)
                {
                    NumberLiteralToken numberLiteral = (NumberLiteralToken)ReadToken();
                    _currentIndex++;
                    if (ReadToken().Type == TokenType.CloseSquare)
                    {
                        _currentIndex++;
                        currentType = new ArrayGloryType(currentType, numberLiteral.Val);
                    }
                    else throw new Exception("Expected [");
                }

                // Handle lists
                else if (ReadToken().Type == TokenType.CloseSquare)
                {
                    currentType = new ListGloryType(currentType);
                    _currentIndex++;
                }

                // Invalid syntax
                else throw new Exception("Expected constant number as array size");
            }

            return currentType;
        }

        public void ParseVariable(GloryType type)
        {
            if (ReadToken().Type == TokenType.Identifier)
            {
                string name = ((IdentifierLiteralToken)ReadToken()).Val;
                _currentIndex++;

                // Make sure there's no variables with this name already.
                if (TryFindIdentifier(name) != null) throw new Exception("Variable name already exists");

                // Create the variable
                Variable variable = new Variable(type, name);
                VariableNode node = new VariableNode(variable);
                AddVariableToList(variable);

                // If necessary, parse the "=" part.
                if (ReadToken().Type == TokenType.Equals)
                {
                    _currentIndex++;

                    // Create the tree
                    Node right = ParseExpression();
                    Node equals = new NonLeafNode(NodeType.Assignment, node, right);
                    VerifyAndGetTypeOf(equals);

                    // Insert a statement to perform the assignment.
                    SingleLineStatement statement = new SingleLineStatement(equals);
                    AddStatementToList(statement);
                }
            }
        }

        public void ParseAssignment()
        {
            string val = ((IdentifierLiteralToken)ReadToken()).Val;
            _currentIndex++;

            if (ReadToken().Type == TokenType.Equals)
            {
                _currentIndex++;

                // Get the variable
                Variable variable = FindIdentifier(val);
                VariableNode varNode = new VariableNode(variable);

                // Parse the right and create the node
                Node right = ParseExpression();
                Node equals = new NonLeafNode(NodeType.Assignment, varNode, right);

                // Type-check the node
                VerifyAndGetTypeOf(equals);

                // Create the statement
                SingleLineStatement statement = new SingleLineStatement(equals);
                AddStatementToList(statement);
            }
            else if (ReadToken().Type is TokenType.Plus or TokenType.Minus or TokenType.Times or TokenType.Divide)
            {
                Token operationToken = ReadToken();
                NodeType operationNodeType = operationToken.Type switch
                {
                    TokenType.Plus => NodeType.Plus,
                    TokenType.Minus => NodeType.Minus,
                    TokenType.Times => NodeType.Multiply,
                    TokenType.Divide => NodeType.Divide,
                    _ => NodeType.Null
                };
                
                _currentIndex++;

                // Expect equals
                if (ReadToken().Type != TokenType.Equals) throw new Exception("Expected equals");

                _currentIndex++;

                // Get the variable
                Variable variable = FindIdentifier(val);
                VariableNode varNode = new VariableNode(variable);

                // Parse the right and create the node
                Node right = ParseExpression();
                Node operation = new NonLeafNode(operationNodeType, varNode, right);
                Node equals = new NonLeafNode(NodeType.Assignment, varNode, operation);
                VerifyAndGetTypeOf(equals);

                // Create the statement
                SingleLineStatement statement = new SingleLineStatement(equals);
                AddStatementToList(statement);
            }
            else throw new Exception("Expected equals");
        }

        public void ParseWhile()
        {
            _currentIndex++;

            // Parse the condition
            Node condition = ParseExpression();
            if (VerifyAndGetTypeOf(condition).Type != GloryTypes.Bool)
                throw new Exception("Expected expression of type 'bool' for while statement.");

            // Look for the {
            if (ReadToken().Type != TokenType.OpenCurly) throw new Exception("Expected {");
            _currentIndex++;

            // Create the while
            WhileStatement newLoop = new WhileStatement();
            newLoop.Condition = condition;

            // Parse the body
            BlockStatement previousBlock = EnterBlock(newLoop);
            ParseStatements();
            ExitBlock(previousBlock);
            AddStatementToList(newLoop);

            // Look for the }
            if (ReadToken().Type == TokenType.CloseCurly)
                _currentIndex++;
            else
                throw new Exception("Expected }");
        }

        public void ParseIf()
        {
            _currentIndex++; // Eat the "if"

            // Parse the condition
            Node condition = ParseExpression();
            if (VerifyAndGetTypeOf(condition).Type != GloryTypes.Bool) 
                throw new Exception("Expected expression of type 'bool' for if condition.");

            // Look for the {
            if (ReadToken().Type != TokenType.OpenCurly) throw new Exception("Expected {");
            _currentIndex++;

            // Create the if
            IfStatement newIf = new IfStatement();
            newIf.Condition = condition;

            // Process the body
            BlockStatement previousBlock = EnterBlock(newIf);
            ParseStatements();
            ExitBlock(previousBlock);
            AddStatementToList(newIf);

            // Look for the }
            if (ReadToken().Type == TokenType.CloseCurly)
                _currentIndex++;
            else
                throw new Exception("Expected }");

            // Handle elses
            if (ReadToken().Type == TokenType.ElseIf)     
                newIf.Else = ParseElseIf();
            else if (ReadToken().Type == TokenType.Else)
                newIf.Else = ParseElse();
        }

        private ElseStatement ParseElseIf()
        {
            // For elseif, we'll literally just act like we saw an "if", but put that "if" inside an "ElseStatement" here.
            ElseStatement newElse = new ElseStatement();
            BlockStatement previousBlock = EnterBlock(newElse);
            ParseIf();
            ExitBlock(previousBlock);

            return newElse;
        }

        private ElseStatement ParseElse()
        {
            _currentIndex++; // Eat the "else"

            // Look for the {
            if (ReadToken().Type != TokenType.OpenCurly) throw new Exception("Expected {");
            _currentIndex++;

            // Create the else            
            ElseStatement newElse = new ElseStatement();

            // Process the body
            BlockStatement previousBlock = EnterBlock(newElse);
            ParseStatements();
            ExitBlock(previousBlock);

            // Look for the }
            if (ReadToken().Type == TokenType.CloseCurly)
                _currentIndex++;
            else
                throw new Exception("Expected }");

            return newElse;
        }

        public Node ParseExpression()
        {
            Node currentTree = ParseCompare();
            return currentTree;
        }
        
        public Node ParseCompare()
        {
            Node currentTree = ParseAdditive();

            while (ReadToken().Type is TokenType.DoubleEquals or TokenType.GreaterThan or TokenType.GreaterThanEquals or TokenType.LessThan or TokenType.LessThanEquals)
            {
                TokenType currentTokenType = ReadToken().Type;
                _currentIndex++;

                Node nextTerm = ParseAdditive();

                // Choose the right NodeType.
                NodeType correctNodeType = currentTokenType switch
                {
                    TokenType.DoubleEquals => NodeType.DoubleEquals,
                    TokenType.GreaterThanEquals => NodeType.GreaterThanEquals,
                    TokenType.GreaterThan => NodeType.GreaterThan,
                    TokenType.LessThan => NodeType.LessThan,
                    TokenType.LessThanEquals => NodeType.LessThanEquals,
                    _ => throw new Exception("Invalid token type")
                };

                currentTree = new NonLeafNode(correctNodeType, currentTree, nextTerm);
            }

            return currentTree;
        }

        public Node ParseAdditive()
        {
            Node currentTree = ParseDivide();

            while (ReadToken().Type is TokenType.Plus or TokenType.Minus)
            {
                if (ReadToken().Type == TokenType.Plus)
                {
                    _currentIndex++;
                    Node nextTerm = ParseDivide();
                    currentTree = new NonLeafNode(NodeType.Plus, currentTree, nextTerm);
                }
                else
                {
                    _currentIndex++;
                    Node nextTerm = ParseDivide();  
                    currentTree = new NonLeafNode(NodeType.Minus, currentTree, nextTerm);
                }
            }

            return currentTree;
        }

        public Node ParseDivide()
        {
            Node currentTree = ParseMultiply();

            while (ReadToken().Type is TokenType.Divide or TokenType.Div or TokenType.Mod)
            {
                NodeType treeType = ReadToken().Type switch
                {
                    TokenType.Divide => NodeType.Divide,
                    TokenType.Div => NodeType.Div,
                    TokenType.Mod => NodeType.Mod,
                    _ => throw new Exception("Unrecognised token type")
                };

                _currentIndex++;
                Node nextTerm = ParseMultiply();
                currentTree = new NonLeafNode(treeType, currentTree, nextTerm);
            }

            return currentTree;
        }

        public Node ParseMultiply()
        {
            Node currentTree = ParseIndex();

            while (ReadToken().Type == TokenType.Times)
            {
                _currentIndex++;
                Node nextTerm = ParseIndex();
                currentTree = new NonLeafNode(NodeType.Multiply, currentTree, nextTerm);
            }

            return currentTree;
        }

        public Node ParseIndex()
        {
            Node currentTree = ParseNegate();

            while (ReadToken().Type == TokenType.Index)
            {
                _currentIndex++;
                Node nextTerm = ParseNegate();
                currentTree = new NonLeafNode(NodeType.Index, currentTree, nextTerm);
            }

            return currentTree;
        }

        public Node ParseNegate()
        {
            int negateCount = 0;

            while (ReadToken().Type == TokenType.Minus)
            {
                negateCount++;
                _currentIndex++;
            }

            Node currentTree = ParseCall();
            if (negateCount % 2 == 1)
                currentTree = new NonLeafNode(NodeType.Multiply, currentTree, new IntNode(-1));

            return currentTree;
        }

        public Node ParseCall()
        {
            if (ReadToken().Type == TokenType.Identifier && PeekToken(1).Type == TokenType.OpenBracket)
            {
                string name = ((IdentifierLiteralToken)ReadToken()).Val;

                // Find the function
                Function func = TryFindFunction(name);
                NativeFunction nativeFunc = null;
                if (func == null)
                {
                    // Check if in NativeFunctions
                    nativeFunc = TryFindNativeFunction(name);
                    if (nativeFunc == null)
                        throw new Exception("Cannot find function with name " + name);
                }

                _currentIndex += 2;

                // Handle arguments
                List<Node> arguments = new List<Node>();
                while (ReadToken().Type != TokenType.CloseBracket)
                {
                    arguments.Add(ParseExpression());
                    
                    if (ReadToken().Type == TokenType.CloseBracket) break;

                    if (ReadToken().Type == TokenType.Comma)
                        _currentIndex++;
                    else
                        throw new Exception("Expected comma");
                }

                _currentIndex++;

                // For native functions
                if (func == null)
                {
                    if (arguments.Count != nativeFunc.Parameters.Count) 
                        throw new Exception("Function " + name + " does not accept " + arguments.Count + " arguments.");

                    for (int i = 0; i < arguments.Count; i++)
                    {
                        GloryType argumentType = VerifyAndGetTypeOf(arguments[i]);

                        if (argumentType != nativeFunc.Parameters[i].Type)
                            throw new Exception("Call to " + name + "() has incorrect argument types.");
                    }

                    return new NativeCallNode(nativeFunc, arguments);
                }

                // For regular functions
                else
                {
                    if (arguments.Count != func.Parameters.Count)
                        throw new Exception("Function " + name + " does not accept " + arguments.Count + " arguments.");

                    for (int i = 0; i < arguments.Count; i++)
                    {
                        GloryType argumentType = VerifyAndGetTypeOf(arguments[i]);

                        if (argumentType != func.Parameters[i].Type)
                            throw new Exception("Call to " + name + "() has incorrect argument types.");
                    }

                    return new CallNode(func, arguments);
                }
            }
            else
            {
                return ParseIndexer();
            }
        }
        public Node ParseIndexer()
        {
            Node currentTree = ParseUnary();

            while (ReadToken().Type == TokenType.OpenSquare)
            {
                // Check base of indexing is valid
                if (VerifyAndGetTypeOf(currentTree).Type is not GloryTypes.Array and not GloryTypes.String and not GloryTypes.List)
                    throw new Exception("Can only index arrays, lists or strings");

                _currentIndex++;

                // Parse index position
                Node index = ParseExpression();
                if (VerifyAndGetTypeOf(index).Type != GloryTypes.Int)
                    throw new Exception("Array index must be an integer");

                // Create node
                currentTree = new IndexNode(currentTree, index);

                // Handle "]"
                if (ReadToken().Type != TokenType.CloseSquare)
                    throw new Exception("Expected ]");
                _currentIndex++;
            }

            return currentTree;
        }

        public Node ParseUnary()
        {
            switch (ReadToken().Type)
            {
                // Expression
                case TokenType.OpenBracket:
                    _currentIndex++;
                    Node result = ParseExpression();

                    if (ReadToken().Type == TokenType.CloseBracket)
                        _currentIndex++;
                    else
                        throw new Exception("Expected closing bracket");

                    return result;

                // Number Literal
                case TokenType.NumberLiteral:
                    int numVal = ((NumberLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new IntNode(numVal);

                // String Literal
                case TokenType.StringLiteral:
                    string strVal = ((StringLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new StringNode(strVal);

                // bool Literal
                case TokenType.BoolLiteral:
                    bool blVal = ((BoolLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new BoolNode(blVal);

                // Identifier
                case TokenType.Identifier:
                    string val = ((IdentifierLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new VariableNode(FindIdentifier(val));

                default:
                    throw new Exception("Syntax Error");
            }
        }

        private Variable FindIdentifier(string name)
        {
            Variable result = TryFindIdentifier(name);
            if (result == null) throw new Exception("Cannot find variable with name " + name);
            return result;
        }

        private Variable TryFindIdentifier(string name)
        {
            for (int i = 0; i < _currentVariables.Count(); i++)
            {
                if (_currentVariables[i].Name == name)
                    return _currentVariables[i];
            }
            return null;
        }

        private Function TryFindFunction(string name)
        {
            for (int i = 0; i < _GlobalFunctions.Count(); i++)
            {
                if (_GlobalFunctions[i].Name == name)
                    return _GlobalFunctions[i];
            }
            return null;
        }

        private NativeFunction TryFindNativeFunction(string name)
        {
            NativeFunctions nativeFunctinons = new NativeFunctions();
            for (int i = 0; i <  nativeFunctinons.nativeFunctions.Count(); i++)
            {
                if (nativeFunctinons.nativeFunctions[i].Name == name)
                    return nativeFunctinons.nativeFunctions[i];
            }
            return null;
        }

        private GloryType VerifyAndGetTypeOf(Node node)
        {
            switch (node.NodeType)
            {
                case NodeType.Null:
                    throw new Exception("Cannot verify type of null");

                case NodeType.NumberLiteral:
                case NodeType.StringLiteral:
                case NodeType.BoolLiteral:
                    return new GloryType(node.NodeType switch
                    {
                        NodeType.NumberLiteral => GloryTypes.Int,
                        NodeType.StringLiteral => GloryTypes.String,
                        NodeType.BoolLiteral => GloryTypes.Bool,
                        _ => throw new Exception("Unrecognised type")
                    });

                case NodeType.Variable:
                    return ((VariableNode)node).Variable.Type;
                case NodeType.DoubleEquals:
                    return VerifyTypeOfDoubleEquals(node);

                case NodeType.GreaterThan:
                case NodeType.GreaterThanEquals:
                case NodeType.LessThan:
                case NodeType.LessThanEquals:
                    return VerifyTypeOfIntComparisonOperators(node);
                case NodeType.Call:
                    CallNode newNode = (CallNode)node;
                    if (newNode.Function.ReturnType == null) throw new Exception("Cannot use return value of Blank function");
                    return newNode.Function.ReturnType;
                case NodeType.Indexer:
                    IndexNode newwNode = (IndexNode)node;
                    GloryType targetType = VerifyAndGetTypeOf(newwNode.Target);
                    if (targetType.Type == GloryTypes.Array)
                        return ((ArrayGloryType)VerifyAndGetTypeOf(newwNode.Target)).ItemType;
                    else if (targetType.Type == GloryTypes.String)
                        return new GloryType(GloryTypes.String);
                    else if (targetType.Type == GloryTypes.List)
                        return ((ListGloryType)VerifyAndGetTypeOf(newwNode.Target)).ItemType;
                    else
                        throw new Exception("Uh oh...");

                default:
                    NonLeafNode nonLeafNode = (NonLeafNode)node;
                    GloryType leftPtrType = VerifyAndGetTypeOf(nonLeafNode.LeftPtr);
                    GloryType rightPtrType = VerifyAndGetTypeOf(nonLeafNode.RightPtr);

                    if (leftPtrType.Type == rightPtrType.Type)
                        return leftPtrType;
                    else
                        throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
            }
        }

        private GloryType VerifyTypeOfDoubleEquals(Node node)
        {
            NonLeafNode nonLeafNode = (NonLeafNode)node;
            GloryType leftPtrType = VerifyAndGetTypeOf(nonLeafNode.LeftPtr);
            GloryType rightPtrType = VerifyAndGetTypeOf(nonLeafNode.RightPtr);

            if (leftPtrType == rightPtrType)
                return new GloryType(GloryTypes.Bool);
            else
                throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
        }

        private GloryType VerifyTypeOfIntComparisonOperators(Node node)
        {
            NonLeafNode nonLeafNode = (NonLeafNode)node;
            GloryType leftType = VerifyAndGetTypeOf(nonLeafNode.LeftPtr);
            GloryType rightType = VerifyAndGetTypeOf(nonLeafNode.RightPtr);

            if (leftType == rightType && leftType.Type == GloryTypes.Int)
            {
                // Comparison operators always give bool back
                if (node.NodeType is NodeType.GreaterThan or NodeType.GreaterThanEquals or NodeType.LessThan or NodeType.LessThanEquals)
                    return new GloryType(GloryTypes.Bool);
                else
                    return leftType;
            }
            else
                throw new Exception("Type error between " + leftType.Type + " and " + rightType.Type);
        }

        public bool VerifyReturn(List<Statement> statements)
        {
            int index = -1;
            foreach (Statement statement in statements)
            {
                index++;
                if (statement is ReturnStatement)
                {
                    statements.RemoveRange(index +1, statements.Count - index -1);
                    return true;
                }
                else if (statement is IfStatement ifStatement)
                {
                    bool mainBranchReturns = VerifyReturn(ifStatement.Code);
                    if (ifStatement.Else != null)
                    {
                        bool elseBranchBranches = VerifyReturn(ifStatement.Else.Code);

                        // If both the main *and* else branches return, this block clearly always returns.
                        if (mainBranchReturns && elseBranchBranches)
                        {
                            statements.RemoveRange(index + 1, statements.Count - index - 1);
                            return true;
                        }
                    }
                }
                else if (statement is WhileStatement whileStatement)
                {
                    // Special case: If we have a really obvious "while true", we'll count that as an always-returning block
                    if (whileStatement.Condition is BoolNode { Bool: true }) return true;
                }
            }
            return false;
        }

        private BlockStatement EnterBlock(BlockStatement newLoop)
        {
            BlockStatement previousBlock = _currentBlock;
            _currentBlock = newLoop;
            return previousBlock;
        }

        private void ExitBlock(BlockStatement previousBlock)
        {
            // Update our "current variables"
            for (int i = 0; i < _currentBlock.Vars.Count; i++)
                _currentVariables.RemoveAt(_currentVariables.Count - 1);

            _currentBlock = previousBlock;
        }
    }
}
