using AstrolabeApp.Controllers;
using AstrolabeApp.Data;
using AstrolabeApp.Services;
using Astrolabe.Schemas.CodeGen;

namespace AstrolabeApp.Forms;

public class AppForms : FormBuilder<object?>
{
    public static readonly FormDefinition<object?>[] Forms =
    [
//#if (IncludeDemoData)
        // Tea Editor Form - for creating/editing tea orders
        Form<TeaEditorForm>("TeaEditorForm", "Tea Editor", null),
        Form<TeaViewPageForm>("TeaViewPageForm", "Tea View", null),
        // Tea Search Form - for searching and listing teas
        Form<TeaSearchForm>("TeaSearchForm", "Tea Search", null),
//#endif
    ];
}
