using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Variable
    {
        public GloryType Type;
        public string Name;
        public Variable(GloryType type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}
