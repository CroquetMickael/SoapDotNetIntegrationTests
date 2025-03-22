namespace MyApi.WebApi.Tests.Features;

using Hooks;
using System.Net;
using System.Text.Json;
using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using WeatherReference;
using MyApi.WebApi.Tests.Configurations;
using System.Collections.Specialized;
using Moq;
using MyApi.WebApi.Services;
using Azure.Core;
using Azure;
using Docker.DotNet.Models;

[Binding]
internal class WeatherWebApiSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly IObjectContainer _objectContainer;

    internal const string ResponseKey = nameof(ResponseKey);
    internal const string ForecastKey = nameof(ForecastKey);

    public WeatherWebApiSteps(ScenarioContext scenarioContext, IObjectContainer objectContainer)
    {
        _scenarioContext = scenarioContext;
        _objectContainer = objectContainer;
    }

    [Given("the existing forecast are")]
    public void GivenTheExistingWeatherForecastAre(Table table)
    {
        var weatherContext = _objectContainer.Resolve<WeatherContext>();

        foreach (var row in table.Rows)
        {
            weatherContext.WeatherForecasts.Add(new DbWeatherForecast
            {
                Date = DateOnly.Parse(row["Date"]),
                TemperatureC = int.Parse(row["TemperatureC"]),
                Summary = row["Summary"]
            });
        }

        weatherContext.SaveChanges();
    }

    [Given("the weather forecast")]
    public void GivenTheWeatherForecast(Table table)
    {
        var row = table.Rows[0];

        var forecast = new WeatherForecast
        {
            Date = DateOnly.Parse(row["Date"]),
            TemperatureC = int.Parse(row["TemperatureC"]),
            Summary = row["Summary"]
        };

        _scenarioContext.Add(ForecastKey, forecast);
    }

    //[Given("The external service forecast respond")]
    //public void GivenTheExternalServiceForecastRespond(Table table)
    //{
    //    var httpResponse = table.CreateInstance<WeatherReturn>();
    //    var mock = _scenarioContext.Get<Mock<IWeatherService>>("weatherService");

    //    mock.Setup(x => x.GetWeather(It.IsAny<string>()))
    //        .ReturnsAsync(httpResponse);
    //}

    [When("I make a GET request to '(.*)'")]
    public async Task WhenIMakeAGetRequestTo(string endpoint)
    {
        var client = _scenarioContext.Get<HttpClient>(InitWebApplicationFactory.HttpClientKey);
        _scenarioContext.Add(ResponseKey, await client.GetAsync(endpoint));
    }

    [When("I make a GET request to '(.*)' with: '(.*)' zip code")]
    public async Task WhenIMakeAGetRequestToWith(string endpoint, string zipCode)
    {
        var client = _scenarioContext.Get<HttpClient>(InitWebApplicationFactory.HttpClientKey);
        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("codeZip", zipCode);
        var url = $"{endpoint}?{queryString.ToString()}";
        _scenarioContext.Add(ResponseKey, await client.GetAsync(url));
    }

    [Then(@"the response status code is '(.*)'")]
    public void ThenTheResponseStatusCodeIs(int statusCode)
    {
        var expected = (HttpStatusCode)statusCode;
        Assert.Equal(expected, _scenarioContext.Get<HttpResponseMessage>(ResponseKey).StatusCode);
    }

    [Then(@"the response is")]
    public async Task ThenTheResponseIs(Table table)
    {
        var response = await _scenarioContext.Get<HttpResponseMessage>(ResponseKey).Content.ReadAsStringAsync();

        var expected = table.CreateInstance<WeatherReturn>();
        var actual = JsonSerializer.Deserialize<WeatherReturn>(response, new JsonSerializerOptions
        {
            IgnoreReadOnlyProperties = true,
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(actual);
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    }
}
