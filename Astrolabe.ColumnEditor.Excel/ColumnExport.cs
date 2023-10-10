using ClosedXML.Excel;

namespace Astrolabe.ColumnEditor.Excel;

public record ColumnExport<T>(string Header, Action<ColumnContext<T>, IXLCell> WriteCell);