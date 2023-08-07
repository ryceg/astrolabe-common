using Astrolabe.Common.ColumnEditor;
using ClosedXML.Excel;

namespace Astrolabe.Common.Excel;

public record ColumnExport<T>(string Header, Action<ColumnContext<T>, IXLCell> WriteCell);