using Astrolabe.Common.Schema;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.TestTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<SchemaField> Get()
    {
        return new List<SchemaField> { new ScalarField("Cool", "Real Cool", FieldType.Bool, 
            Array.Empty<string>(), "", false, true, false, "", false, null, false, 
            Array.Empty<string>(), null) };
    }
}