using Astrolabe.Schemas;
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
        return new List<SchemaField>
        {
            new SimpleSchemaField(FieldType.Bool.ToString(), "Cool")
            {
                DisplayName = "Real Cool"
            }
        };
    }
    
    [HttpPost("PostO")]
    public ControlDefinition PostControl(ControlDefinition controlDefinition)
    {
        return controlDefinition;
    }

}