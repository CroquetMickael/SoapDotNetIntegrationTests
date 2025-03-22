using WeatherReference;

namespace MyApi.WebApi.Services;

public interface IWeatherService
{
    Task<WeatherReturn?> GetWeather(string codeZip);
}
