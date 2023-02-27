using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Node
    {
        public NodeType _nodeType;
        
        public Node(NodeType nodeType)
        {
            _nodeType = nodeType;
        }
    }

    internal class NonLeafNode : Node
    {
        Node _leftPtr;
        Node _rightPtr;

        public NonLeafNode(NodeType type, Node leftPtr, Node rightPtr) : base(type)
        {
            _leftPtr = leftPtr;
            _rightPtr = rightPtr;
        }
    }

    internal class VariableNode : Node
    {
        public Variable _variable;

        public VariableNode(Variable variable) : base(NodeType.Variable)
        {
            _variable = variable;
        }
    }

    internal class StringNode : Node
    {
        public string _string;

        public StringNode(string String) : base(NodeType.StringLiteral)
        {
            _string = String;
        }
    }

    internal class IntNode : Node
    {
        public int _int;

        public IntNode(int Int) : base(NodeType.NumberLiteral)
        {
            _int = Int;
        }
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
        Typename,
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

        DoubleEquals,
        GreaterThan,
        GreaterThanEquals,
        LessThan,
        LessThanEquals,

        Null
    }
}
