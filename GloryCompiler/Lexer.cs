using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Lexer
    {
        public string lexerString;
        public int currentPosition;
        public List<Token> output;

        public Lexer(string str)
        {
            lexerString = str;
            currentPosition = 0;
            output = new List<Token>();
            Iterate();
        }

        private void AddToken(Token token) => output.Add(token);

        private char PeekAhead(int count)
        {
            if (currentPosition + count < lexerString.Length)
                return lexerString[currentPosition + count];
            else 
                return (char)0;
        }

        public char ReadChar()
        {
            if (currentPosition < lexerString.Length)
                return lexerString[currentPosition];
            else 
                return (char)0;
        }

        public void HandleIndentifier()
        {
            string stringLiteral = "";
            while (char.IsLetter(ReadChar()))
            {
                stringLiteral = stringLiteral + ReadChar();
                currentPosition++;
            }
            
            if (stringLiteral != "")
            {
                currentPosition--;
                AddToken(new IdentifierLiteralToken(stringLiteral));
            }
                
        }

        public void Iterate()
        {
            for (; currentPosition < lexerString.Length; currentPosition++)
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
                            currentPosition++;
                            AddToken(new Token(TokenType.DoubleEquals));
                        }
                        else
                            AddToken(new Token(TokenType.Equals));
                        break;
                    case '"':
                        string stringLiteral = "";
                        currentPosition++;
                        while (ReadChar() != '"')
                        {
                            stringLiteral = stringLiteral + ReadChar();
                            currentPosition++;
                        }
                        AddToken(new StringLiteralToken(stringLiteral));
                        break;
                    case 'b':
                        if (PeekAhead(1) == 'l' && PeekAhead(2) == 'a' && PeekAhead(3) == 'n' && PeekAhead(4) == 'k')
                        {
                            AddToken(new Token(TokenType.Blank));
                            currentPosition += 4;
                        }
                        else
                            HandleIndentifier();
                        break;
                    case 'i':
                        if(PeekAhead(1) == 't' && PeekAhead(2) == 't')
                        {
                            AddToken(new Token(TokenType.IntType));
                            currentPosition += 2;
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
                                currentPosition++;
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
        }
    }
}
