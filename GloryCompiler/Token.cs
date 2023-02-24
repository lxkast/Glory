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
        public Token(TokenType type) => Type = type;
    }

    internal class NumberLiteralToken : Token
    {
        public int Val;
        public NumberLiteralToken(int val) : base(TokenType.NumberLiteral) => Val = val;
    }

    internal class StringLiteralToken : Token
    {
        public string Val;
        public StringLiteralToken(string val) : base(TokenType.StringLiteral) => Val = val;
    }

    internal class IdentifierLiteralToken : Token
    {
        public string Val;
        public IdentifierLiteralToken(string val) : base(TokenType.Identifier) => Val = val;
    }

    public enum TokenType
    {
        //FLOAT,

        // Operators:
        Plus, //tick
        Minus, //tick
        Times, //tick
        Divide, //tick
        LessThan,
        Equals, //tick
        DoubleEquals, //tick
        GreaterThan,

        // Types:
        IntType, //tick
        StringType,
        Blank, //tick

        // General Structure:
        Semicolon, //tick
        OpenBracket,//tick
        CloseBracket, //tick
        OpenCurly, //tick
        CloseCurly, //tick

        // Literals:
        StringLiteral, //tick
        NumberLiteral,
        Identifier,

        // Keywords:
        If,
        ElseIf,
        Else
    }
}
