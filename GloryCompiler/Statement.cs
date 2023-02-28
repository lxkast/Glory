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

    internal class WhileStatement : Statement
    {
        public Node Condition;
        public List<Statement> Code;
        public List<Variable> Vars;

        public WhileStatement()
        {
            Code = new List<Statement>();
            Vars = new List<Variable>();
        }
    }

    internal class IfStatement : Statement
    {
        public Node Condition;
        public List<Statement> If;
        public List<Variable> Vars;

        // Give the else its own variables as well??
        public List<Statement> Else;
    }
}
