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
        List<Variable> _currentVariables;

        BlockStatement _currentBlock;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _currentIndex = 0;
            GlobalStatements = new List<Statement>();
            GlobalVariables = new List<Variable>();
            _currentVariables = new List<Variable>();
            ParseOuterStatements();
        }

        private Token ReadToken()
        {
            if (_currentIndex < _tokens.Count)
                return _tokens[_currentIndex];
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

        public bool ParseOuterStatement()
        {
            return ParseStatement();
        }

        public void ParseOuterStatements()
        {
            bool isStatement = true;
            while (ReadToken().Type != TokenType.Null && isStatement)
            {
                isStatement = ParseOuterStatement();
            }
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
                ParseVariable();
                if (ReadToken().Type != TokenType.Semicolon) throw new Exception("Expected semicolon");
                _currentIndex++;
            }

            // Assignments (a = ...)
            else if (ReadToken().Type == TokenType.Identifier)
                ParseAssignment();

            // While (while ...)
            else if (ReadToken().Type == TokenType.While)
                ParseWhile();

            // If (if ...)
            else if (ReadToken().Type == TokenType.If)
                ParseIf();

            else
                return false; // Return false if we had no idea what to do with this

            return true; // Return true if we were happy with what we parsed
        }

        public void ParseVariable()
        {
            TokenType type = ReadToken().Type;
            _currentIndex++;

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
            _currentIndex++;

            if (ReadToken().Type == TokenType.Equals)
            {
                string val = ((IdentifierLiteralToken)ReadToken()).Val;
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
            if (VerifyAndGetTypeOf(condition) != NodeType.BoolLiteral) throw new Exception("Invalid condition");

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
            if (VerifyAndGetTypeOf(condition) != NodeType.BoolLiteral) throw new Exception("Expected expression of type 'bool' for if condition.");

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
                _currentIndex++;
                Node nextTerm = ParseDivide();

                if (ReadToken().Type == TokenType.Plus)
                    currentTree = new NonLeafNode(NodeType.Plus, currentTree, nextTerm);
                else
                    currentTree = new NonLeafNode(NodeType.Minus, currentTree, nextTerm);
            }

            return currentTree;
        }

        public Node ParseDivide()
        {
            Node currentTree = ParseMultiply();

            while (ReadToken().Type == TokenType.Divide)
            {
                _currentIndex++;
                Node nextTerm = ParseMultiply();
                currentTree = new NonLeafNode(NodeType.Divide, currentTree, nextTerm);
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

            Node currentTree = ParseUnary();
            if (negateCount % 2 == 1)
                currentTree = new NonLeafNode(NodeType.Multiply, currentTree, new IntNode(-1));

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

        private NodeType VerifyAndGetTypeOf(Node node)
        {
            switch (node.NodeType)
            {
                case NodeType.Null:
                case NodeType.NumberLiteral:
                case NodeType.StringLiteral:
                case NodeType.BoolLiteral:
                    return node.NodeType;

                case NodeType.Variable:
                    return ((VariableNode)node).Variable.Type switch
                    {
                        TokenType.IntType => NodeType.NumberLiteral,
                        TokenType.StringType => NodeType.StringLiteral,
                        TokenType.BoolType => NodeType.BoolLiteral,
                        _ => throw new Exception("Unknown variable type")
                    };
                case NodeType.DoubleEquals:
                    return VerifyTypeOfDoubleEquals(node);

                case NodeType.GreaterThan:
                case NodeType.GreaterThanEquals:
                case NodeType.LessThan:
                case NodeType.LessThanEquals:
                    return VerifyTypeOfIntComparisonOperators(node);

                default:
                    NonLeafNode nonLeafNode = (NonLeafNode)node;
                    NodeType leftPtrType = VerifyAndGetTypeOf(nonLeafNode.LeftPtr);
                    NodeType rightPtrType = VerifyAndGetTypeOf(nonLeafNode.RightPtr);

                    if (leftPtrType == rightPtrType)
                        return leftPtrType;
                    else
                        throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
            }
        }

        private NodeType VerifyTypeOfDoubleEquals(Node node)
        {
            NonLeafNode nonLeafNode = (NonLeafNode)node;
            NodeType leftPtrType = VerifyAndGetTypeOf(nonLeafNode.LeftPtr);
            NodeType rightPtrType = VerifyAndGetTypeOf(nonLeafNode.RightPtr);

            if (leftPtrType == rightPtrType)
                return NodeType.BoolLiteral;
            else
                throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
        }

        private NodeType VerifyTypeOfIntComparisonOperators(Node node)
        {
            NonLeafNode nonLeafNode = (NonLeafNode)node;
            NodeType leftPtrType = VerifyAndGetTypeOf(nonLeafNode.LeftPtr);
            NodeType rightPtrType = VerifyAndGetTypeOf(nonLeafNode.RightPtr);

            if (leftPtrType == rightPtrType && leftPtrType == NodeType.NumberLiteral)
            {
                // Comparison operators always give bool back
                if (node.NodeType is NodeType.GreaterThan or NodeType.GreaterThanEquals or NodeType.LessThan or NodeType.LessThanEquals)
                    return NodeType.BoolLiteral;
                else
                    return leftPtrType;
            }
            else
                throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
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
                _currentVariables.RemoveAt(_currentVariables.Count - i - 1);

            _currentBlock = previousBlock;
        }
    }
}
