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
                            ParseVariable(type);
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
            Node returnExpression= ParseExpression();
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
            _currentIndex+=2;
            List<Variable> parameters = new List<Variable>();

            while (ReadToken().Type != TokenType.CloseBracket)
            {
                if (ReadToken().Type is not TokenType.IntType and not TokenType.StringType and not TokenType.FloatType)
                    throw new Exception("Expected type");

                GloryType paramType = ParseType();

                if (ReadToken().Type == TokenType.Identifier)
                {
                    string paramName = ((IdentifierLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    parameters.Add(new Variable(paramType, paramName));

                    if (ReadToken().Type == TokenType.CloseBracket) break;

                    if (ReadToken().Type == TokenType.Comma)
                        _currentIndex++;
                    else
                        throw new Exception("Expected comma");
                }
                else
                {
                    throw new Exception("Expected identifier");
                }
            }

            _currentIndex++; // Eat the )
            _currentVariables.AddRange(parameters);

            Function func = new Function(parameters, name, returnType);

            if (ReadToken().Type == TokenType.OpenCurly)
            {
                _currentIndex++;
                _GlobalFunctions.Add(func);
                _currentFunction = func;
                BlockStatement previousBlock = EnterBlock(func);
                ParseStatements();
                ExitBlock(previousBlock);
                _currentFunction = null;
                if (ReadToken().Type == TokenType.CloseCurly)
                {
                    _currentIndex++;
                }
                else
                {
                    throw new Exception("Expected }");
                }
            }
            else
            {
                throw new Exception("Expected {");
            }
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
            while (ReadToken().Type == TokenType.OpenSquare)
            {
                _currentIndex++;
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
                else if (ReadToken().Type == TokenType.CloseSquare)
                {
                    currentType = new ListGloryType(currentType);
                    _currentIndex++;
                }
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
                NodeType operationNodeType = NodeType.Null;
                switch (operationToken.Type)
                {
                    case TokenType.Plus:
                        operationNodeType = NodeType.Plus;
                        break;
                    case TokenType.Minus:
                        operationNodeType = NodeType.Minus;
                        break;
                    case TokenType.Times:
                        operationNodeType = NodeType.Multiply;
                        break;
                    case TokenType.Divide:
                        operationNodeType = NodeType.Divide;
                        break;
                }
                _currentIndex++;
                if (ReadToken().Type == TokenType.Equals)
                {
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
                else
                {
                    throw new Exception("Expected equals");
                }
            }
            else
            {
                throw new Exception("Expected equals");
            }
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
                if (ReadToken().Type == TokenType.Divide)
                {
                    _currentIndex++;
                    Node nextTerm = ParseMultiply();
                    currentTree = new NonLeafNode(NodeType.Divide, currentTree, nextTerm);
                }
                else if (ReadToken().Type == TokenType.Div)
                {
                    _currentIndex++;
                    Node nextTerm = ParseMultiply();
                    currentTree = new NonLeafNode(NodeType.Div, currentTree, nextTerm);
                }
                else
                {
                    _currentIndex++;
                    Node nextTerm = ParseMultiply();
                    currentTree = new NonLeafNode(NodeType.Mod, currentTree, nextTerm);
                }
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
            if(ReadToken().Type == TokenType.Identifier && PeekToken(1).Type == TokenType.OpenBracket)
            {
                string name = ((IdentifierLiteralToken)ReadToken()).Val;
                Function func = FindFunction(name);
                _currentIndex += 2;

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
                if (arguments.Count != func.Parameters.Count) 
                    throw new Exception("Function " + name + " does not accept " + arguments.Count + " arguments.");

                for (int i = 0; i < arguments.Count; i++)
                {
                    GloryType argumentType = VerifyAndGetTypeOf(arguments[i]);

                    if (argumentType != func.Parameters[i].Type) 
                        throw new Exception("Call to " + name + "() has incorrect argument types.");
                }

                CallNode res = new CallNode(func, arguments);
                return res;
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
                if (VerifyAndGetTypeOf(currentTree).Type is not GloryTypes.Array and not GloryTypes.String and not GloryTypes.List)
                    throw new Exception("Can only index arrays, lists or strings");
                _currentIndex++;
                Node index = ParseExpression();
                if (VerifyAndGetTypeOf(index).Type != GloryTypes.Int)
                    throw new Exception("Array index must be an integer");
                currentTree = new IndexNode(currentTree, index);
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
            for (int i = 0; i < _currentVariables.Count();i++)
            {
                if (_currentVariables[i].Name == name)
                    return _currentVariables[i];
            }
            throw new Exception("Cannot find variable with name " + name);
        }

        private Function FindFunction(string name)
        {
            for (int i = 0; i < _GlobalFunctions.Count(); i++)
            {
                if (_GlobalFunctions[i].Name == name)
                    return _GlobalFunctions[i];
            }
            throw new Exception("Cannot find function with name " + name);
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
                    if (newNode._function.ReturnType == null) throw new Exception("Cannot use return value of Blank function");
                    return newNode._function.ReturnType;
                case NodeType.Indexer:
                    IndexNode newwNode = (IndexNode)node;
                    GloryType targetType = VerifyAndGetTypeOf(newwNode._target);
                    if (targetType.Type == GloryTypes.Array)
                        return ((ArrayGloryType)VerifyAndGetTypeOf(newwNode._target)).ItemType;
                    else if (targetType.Type == GloryTypes.String)
                        return new GloryType(GloryTypes.String);
                    else if (targetType.Type == GloryTypes.List)
                        return ((ListGloryType)VerifyAndGetTypeOf(newwNode._target)).ItemType;
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
            foreach (Statement statement in statements)
            {
                if (statement is ReturnStatement)
                {
                    return true;
                }
                else
                {
                    if (statement is IfStatement ifStatement)
                    {
                        bool isMainBlock = VerifyReturn(ifStatement.Code);
                        if (ifStatement.Else != null)
                        {
                            bool isElse = VerifyReturn(ifStatement.Else.Code);
                            if (isMainBlock && isElse) return true;
                        }
                    }
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
