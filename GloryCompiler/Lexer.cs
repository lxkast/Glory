using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Lexer
    {
        string _currentStr;
        int _currentPos;
        List<Token> _result;

        public Lexer(string str)
        {
            _currentStr = str;
            _currentPos = 0;
            _result = new List<Token>();
        }

        private void AddToken(Token token) => _result.Add(token);

        private char PeekAhead(int count)
        {
            if (_currentPos + count < _currentStr.Length)
                return _currentStr[_currentPos + count];
            else 
                return (char)0;
        }

        public char ReadChar()
        {
            if (_currentPos < _currentStr.Length)
                return _currentStr[_currentPos];
            else 
                return (char)0;
        }

        public void HandleIndentifier()
        {
            string stringLiteral = "";
            while (char.IsLetter(ReadChar()))
            {
                stringLiteral = stringLiteral + ReadChar();
                _currentPos++;
            }
            
            if (stringLiteral != "")
            {
                _currentPos--;
                AddToken(new IdentifierLiteralToken(stringLiteral));
            }
                
        }

        public List<Token> Process()
        {
            for (; _currentPos < _currentStr.Length; _currentPos++)
            {
                char currentChar = ReadChar();
                switch (currentChar)
                {
                    case '+':
                        AddToken(new Token(TokenType.Plus));
                        break;
                    case '-':
                        AddToken(new Token(TokenType.Minus));
                        break;
                    case '*':
                        AddToken(new Token(TokenType.Times));
                        break;
                    case '/':
                        AddToken(new Token(TokenType.Divide));
                        break;
                    case ';':
                        AddToken(new Token(TokenType.Semicolon));
                        break;
                    case '(':
                        AddToken(new Token(TokenType.OpenBracket));
                        break;
                    case ')':
                        AddToken(new Token(TokenType.CloseBracket));
                        break;
                    case '{':
                        AddToken(new Token(TokenType.OpenCurly));
                        break;
                    case '}':
                        AddToken(new Token(TokenType.CloseCurly));
                        break;
                    case '=':
                        if (PeekAhead(1) == '=')
                        {
                            _currentPos++;
                            AddToken(new Token(TokenType.DoubleEquals));
                        }
                        else
                            AddToken(new Token(TokenType.Equals));
                        break;
                    case '"':
                        string stringLiteral = "";
                        _currentPos++;
                        while (ReadChar() != '"')
                        {
                            stringLiteral = stringLiteral + ReadChar();
                            _currentPos++;
                        }
                        AddToken(new StringLiteralToken(stringLiteral));
                        break;
                    case 'b':
                        if (PeekAhead(1) == 'l' && PeekAhead(2) == 'a' && PeekAhead(3) == 'n' && PeekAhead(4) == 'k')
                        {
                            AddToken(new Token(TokenType.Blank));
                            _currentPos += 4;
                        }
                        else
                            HandleIndentifier();
                        break;
                    case 'i':
                        if(PeekAhead(1) == 't' && PeekAhead(2) == 't')
                        {
                            AddToken(new Token(TokenType.IntType));
                            _currentPos += 2;
                        }
                        else
                            HandleIndentifier();
                        break;
                    default:
                        if (char.IsDigit(currentChar))
                        {
                            string newStringLiteral = "";
                            while (char.IsDigit(ReadChar()))
                            {
                                newStringLiteral = newStringLiteral + ReadChar();
                                _currentPos++;
                            }
                            AddToken(new NumberLiteralToken(int.Parse(newStringLiteral)));
                        }
                        else
                        {
                            HandleIndentifier();
                        }
                        break;
                }
            }
            Console.WriteLine("Debug Point");
            return _result;
        }
    }
}
