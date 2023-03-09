using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GloryCompiler
{
    internal class NativeFunctions
    {
        public List<NativeFunction> nativeFunctions = new List<NativeFunction> { 
        new NativeFunction(
            new List<Variable>{new Variable(new GloryType(GloryTypes.String) , "text") },
            "print",
            null
            ),
        new NativeFunction(
            new List<Variable>(),
            "input",
            new GloryType(GloryTypes.String))
        };
    }
}
