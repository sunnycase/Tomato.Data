using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tomato.Plugin
{
    public class DebugProvider
    {
        public void writeln(object value)
        {
            Debug.WriteLine(value);
        }

        public void writeln(string value)
        {
            Debug.WriteLine(value);
        }

        public void writeln(string value, params object[] args)
        {
            Debug.WriteLine(value, args);
        }
    }
}
