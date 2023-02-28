using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Statement
    {
        
    }
    internal class SingleLineStatement : Statement
    {
        public Node Expression;

        public SingleLineStatement(Node expression)
        {
            Expression = expression;
        }
    }

    internal class BlockStatement : Statement
    {
        public List<Statement> Code;
        public List<Variable> Vars;

        public BlockStatement()
        {
            Code = new List<Statement>();
            Vars = new List<Variable>();
        }
    }

    internal class WhileStatement : BlockStatement
    {
        public Node Condition;
    }

    internal class IfStatement : BlockStatement
    {
        public Node Condition;
        public ElseStatement Else;
    }

    internal class ElseStatement : BlockStatement { }
}
