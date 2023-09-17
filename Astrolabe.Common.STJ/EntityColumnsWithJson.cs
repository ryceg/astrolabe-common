using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using Astrolabe.Common.ColumnEditor;

namespace Astrolabe.Common.STJ;

public class EntityColumnsWithJson<TEDIT, TDB> : EntityColumns<TEDIT, TDB>
{
    private readonly Expression<Func<TDB, string>> _jsonFieldExpression;
    private readonly Func<Expression<Func<TDB, string>>, Type, string, (Expression<Func<TDB, object?>>, Type)> _jsonValueMethod;
    private Func<TEDIT, string, JsonValue?> GetEditJsonField { get; }

    private Func<TDB, string> GetJsonString { get; }

    private Action<TDB, string> SetJsonString { get; }

    private PropertyInfo JsonProperty { get; }

    protected EntityColumnsWithJson(Expression<Func<TDB, string>> jsonField,
        Func<TEDIT, string, JsonValue?> getEditJsonField,
        Func<Expression<Func<TDB, string>>, Type, string, (Expression<Func<TDB, object?>>, Type)> jsonValueMethod)
    {
        _jsonFieldExpression = jsonField;
        _jsonValueMethod = jsonValueMethod;
        GetJsonString = jsonField.Compile();
        JsonProperty = jsonField.GetPropertyInfo();
        var setObj = JsonProperty.GetSetMethod();
        Debug.Assert(setObj != null, "No setter for " + jsonField.Name + " on " + nameof(TDB));
        GetEditJsonField = getEditJsonField;
        SetJsonString = (db, jsonString) => setObj.Invoke(db, new object[] { jsonString });
    }

    public JsonColumnBuilder<TEDIT, TDB, T, T> AddJson<T>(string jsonField, Func<JsonValue?, T>? fromJson = null,
        Func<T, JsonValue?>? toJson = null, Func<JsonColumnBuilder<TEDIT, TDB, T, T>, JsonColumnBuilder<TEDIT, TDB, T, T>>? configure = null)
    {
        var getterDb = MakeDbJsonGetter(jsonField, fromJson);
        var getter = MakeJsonGetter<T>(jsonField);
        var setterDb = MakeDbJsonSetter(jsonField, toJson);
        var fieldPath = "$.\"" + jsonField + "\"";
        var (getterExpression, jsonType) = _jsonValueMethod(_jsonFieldExpression, typeof(T), fieldPath);
        var col = new JsonColumnBuilder<TEDIT, TDB, T, T>(jsonField, getterDb, getterExpression, jsonType, setterDb,
            (edit, ctx) =>
            {
                var existing = getterDb(ctx);
                var newVal = getter(edit);
                var changed = !Equals(existing, newVal);
                if (changed) setterDb(ctx, newVal);
                ctx.Edited |= changed;
                return Task.FromResult(ctx);
            }
        );
        if (configure != null) 
            col = configure(col);
        Columns.Add(col);
        return col;
    }

    public Func<TEDIT, T> MakeJsonGetter<T>(string jsonField)
    {
        return e =>
        {
            var srcField = GetEditJsonField(e, jsonField);
            try
            {
                return srcField != null ? srcField.GetValue<T>() : default;
            }
            catch (ArgumentException ae)
            {
                return default;
            }
        };
    }

    public override ColumnContext<TDB> InitContext(TDB entity)
    {
        var ctx = new ColumnContext<TDB>(entity);
        ctx.WithProp("GetJsonString", GetJsonString);
        return ctx;
    }

    private static JsonValue? GetJsonField(ColumnContext<TDB> context, string field)
    {
        return context.Json()[field]?.AsValue();
    }

    protected void SetJsonField(ColumnContext<TDB> context, string field, JsonValue? value)
    {
        context.Json()[field] = value;
        context.JsonEdited();
    }

    protected virtual Func<ColumnContext<TDB>, T> MakeDbJsonGetter<T>(string jsonField,
        Func<JsonValue?, T>? fromJson)
    {
        return db =>
        {
            var field = GetJsonField(db, jsonField);
            if (fromJson != null)
            {
                return fromJson(field);
            }

            try
            {
                return field != null ? field.GetValue<T>() : default;
            }
            catch (ArgumentException ae)
            {
                return default;
            }
        };
    }

    protected virtual Action<ColumnContext<TDB>, T> MakeDbJsonSetter<T>(string jsonField, Func<T, JsonValue?>? toJson)
    {
        return (db, newVal) => SetJsonField(db, jsonField, toJson != null ? toJson(newVal) :
            newVal != null ? JsonValue.Create(newVal) : null);
    }

    public async Task<ColumnContext<TDB>> EditIncludingJson(TEDIT edit, ColumnContext<TDB> ctx)
    {
        await Edit(edit, ctx);
        ApplyJsonEdits(ctx);
        return ctx;
    }

    public ColumnContext<TDB> ApplyJsonEdits(ColumnContext<TDB> ctx)
    {
        if (ctx.IsJsonEdited())
        {
            SetJsonString(ctx.Entity, ctx.Json().ToString());
        }

        return ctx;
    }
}

public static class ColumnContextExtensions
{
    public static IDictionary<string, JsonNode?> Json<TDB>(this ColumnContext<TDB> ctx)
    {
        return ctx.GetProp("Json", () => JsonColumnUtils.ParseJson(ctx.GetJsonString(ctx.Entity)));
    }

    private static string GetJsonString<TDB>(this ColumnContext<TDB> ctx, TDB entity)
    {
        return ((Func<TDB, string>)ctx.Props["GetJsonString"]).Invoke(entity);
    }


    public static void JsonEdited<TDB>(this ColumnContext<TDB> ctx)
    {
        ctx.WithProp("JsonEdited", true);
    }

    public static bool IsJsonEdited<TDB>(this ColumnContext<TDB> ctx)
    {
        return ctx.Props.ContainsKey("JsonEdited");
    }
}