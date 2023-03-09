using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class Function : BlockStatement
    {
        public string Name;
        public GloryType ReturnType;
        public List<Variable> Parameters;

        public Function(List<Variable> parameters, string name, GloryType returnType)
        {
            Parameters = parameters;
            ReturnType = returnType;
            Name = name;
            Vars.AddRange(Parameters);
        }
    }

    internal class NativeFunction
    {
        public string Name;
        public GloryType ReturnType;
        public List<Variable> Parameters;

        public NativeFunction(List<Variable> parameters, string name, GloryType returnType)
        {
            Parameters = parameters;
            ReturnType = returnType;
            Name = name;
        }
    }
}
