using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Function : BlockStatement
    {
        public string _name;
        public TokenType _returnType;
        public List<Variable> _parameters;

        public Function(List<Variable> parameters, string name, TokenType returnType)
        {
            _parameters = parameters;
            _returnType = returnType;
            _name = name;
            Vars.AddRange(_parameters);
        }
    }
}
