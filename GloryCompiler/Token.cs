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

    internal class BoolLiteralToken : Token
    {
        public bool Val;
        public BoolLiteralToken(bool val) : base(TokenType.BoolLiteral) => Val = val;
    }

    public enum TokenType
    {
        // Operators:
        Plus, 
        Minus, 
        Times, 
        Divide, 
        LessThan, 
        LessThanEquals, 
        Equals, 
        DoubleEquals, 
        GreaterThan, 
        GreaterThanEquals, 
        Index, 
        Mod, 
        Div, 


        Comma,

        // Types:
        IntType, 
        StringType, 
        FloatType, 
        BoolType, 
        Blank, 

        // General Structure:
        Semicolon, 
        OpenBracket,
        CloseBracket, 
        OpenCurly, 
        CloseCurly, 
        OpenSquare,
        CloseSquare,

        // Literals:
        StringLiteral, 
        NumberLiteral, 
        BoolLiteral,
        Identifier,

        // Keywords:
        If, 
        ElseIf,
        Else,
        While,
        Return,

        // Null:
        Null
    }
}
