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
        WhileStatement _currentLoop;

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
            if (_currentLoop == null)
                GlobalStatements.Add(statement);
            else
            {
                _currentLoop.Code.Add(statement);
            }
        }

        private void AddVariableToList(Variable variable)
        {
            if (_currentLoop == null)
                GlobalVariables.Add(variable);
            else
                _currentLoop.Vars.Add(variable);

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
            if (ReadToken().Type is TokenType.IntType or TokenType.StringType or TokenType.FloatType or TokenType.BoolType)
            {
                ParseVariable();
                if (ReadToken().Type == TokenType.Semicolon)
                {
                    _currentIndex++;
                }
                else
                {
                    throw new Exception("Expected semicolon");
                }
            }
            else if (ReadToken().Type == TokenType.Identifier)
            {
                ParseAssignment();
            }
            else if (ReadToken().Type == TokenType.While)
            {
                ParseWhile();
            }
            else if (ReadToken().Type == TokenType.If)
            {
                ParseIf();
            }
            else
            {
                return false;
            }
            return true;    
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

                    Node right = ParseExpression();
                    Node equals = new NonLeafNode(NodeType.Assignment, node, right);
                    VerifyTypesOn(equals);

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
                VerifyTypesOn(equals);

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
            Node condition = ParseExpression();
            if (VerifyTypesOn(condition) == NodeType.BoolLiteral)
            {
                if (ReadToken().Type == TokenType.OpenCurly)
                {
                    _currentIndex++;

                    WhileStatement currentLoopBefore = _currentLoop;
                    _currentLoop = new WhileStatement();
                    
                    ParseStatements();

                    WhileStatement loop = _currentLoop;
                    loop.Condition = condition;

                    for (int i = 0; i < _currentLoop.Vars.Count; i++)
                        _currentVariables.RemoveAt(_currentVariables.Count - i - 1);

                    _currentLoop = currentLoopBefore;
                    AddStatementToList(loop);

                    if (ReadToken().Type == TokenType.CloseCurly)
                    {
                        _currentIndex++;
                        return;
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
            }
            else
            {
                throw new Exception("Invalid condition");
            }
            
        }

        public void ParseIf()
        {
            _currentIndex++;
            Node condition = ParseExpression();
            if (ReadToken().Type == TokenType.OpenCurly)
            {
                _currentIndex++;
            }
            else
            {
                throw new Exception("Expected {");
            }
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
                case TokenType.OpenBracket:
                    _currentIndex++;
                    Node result = ParseExpression();

                    if (ReadToken().Type == TokenType.CloseBracket)
                        _currentIndex++;
                    else
                        throw new Exception("Expected closing bracket");

                    return result;    
                case TokenType.NumberLiteral:
                    int numVal = ((NumberLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new IntNode(numVal);
                case TokenType.StringLiteral:
                    string strVal = ((StringLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new StringNode(strVal);
                case TokenType.BoolLiteral:
                    bool blVal = ((BoolLiteralToken)ReadToken()).Val;
                    _currentIndex++;
                    return new BoolNode(blVal);
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

        private NodeType VerifyTypesOn(Node node)
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

                    NodeType leftPtrType = VerifyTypesOn(((NonLeafNode)node).LeftPtr);
                    NodeType rightPtrType = VerifyTypesOn(((NonLeafNode)node).RightPtr);
                    if (leftPtrType == rightPtrType)
                        return leftPtrType;
                    else
                        throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
            }
        }

        private NodeType VerifyTypeOfDoubleEquals(Node node)
        {
            NodeType leftPtrType = VerifyTypesOn(((NonLeafNode)node).LeftPtr);
            NodeType rightPtrType = VerifyTypesOn(((NonLeafNode)node).LeftPtr);
            if (leftPtrType == rightPtrType)
                return NodeType.BoolLiteral;
            else
                throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
        }

        private NodeType VerifyTypeOfIntComparisonOperators(Node node)
        {
            NodeType leftPtrType = VerifyTypesOn(((NonLeafNode)node).LeftPtr);
            NodeType rightPtrType = VerifyTypesOn(((NonLeafNode)node).RightPtr);
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
    }
}
