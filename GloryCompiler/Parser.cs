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
            if (ReadToken().Type is TokenType.IntType or TokenType.StringType or TokenType.FloatType)
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
            if (ReadToken().Type == TokenType.OpenCurly)
            {
                _currentIndex++;
                _currentLoop = new WhileStatement();
                ParseStatements();
                WhileStatement loop = _currentLoop;
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

        public Node ParseExpression()
        {
            _currentIndex++;
            return new Node(NodeType.Null);
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
    }
}
