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

        private void AddToken(Token token)
        {
            output.Add(token);
        }

        private char PeekAhead(int count)
        {
            if (currentPosition + count < lexerString.Length)
            {
                return lexerString[currentPosition + count];
            }
            else return (char)0;
        }

        public char ReadChar()
        {
            if (currentPosition < lexerString.Length)
            {
                return lexerString[currentPosition];
            }
            else return (char)0;
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
                        AddToken(new Token(TokenType.PLUS));
                        break;
                    case '-':
                        AddToken(new Token(TokenType.MINUS));
                        break;
                    case '*':
                        AddToken(new Token(TokenType.TIMES));
                        break;
                    case '/':
                        AddToken(new Token(TokenType.DIVIDE));
                        break;
                    case ';':
                        AddToken(new Token(TokenType.SEMICOLON));
                        break;
                    case '(':
                        AddToken(new Token(TokenType.OPENBRACKET));
                        break;
                    case ')':
                        AddToken(new Token(TokenType.CLOSEBRACKET));
                        break;
                    case '{':
                        AddToken(new Token(TokenType.OPENCURLY));
                        break;
                    case '}':
                        AddToken(new Token(TokenType.CLOSECURLY));
                        break;
                    case '=':
                        if (PeekAhead(1) == '=')
                        {
                            currentPosition++;
                            AddToken(new Token(TokenType.DOUBLEEQUALS));
                        }
                        else
                            AddToken(new Token(TokenType.EQUALS));
                        break;
                    case '"':
                        string stringLiteral = "";
                        while (ReadChar() != '"')
                        {
                            stringLiteral = stringLiteral + ReadChar();
                            currentPosition++;
                        }
                        currentPosition--;
                        AddToken(new StringLiteralToken(stringLiteral));
                        break;
                    case 'b':
                        if (PeekAhead(1) == 'l' && PeekAhead(2) == 'a' && PeekAhead(3) == 'n' && PeekAhead(4) == 'k')
                        {
                            AddToken(new Token(TokenType.BLANK));
                            currentPosition += 4;
                        }
                        else
                            HandleIndentifier();
                        break;
                    case 'i':
                        if(PeekAhead(1) == 't' && PeekAhead(2) == 't')
                        {
                            AddToken(new Token(TokenType.INTTYPE));
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
