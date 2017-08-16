using System.Collections.Generic;
using JLisp.Parsing.Types;

namespace JLisp.Parsing
{
    public class Env
    {
        public static IList<Env> Enviornments = new List<Env>();
        private readonly Env _outer = null;
        private readonly Dictionary<string, JlValue> _data = new Dictionary<string, JlValue>();

        public Env(Env outer) { _outer = outer; Enviornments.Add( this ); }

        public Env(Env outer, JlList binds, JlList exprs) : this( outer ) {
            for ( int i = 0; i < binds.Count; i++ ) {
                string sym = ((JlSymbol)binds[i]).Name;
                if ( sym == "&" ) {
                    _data[((JlSymbol)binds[i+1]).Name] = exprs.Slice( i );
                    break;
                }
                _data[sym] = exprs[i];
            }
        }

        public Env Find(string key) {
            if ( _data.ContainsKey( key ) ) {
                return this;
            }
            return _outer?.Find( key );
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
