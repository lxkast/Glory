using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler.Representation
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
        public int Int;

        public IntNode(int Int) : base(NodeType.NumberLiteral) => this.Int = Int;
    }

    internal class BoolNode : Node
    {
        public bool Bool;

        public BoolNode(bool Bool) : base(NodeType.BoolLiteral) => this.Bool = Bool;
    }

    internal class CallNode : Node
    {
        public Function Function;
        public List<Node> Args;
        public CallNode(Function function, List<Node> args) : base(NodeType.Call)
        {
            Function = function;
            Args = args;
        }
    }

    internal class NativeCallNode : Node
    {
        public NativeFunction Function;
        public List<Node> Args;
        public NativeCallNode(NativeFunction function, List<Node> args) : base(NodeType.NativeCall)
        {
            Function = function;
            Args = args;
        }
    }

    internal class IndexNode : Node
    {
        public Node Target;
        public Node Index;

        public IndexNode(Node target, Node index) : base(NodeType.Indexer)
        {
            Target = target;
            Index = index;
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
        Expression,
        Compare,
        Plus,
        Minus,
        Divide,
        Div,
        Mod,
        Multiply,
        Index,
        Negate,
        Call,
        NativeCall,
        Indexer,
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