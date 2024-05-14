using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Astrolabe.Web.Common;

public class HideDevModeControllersConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var controllers = application.Controllers;
        var devControllers = controllers.Where(x => x.Attributes.OfType<DevModeAttribute>().Any()).ToList();
        devControllers.ForEach(x => controllers.Remove(x));
    }
}