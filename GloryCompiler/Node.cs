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
        Additive,
        Divide,
        Multiply,
        Index,
        Negate,
        Call,
        Unary,
        Null
    }
}
