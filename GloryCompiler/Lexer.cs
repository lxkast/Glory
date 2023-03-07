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

        public List<Token> Process()
        {
            for (; _currentPos < _currentStr.Length; _currentPos++)
            {
                char currentChar = GetCurrentChar();
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
                        if (PeekAhead(1) == '/')
                            AddToken(new Token(TokenType.Div));
                        else
                            AddToken(new Token(TokenType.Divide));
                        break;
                    case '%':
                        AddToken(new Token(TokenType.Mod));
                        break;
                    case '^':
                        AddToken(new Token(TokenType.Index));
                        break;
                    case ';':
                        AddToken(new Token(TokenType.Semicolon));
                        break;
                    case ',':
                        AddToken(new Token(TokenType.Comma));
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
                    case '[':
                        AddToken(new Token(TokenType.OpenSquare));
                        break;
                    case ']':
                        AddToken(new Token(TokenType.CloseSquare));
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
                    case '>':
                        if (PeekAhead(1) == '=')
                        {
                            _currentPos++;
                            AddToken(new Token(TokenType.GreaterThanEquals));
                        }
                        else
                            AddToken(new Token(TokenType.GreaterThan));
                        break;
                    case '<':
                        if (PeekAhead(1) == '=')
                        {
                            _currentPos++;
                            AddToken(new Token(TokenType.LessThanEquals));
                        }
                        else
                            AddToken(new Token(TokenType.LessThan));
                        break;
                    case '"':
                        string stringLiteral = "";
                        _currentPos++;
                        while (GetCurrentChar() != '"')
                        {
                            stringLiteral = stringLiteral + GetCurrentChar();
                            _currentPos++;
                        }
                        AddToken(new StringLiteralToken(stringLiteral));
                        break;
                    case 'b':
                        if (PeekAhead(1) == 'l' && PeekAhead(2) == 'a' && PeekAhead(3) == 'n' && PeekAhead(4) == 'k' && char.IsWhiteSpace(PeekAhead(5)))
                        {
                            AddToken(new Token(TokenType.Blank));
                            _currentPos += 5;
                        }
                        else if (PeekAhead(1) == 'o' && PeekAhead(2) == 'o' && PeekAhead(3) == 'l' && char.IsWhiteSpace(PeekAhead(4)))
                        {
                            AddToken(new Token(TokenType.BoolType));
                            _currentPos += 4;
                        }
                        else
                            ReadIdentifier();
                        break;
                    case 'e':
                        if (PeekAhead(1) == 'l' && PeekAhead(2) == 'i' && PeekAhead(3) == 'f' && char.IsWhiteSpace(PeekAhead(4)))
                        {
                            AddToken(new Token(TokenType.ElseIf));
                            _currentPos += 4;
                        }
                        else if (PeekAhead(1) == 'l' && PeekAhead(2) == 's' && PeekAhead(3) == 'e' && char.IsWhiteSpace(PeekAhead(4)))
                        {
                            AddToken(new Token(TokenType.Else));
                            _currentPos += 4;
                        }
                        else
                            ReadIdentifier();

                        break;
                    case 'f':
                        if (PeekAhead(1) == 'l' && PeekAhead(2) == 'o' && PeekAhead(3) == 'a' && PeekAhead(4) == 't' && char.IsWhiteSpace(PeekAhead(5)))
                        {
                            AddToken(new Token(TokenType.FloatType));
                            _currentPos += 6;
                        }
                        else if (PeekAhead(1) == 'a' && PeekAhead(2) == 'l' && PeekAhead(3) == 's' && PeekAhead(4) == 'e' && char.IsWhiteSpace(PeekAhead(5)))
                        {
                            AddToken(new BoolLiteralToken(false));
                            _currentPos += 5;
                        }
                        else if (PeekAhead(1) == 'a' && PeekAhead(2) == 'l' && PeekAhead(3) == 's' && PeekAhead(4) == 'e' && PeekAhead(5) == ';')
                        {
                            AddToken(new BoolLiteralToken(false));
                            AddToken(new Token(TokenType.Semicolon));
                            _currentPos += 5;
                        }
                        else
                            ReadIdentifier();
                        break;

                    case 'i':
                        if (PeekAhead(1) == 'n' && PeekAhead(2) == 't' && char.IsWhiteSpace(PeekAhead(3)))
                        {
                            AddToken(new Token(TokenType.IntType));
                            _currentPos += 3;
                        }
                        else if (PeekAhead(1) == 'f' && char.IsWhiteSpace(PeekAhead(2)))
                        {
                            _currentPos += 2;
                            AddToken(new Token(TokenType.If));
                        }
                        else
                            ReadIdentifier();
                        break;
                    case 'r':
                        if (PeekAhead(1) == 'e' && PeekAhead(2) == 't' && PeekAhead(3) == 'u' && PeekAhead(4) == 'r' && PeekAhead(5) == 'n' && char.IsWhiteSpace(PeekAhead(6)))
                        {
                            AddToken(new Token(TokenType.Return));
                            _currentPos += 6;
                        }
                        else
                            ReadIdentifier();
                        break;
                    case 's':
                        if (PeekAhead(1) == 't' && PeekAhead(2) == 'r' && PeekAhead(3) == 'i' && PeekAhead(4) == 'n' && PeekAhead(5) == 'g' && char.IsWhiteSpace(PeekAhead(6)))
                        {
                            AddToken(new Token(TokenType.StringType));
                            _currentPos += 6;
                        }
                        else
                            ReadIdentifier();
                        break;
                    case 't':
                        if (PeekAhead(1) == 'r' && PeekAhead(2) == 'u' && PeekAhead(3) == 'e' && char.IsWhiteSpace(PeekAhead(4)))
                        {
                            AddToken(new BoolLiteralToken(true));
                            _currentPos += 4;
                        }
                        else if(PeekAhead(1) == 'r' && PeekAhead(2) == 'u' && PeekAhead(3) == 'e' && PeekAhead(4) == ';')
                        {
                            AddToken(new BoolLiteralToken(true));
                            AddToken(new Token(TokenType.Semicolon));
                            _currentPos += 4;
                        }
                        break;
                    case 'w':
                        if (PeekAhead(1) == 'h' && PeekAhead(2) == 'i' && PeekAhead(3) == 'l' && PeekAhead(4) == 'e' && char.IsWhiteSpace(PeekAhead(5)))
                        {
                            AddToken(new Token(TokenType.While));
                            _currentPos += 5;
                        }
                        else
                            ReadIdentifier();
                        break;
                    case '#':
                        while (GetCurrentChar() != '\n')
                        {
                            _currentPos += 1;
                        }
                        break;
                    default:
                        if (char.IsDigit(currentChar))
                            ReadNumber();
                        else
                            ReadIdentifier();

                        break;
                }
            }

            return _result;
        }

        private void ReadNumber()
        {
            string newNumLiteral = "";
            while (char.IsDigit(GetCurrentChar()))
            {
                newNumLiteral += GetCurrentChar();
                _currentPos++;
            }
            _currentPos--;
            AddToken(new NumberLiteralToken(int.Parse(newNumLiteral)));
        }

        private void ReadIdentifier()
        {
            string currentIdentifier = "";
            while (char.IsLetter(GetCurrentChar()))
            {
                currentIdentifier = currentIdentifier + GetCurrentChar();
                _currentPos++;
            }
            
            if (currentIdentifier != "")
            {
                _currentPos--; // The last letter we encountered was not part of the identifier, so make sure it doesn't get skipped.
                AddToken(new IdentifierLiteralToken(currentIdentifier));
            }
        }

        private char PeekAhead(int count)
        {
            if (_currentPos + count < _currentStr.Length)
                return _currentStr[_currentPos + count];
            else
                return (char)0;
        }

        private char GetCurrentChar()
        {
            if (_currentPos < _currentStr.Length)
                return _currentStr[_currentPos];
            else
                return (char)0;
        }

        private void AddToken(Token token) => _result.Add(token);
    }
}
