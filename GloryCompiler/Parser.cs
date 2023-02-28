using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Parser
    {
        public int _currentIndex;
        public List<Token> _TokenList;
        public List<Statement> _GlobalStatements;
        public List<Variable> _GlobalVariables;
        public WhileStatement _currentLoop;

        public Parser(List<Token> list)
        {
            _TokenList = list;
            _currentIndex = 0;
            _GlobalStatements = new List<Statement>();
            _GlobalVariables = new List<Variable>();
            ParseOuterStatements();
        }

        private Token ReadToken()
        {
            if (_currentIndex < _TokenList.Count)
                return _TokenList[_currentIndex];
            else
                return new Token(TokenType.Null);
        }

        private void AddStatementToList(Statement statement)
        {
            if (_currentLoop == null)
                _GlobalStatements.Add(statement);
            else
            {
                _currentLoop._code.Add(statement);
            }
        }

        private void AddVariableToList(Variable variable)
        {
            _GlobalVariables.Add(variable);
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
                Variable variable = new Variable(type, name);
                VariableNode node = new VariableNode(variable);
                AddVariableToList(variable);
                if (ReadToken().Type == TokenType.Equals)
                {
                    _currentIndex++;
                    Node expression = ParseExpression();
                    Node equals = new NonLeafNode(NodeType.Assignment,node,expression);
                    VerifyType(equals);
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
                Variable variable = FindIdentifier(val);
                Node expression = ParseExpression();
                VariableNode varNode = new VariableNode(variable);
                Node equals = new NonLeafNode(NodeType.Assignment, varNode, expression);
                VerifyType(equals);
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
            if (VerifyType(condition) == NodeType.BoolLiteral)
            {
                if (ReadToken().Type == TokenType.OpenCurly)
                {
                    _currentIndex++;
                    _currentLoop = new WhileStatement();
                    ParseStatements();
                    WhileStatement loop = _currentLoop;
                    loop._condition = condition;
                    _currentLoop = null;
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
                switch (ReadToken().Type)
                {
                    case TokenType.DoubleEquals:
                        _currentIndex++;
                        currentTree = CreateDoubleEqualsNode(currentTree);
                        break;
                    case TokenType.GreaterThanEquals:
                        _currentIndex++;
                        currentTree = CreateGreaterThanEqualsNode(currentTree);
                        break;
                    case TokenType.GreaterThan:
                        _currentIndex++;
                        currentTree = CreateGreaterThanNode(currentTree);
                        break;
                    case TokenType.LessThan:
                        _currentIndex++;
                        currentTree = CreateLessThanNode(currentTree);
                        break;
                    case TokenType.LessThanEquals:
                        _currentIndex++;
                        currentTree = CreateLessThanEqualsNode(currentTree);
                        break;
                }
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
            if (ReadToken().Type == TokenType.OpenBracket)
            {
                _currentIndex++;
                Node newNode = ParseExpression();
                if (ReadToken().Type == TokenType.CloseBracket)
                {
                    _currentIndex++;
                    return newNode;
                }
                else
                    throw new Exception("Expected closing bracket");
            }
            if (ReadToken().Type is TokenType.NumberLiteral)
            {
                int val = ((NumberLiteralToken)ReadToken()).Val;
                _currentIndex++;
                IntNode node = new IntNode(val);
                return (Node)node;
            }
            else if (ReadToken().Type is TokenType.StringLiteral)
            {
                string val = ((StringLiteralToken)ReadToken()).Val;
                _currentIndex++;
                StringNode node = new StringNode(val);
                return (Node)node;
            }
            else if (ReadToken().Type is TokenType.BoolLiteral)
            {
                bool val = ((BoolLiteralToken)ReadToken()).Val;
                _currentIndex++;
                BoolNode node = new BoolNode(val);
                return (Node)node;
            }
            else if (ReadToken().Type is TokenType.Identifier)
            {
                string val = ((IdentifierLiteralToken)ReadToken()).Val;
                _currentIndex++;
                VariableNode node = new VariableNode(FindIdentifier(val));
                return (Node)node;
            }
            else
            {
                throw new Exception("Syntax Error");
            }
        }

        public Variable FindIdentifier(string name)
        {
            for (int i = 0; i < _GlobalVariables.Count();i++)
            {
                if (_GlobalVariables[i]._name == name)
                {
                    return _GlobalVariables[i];
                }
            }
            throw new Exception("Cannot find variable with name " + name);
        }

        public NonLeafNode CreateDoubleEqualsNode(Node currentTree)
        {
            Node nextTerm = ParseAdditive();
            return new NonLeafNode(NodeType.DoubleEquals, currentTree, nextTerm);
        }

        public NonLeafNode CreateGreaterThanEqualsNode(Node currentTree)
        {
            Node nextTerm = ParseAdditive();
            return new NonLeafNode(NodeType.GreaterThanEquals, currentTree, nextTerm);
        }

        public NonLeafNode CreateGreaterThanNode(Node currentTree)
        {
            Node nextTerm = ParseAdditive();
            return new NonLeafNode(NodeType.GreaterThan, currentTree, nextTerm);
        }

        public NonLeafNode CreateLessThanNode(Node currentTree)
        {
            Node nextTerm = ParseAdditive();
            return new NonLeafNode(NodeType.LessThan, currentTree, nextTerm);
        }
        public NonLeafNode CreateLessThanEqualsNode(Node currentTree)
        {
            Node nextTerm = ParseAdditive();
            return new NonLeafNode(NodeType.LessThanEquals, currentTree, nextTerm);
        }

        public NodeType VerifyType(Node node)
        {
            if (node._nodeType is NodeType.Null or NodeType.NumberLiteral or NodeType.StringLiteral or NodeType.BoolLiteral or NodeType.Typename)
            {
                return node._nodeType;
            }
            else if (node._nodeType == NodeType.Variable)
            {
                if (((VariableNode)node)._variable._type == TokenType.IntType)
                {
                    return NodeType.NumberLiteral;
                }
                else if (((VariableNode)node)._variable._type == TokenType.StringType)
                {
                    return NodeType.StringLiteral;
                }
                else if (((VariableNode)node)._variable._type == TokenType.BoolType)
                {
                    return NodeType.BoolLiteral;
                }
                else
                {
                    throw new Exception("Unknown variable type");
                }
            }
            else if (node._nodeType is NodeType.DoubleEquals)
            {
                NodeType leftPtrType = VerifyType(((NonLeafNode)node)._leftPtr);
                NodeType rightPtrType = VerifyType(((NonLeafNode)node)._rightPtr);
                if (leftPtrType == rightPtrType)
                {
                    return NodeType.BoolLiteral;
                }
                else
                {
                    throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
                }
            }
            else if (node._nodeType is NodeType.GreaterThan or NodeType.GreaterThanEquals or NodeType.LessThan or NodeType.LessThanEquals)
            {
                NodeType leftPtrType = VerifyType(((NonLeafNode)node)._leftPtr);
                NodeType rightPtrType = VerifyType(((NonLeafNode)node)._rightPtr);
                if (leftPtrType == rightPtrType && leftPtrType == NodeType.NumberLiteral)
                {
                    return NodeType.BoolLiteral;
                }
                else
                {
                    throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
                }
            }
            else
            {
                NodeType leftPtrType = VerifyType(((NonLeafNode)node)._leftPtr);
                NodeType rightPtrType = VerifyType(((NonLeafNode)node)._rightPtr);
                if (leftPtrType == rightPtrType)
                {
                    return leftPtrType;
                }
                else
                {
                    throw new Exception("Type error between " + leftPtrType + " and " + rightPtrType);
                }
            }
           
        }
    }
}
