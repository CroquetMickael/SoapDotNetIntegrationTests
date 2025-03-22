using Microsoft.AspNetCore.Mvc;
using MyApi.WebApi.Services;
using WeatherReference;

namespace MyApi.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly WeatherContext _weatherContext;
    private readonly IWeatherService _weatherService;

    public WeatherForecastController(WeatherContext weatherContext, IWeatherService weatherService)
    {
        _weatherContext = weatherContext;
        _weatherService = weatherService;
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        var allWeathers = _weatherContext.WeatherForecasts.ToList();

        return allWeathers.Select(dbWeather => new WeatherForecast
        {
            Date = dbWeather.Date,
            TemperatureC = dbWeather.TemperatureC,
            Summary = dbWeather.Summary
        });
    }

    [HttpGet]
    [Route("{date}")]
    public WeatherForecast? Get(DateOnly date)
    {
        var dbWeather = _weatherContext.WeatherForecasts.FirstOrDefault(w => w.Date == date);

        if (dbWeather == null)
        {
            return null;
        }

        return new WeatherForecast
        {
            Date = dbWeather.Date,
            TemperatureC = dbWeather.TemperatureC,
            Summary = dbWeather.Summary
        };
    }

    [HttpPost]
    public IActionResult Post([FromBody] WeatherForecast weatherForecast)
    {
        _weatherContext.WeatherForecasts.Add(new DbWeatherForecast
        {
            Date = weatherForecast.Date,
            TemperatureC = weatherForecast.TemperatureC,
            Summary = weatherForecast.Summary
        });

        _weatherContext.SaveChanges();

        return Ok();
    }

    [HttpGet]
    [Route("byZip")]
    public Task<WeatherReturn> GetWeatherByZip([FromQuery] string codeZip)
    {
        return _weatherService.GetWeather(codeZip);
    }
}
