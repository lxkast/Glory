using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Node
    {
        public NodeType NodeType;
        
        public Node(NodeType nodeType)
        {
            NodeType = nodeType;
        }
    }

    internal class NonLeafNode : Node
    {
        public Node LeftPtr;
        public Node RightPtr;

        public NonLeafNode(NodeType type, Node leftPtr, Node rightPtr) : base(type)
        {
            LeftPtr = leftPtr;
            RightPtr = rightPtr;
        }
    }

    internal class VariableNode : Node
    {
        public Variable Variable;

        public VariableNode(Variable variable) : base(NodeType.Variable) => Variable = variable;
    }

    internal class StringNode : Node
    {
        public string String;

        public StringNode(string str) : base(NodeType.StringLiteral) => String = str;
    }

    internal class IntNode : Node
    {
        public int _int;

        public IntNode(int Int) : base(NodeType.NumberLiteral) => _int = Int;
    }

    internal class BoolNode : Node
    {
        public bool _bool;

        public BoolNode(bool Bool) : base(NodeType.BoolLiteral) => _bool = Bool;
    }

    public enum NodeType
    {
        OuterStatement,
        Statement,
        Variable,
        Assignment,
        If,
        While,
        Function,
        Expression,
        Compare,
        Plus,
        Minus,
        Divide,
        Multiply,
        Index,
        Negate,
        Call,
        Unary,
        StringLiteral,
        NumberLiteral,
        BoolLiteral,

        DoubleEquals,
        GreaterThan,
        GreaterThanEquals,
        LessThan,
        LessThanEquals,

        Null
    }
}