using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Variable
    {
        public TokenType _type;
        public string _name;
        public Variable(TokenType type, string name)
        {
            _type = type;
            _name = name;
        }
    }
}
