using Astrolabe.Common.ColumnEditor;
using ClosedXML.Excel;

namespace Astrolabe.Common.Excel;

public static class ExportExcel
{
    public const string ExportKey = "Exporter";

    public static XLWorkbook WriteWorkbook<TEdit, TDb>(
        this EntityColumns<TEdit, TDb> entityColumns, string sheetName, List<TDb> rows)
    {
        var columnExports = entityColumns.Columns.SelectMany(x =>
        {
            return x.Attributes.TryGetValue(ExportKey, out var exporter)
                ? new[] { (ColumnExport<TDb>)exporter }
                : Enumerable.Empty<ColumnExport<TDb>>();
        }).ToList();
        return WriteWorkbookWithColumnExports(sheetName, rows, columnExports, entityColumns.InitContext);
    }

    public static XLWorkbook WriteWorkbookWithColumnExports<TDb>(string sheetName, IEnumerable<TDb> rows,
        ICollection<ColumnExport<TDb>> columnExports, Func<TDb, ColumnContext<TDb>> initContext)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(sheetName);
        ws.SheetView.FreezeRows(1);
        var cell = ws.FirstColumn().FirstCell();
        foreach (var columnExport in columnExports)
        {
            cell.SetValue(columnExport.Header);
            cell.Style.Fill.SetBackgroundColor(XLColor.DimGray);
            cell.Style.Font.SetFontColor(XLColor.White);
            cell = cell.CellRight();
        }

        var rowNum = 2;
        rows.ToList().ForEach(t =>
        {
            var cc = initContext(t);
            cell = ws.Row(rowNum).FirstCell();
            foreach (var columnExport in columnExports)
            {
                columnExport.WriteCell(cc, cell);
                cell = cell.CellRight();
            }

            rowNum++;
        });
        ws.Columns().AdjustToContents();
        return wb;
    }
}