namespace Astrolabe.ColumnEditor;

public interface ColumnEditorBuilder<TEdit, TDb, T, T2> : ColumnEditor<TEdit, TDb>
{
    public Func<ColumnContext<TDb>, T2> GetDbValue { get; }
    
    
}