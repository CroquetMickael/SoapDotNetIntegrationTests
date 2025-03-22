using WeatherReference;

namespace MyApi.WebApi.Services;

public class WeatherService : IWeatherService
{

    private readonly CustomWeatherSoapClient _weatherSoapClient;

    public WeatherService(HttpClient httpClient, string soapUrl)
    {
        _weatherSoapClient = CustomWeatherSoapClient.Create(soapUrl);
    }


    public async Task<WeatherReturn?> GetWeather(string codeZip)
    {
        try
        {
            var response = await _weatherSoapClient.GetCityWeatherByZIPAsync(codeZip);
            return response;

        }
         catch (Exception ex)
        {
            
        }
        return null;
    }
}
