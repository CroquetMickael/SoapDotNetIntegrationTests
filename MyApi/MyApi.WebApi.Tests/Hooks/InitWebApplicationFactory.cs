using Microsoft.AspNetCore.Mvc.Testing;
using TechTalk.SpecFlow;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BoDi;
using Respawn;
using MyApi.WebApi.Services;
using Microcks.Testcontainers;

namespace MyApi.WebApi.Tests.Hooks;

[Binding]
internal class InitWebApplicationFactory
{
    internal const string HttpClientKey = nameof(HttpClientKey);
    internal const string ApplicationKey = nameof(ApplicationKey);
    internal Uri _microcksUrl;
    private MicrocksContainer _microcksContainer = null!;


    private async Task CreateApiTestcontainer()
    {
        _microcksContainer = new MicrocksBuilder()
           .WithImage("quay.io/microcks/microcks-uber:1.10.0")
            .WithMainArtifacts("Mocks\\Weather\\weatherSoapMock.xml")
           .Build();
        await _microcksContainer.StartAsync();
        _microcksUrl = _microcksContainer.GetSoapMockEndpoint("WeatherSoap Mock", "1.0");
    }
    private static void ReplaceLogging(IServiceCollection services)
    {
        services.RemoveAll(typeof(ILogger<>));
        services.RemoveAll<ILogger>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private void ReplaceDatabase(IServiceCollection services, IObjectContainer objectContainer)
    {
        services.RemoveAll<DbContextOptions<WeatherContext>>();
        services.RemoveAll<WeatherContext>();

        services.AddDbContext<WeatherContext>(options =>
            options.UseSqlServer(DatabaseHook.MsSqlContainer.GetConnectionString(), providerOptions =>
            {
                providerOptions.EnableRetryOnFailure();
            }));

        var database = new WeatherContext(new DbContextOptionsBuilder<WeatherContext>()
            .UseSqlServer(DatabaseHook.MsSqlContainer.GetConnectionString())
            .Options);

        objectContainer.RegisterInstanceAs(database);
    }

    public static void ReplaceExternalServices(IServiceCollection services, string url)
    {
        services.AddHttpClient<WeatherService>();

        services.AddSingleton<IWeatherService>(provider => new WeatherService(provider.GetRequiredService<HttpClient>(), url));


        //var mockApiWeather = new Mock<IWeatherService>();

        //services.AddTransient(provider => mockApiWeather.Object);
        //scenarioContext.TryAdd("weatherService", mockApiWeather);

    }


    private async Task InitializeRespawnAsync()
    {
        var respawner = await Respawner.CreateAsync(
            DatabaseHook.MsSqlContainer.GetConnectionString(),
            new()
            {
                DbAdapter = DbAdapter.SqlServer
            });

        await respawner.ResetAsync(DatabaseHook.MsSqlContainer.GetConnectionString());
    }


    [BeforeScenario]
    public async Task BeforeScenario(ScenarioContext scenarioContext, IObjectContainer objectContainer)
    {
        await InitializeRespawnAsync();
        await CreateApiTestcontainer();
        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    ReplaceLogging(services);
                    ReplaceDatabase(services, objectContainer);
                    ReplaceExternalServices(services, _microcksUrl.AbsoluteUri);
                });
            });


        var client = application.CreateClient();

        scenarioContext.TryAdd(HttpClientKey, client);
        scenarioContext.TryAdd(ApplicationKey, application);
    }

    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TryGetValue(HttpClientKey, out var client) && client is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (scenarioContext.TryGetValue(ApplicationKey, out var application) && application is IDisposable disposableApplication)
        {
            disposableApplication.Dispose();
        }
    }

}
