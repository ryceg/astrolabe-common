namespace Astrolabe.ColumnEditor;

public class ColumnContext<TDb>
{
    public TDb Entity { get; init; }
    
    public ColumnContext(TDb entity)
    {
        Entity = entity;
    }

    public bool Edited { get; set; }
    
    public IDictionary<string, object> Props { get; set; } = new Dictionary<string, object>();

    public ColumnContext<TDb> WithProp(string name, object prop)
    {
        Props[name] = prop;
        return this;
    }

    public T GetProp<T>(string propName, Func<T> initProp)
    {
        if (Props.TryGetValue(propName, out var prop))
        {
            return (T)prop;
        }

        var res = initProp();
        Props[propName] = res;
        return res;
    }
}