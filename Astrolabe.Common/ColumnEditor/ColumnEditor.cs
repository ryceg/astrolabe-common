using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Astrolabe.Common.ColumnEditor;

public interface ColumnEditor<TEdit, TDb> : ColumnDbExtractor<TDb>
{
    public Dictionary<string, object> Attributes { get; }
    public Func<TEdit, ColumnContext<TDb>, Task<ColumnContext<TDb>>> Edit { get; }
    
    

}
