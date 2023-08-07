using Astrolabe.Common.ColumnEditor;
using ClosedXML.Excel;

namespace Astrolabe.Common.Excel;

public static class ExcelExtension
{
    public static ColumnEditorBuilder<TEdit, TDb, T, T2> WithExport<TEdit, TDb, T, T2>(
        this ColumnEditorBuilder<TEdit, TDb, T, T2> columnEditorBuilder, string headerColumn,
        Action<T2, IXLCell>? write = null)
    {
        var exporter = new ColumnExport<TDb>(headerColumn, ((db, cell) =>
        {
            var tVal = columnEditorBuilder.GetDbValue(db);
            if (write != null)
            {
                write(tVal, cell);
            }
            else
            {
                cell.SetValue(tVal);
            }
        }));
        columnEditorBuilder.Attributes[ExportExcel.ExportKey] = exporter;
        return columnEditorBuilder;
    }
}