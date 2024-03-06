namespace Astrolabe.EF.Search;

public record SimpleSearchDocument(string SearchableText, IEnumerable<FieldValue> FilterFields);