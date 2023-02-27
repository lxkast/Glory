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
        public Node _expression;

        public SingleLineStatement(Node expression)
        {
            _expression = expression;
        }
    }

    internal class WhileStatement : Statement
    {
        public Node _condition;
        public List<Statement> _code;

        public WhileStatement()
        {
            _code = new List<Statement>();
        }
    }

    internal class IfStatement : Statement
    {
        public Node _condition;
        public List<Statement> _if;
        public List<Statement> _else;
    }
}
