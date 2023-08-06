using System;
using System.Collections.Generic;

namespace Astrolabe.Common.ColumnEditor;

public class ColumnContext<TDB>
{
    public TDB Entity { get; init; }
    
    public ColumnContext(TDB entity)
    {
        Entity = entity;
    }

    public bool Edited { get; set; }
    
    public IDictionary<string, object> Props { get; set; } = new Dictionary<string, object>();

    public ColumnContext<TDB> WithProp(string name, object prop)
    {
        Props[name] = prop;
        return this;
    }

    public T GetProp<T>(string propName, Func<T> initProp)
    {
        if (Props.ContainsKey(propName))
        {
            return (T)Props[propName];
        }

        var res = initProp();
        Props[propName] = res;
        return res;
    }
}