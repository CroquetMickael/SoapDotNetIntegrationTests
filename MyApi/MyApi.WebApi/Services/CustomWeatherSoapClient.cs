using System.ServiceModel;
using System.ServiceModel.Channels;
using WeatherReference;

namespace MyApi.WebApi.Services;

public class CustomWeatherSoapClient : WeatherSoapClient
{
    public CustomWeatherSoapClient(Binding binding, EndpointAddress endpoint) : base(binding, endpoint)
    {
    }

    public static CustomWeatherSoapClient Create(string url)
    {
        var binding = new BasicHttpBinding(); 
        var endpoint = new EndpointAddress(url);

        var client = new CustomWeatherSoapClient(binding, endpoint);

        return client;
    }
}
