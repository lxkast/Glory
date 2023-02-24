using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Token
    {
        public TokenType Type;
        public Token(TokenType type)
        {
            Type = type;
        }

        
    }

    internal class NumberLiteralToken : Token
    {
        public int Val;
        public NumberLiteralToken(int val) : base(TokenType.NUMLITTLE)
        {
            Val = val;
        }
    }

    internal class StringLiteralToken : Token
    {
        public string Val;
        public StringLiteralToken(string val) : base(TokenType.STRING)
        {
            Val = val;
        }
    }

    internal class IdentifierLiteralToken : Token
    {
        public string Val;
        public IdentifierLiteralToken(string val) : base(TokenType.IDENTIFIER)
        {
            Val = val;
        }
    }

    public enum TokenType
    {
        //FLOAT,
        PLUS, //tick
        MINUS, //tick
        TIMES, //tick
        DIVIDE, //tick
        SEMICOLON, //tick
        OPENBRACKET,//tick
        CLOSEBRACKET, //tick
        OPENCURLY, //tick
        CLOSECURLY, //tick
        INTTYPE, //tick
        STRINGTYPE,
        STRING, //tick
        NUMLITTLE,
        BLANK, //tick
        IDENTIFIER,
        IF,
        ELIF,
        ELSE,
        LESSTHAN,
        EQUALS, //tick
        DOUBLEEQUALS, //tick
        GREATERTHAN
    }
}
