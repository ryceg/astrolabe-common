using Astrolabe.SearchState;

namespace AstrolabeApp.Forms;

public record TeaSearchPage(SearchOptions Request, SearchResults<TeaInfo> Results);
