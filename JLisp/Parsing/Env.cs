using System;
using System.Collections.Generic;
using System.Text;
using JLisp.Parsing.Types;

namespace JLisp.Parsing
{
    public class Env
    {
        private readonly Env _outer = null;
        private readonly Dictionary<string, JlValue> _data = new Dictionary<string, JlValue>();

        public Env(Env outer) { _outer = outer; }

        public Env(Env outer, JlList binds, JlList exprs) : this( outer ) {
            for ( int i = 0; i < binds.Size; i++ ) {
                string sym = ((JlSymbol)binds.Nth( i )).Name;
                if ( sym == "&" ) {
                    _data[((JlSymbol)binds.Nth( i )).Name] = exprs.Slice( i );
                    break;
                }
                else {
                    _data[sym] = exprs.Nth( i );

                }
            }
        }

        public Env Find(string key) {
            if ( _data.ContainsKey( key ) ) {
                return this;
            }
            else if ( _outer != null ) {
                return _outer.Find( key );
            }
            else {
                return null;
            }
        }

        public JlValue Get(string key) {
            Env e = Find( key );
            if ( e == null )
                throw new JlException( $"'{key}' not found" );
            return e._data[key];
        }

        public Env Set(string key, JlValue value) {
            _data[key] = value;
            return this;
        }
    }
}
