using System;
using System.Collections.Generic;
using System.Text;
using JLisp.Parsing.Types;

namespace JLisp.Parsing.Functions
{
    class Plus : JlFunction
    {
        public override JlValue Apply(JlList args) {
            return ((JlInteger)args.Nth(0)).Add((JlInteger)args.Nth(1));
        }
    }
    class Minus : JlFunction {
        public override JlValue Apply(JlList args) {
            return ((JlInteger) args.Nth(0)).Subtract((JlInteger) args.Nth(1));
        }
    }
    class Multiply : JlFunction {
        public override JlValue Apply(JlList args) {
            return ((JlInteger) args.Nth(0)).Multiply((JlInteger) args.Nth(1));
        }
    }
    class Divide : JlFunction {
        public override JlValue Apply(JlList args) {
            return ((JlInteger) args.Nth(0)).Divide((JlInteger) args.Nth(1));
        }
    }
}
