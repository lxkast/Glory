using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler.Representation
{
    internal class Variable
    {
        public GloryType Type;
        public string Name;
        public int Offset;
        public Variable(GloryType type, string name)
        {
            Type = type;
            Name = name;

        }
    }
}
