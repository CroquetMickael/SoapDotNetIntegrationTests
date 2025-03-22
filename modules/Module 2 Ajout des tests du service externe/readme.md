# Module 2: Ajout des tests du service meteo

Démarrer avec le projet du module précédent:

```
git clone https://github.com/CroquetMickael/SoapDotNetIntegrationTests.git --branch feature/module1
```

## Modification du gherkin

Dans le fichier `WeatherWebApi.feature`, nous allons ajouter un scénario pour effectuer ce nouvel appel de service.

```Gherkin
Scenario: Get weather from Zip code
    Given The external service forecast respond
        | Success | State | City      | Temperature | RelativeHumidity | description      |
        | true    | Iowa  | Iowa City | 15          | 90               | Iowa description |
    When I make a GET request to 'weatherforecast/byZip' with: '90' zip code
    Then the response status code is '200'
    And the response by zip code is
        | Success | State | City      | Temperature | RelativeHumidity | description      |
        | true    | Iowa  | Iowa City | 15          | 90               | Iowa description |
```

Puis dans le fichier `WeatherWebApi.cs`, nous ajoutons les steps associé à ce nouveau scénario.

Pour les Given:

```cs
    [Given("The external service forecast respond")]
    public void GivenTheExternalServiceForecastRespond(Table table)
    {
        var httpResponse = table.CreateInstance<WeatherReturn>();
        var mock = _scenarioContext.Get<Mock<IWeatherService>>("weatherService");

        mock.Setup(x => x.GetWeather(It.IsAny<string>()))
            .ReturnsAsync(httpResponse);
    }
```

Pour le `When`:

```cs
[When("I make a GET request to '(.*)' with: '(.*)' zip code")]
 public async Task WhenIMakeAGetRequestToWith(string endpoint, string zipCode)
 {
     var client = _scenarioContext.Get<HttpClient>(InitWebApplicationFactory.HttpClientKey);
     NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
     queryString.Add("codeZip", zipCode);
     var url = $"{endpoint}?{queryString.ToString()}";
     _scenarioContext.Add(ResponseKey, await client.GetAsync(url));
 }
```

Et pour finir le `Then`

```cs
[Then(@"the response by zip code is")]
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
```

## Ajout de la classe de de sérialisation Json pour simplifier notre comparaison de Json

Il faut ajouter une classe que l'on nommera `SerializerOptions.cs` dans un dossier `Configurations` dans le projet de test, une fois cela fait remplissez le avec :

```cs
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MyApi.WebApi.Tests.Configurations;

public static class SerializerOptions
{
    public static readonly JsonSerializerOptions SerializeOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
```

Cette classe nous permet simplement de modifier les configuration de notre Serializer / Deserializer JSON dans le cadre de notre test, cela dépendra aussi de vos projets, dans notre cas, nous testons simplement que nous avons les mêmes objet JSON de chaque côté mais pas forcément l'ordre.

## Ajustement du InitWebApplicationFactory

Nous allons modifier le hook de démarrage de nos tests pour permettre le mocking de notre service externe et donc de sa réponse.

```cs
    public static void ReplaceExternalServices(IServiceCollection services, ScenarioContext scenarioContext)
    {

        var mockApiSanteMoralPersonne = new Mock<IWeatherService>();

        services.AddTransient(provider => mockApiSanteMoralPersonne.Object);
        scenarioContext.TryAdd("weatherService", mockApiSanteMoralPersonne);
    }
```

Cette nouvelle méthode créer un Mock avec [Moq](https://github.com/devlooped/moq) et vient l'injecter en transient dans tout notre projet, nous devons préciser `mockApiSanteMoralPersonne.Object` pour qu'il sorte le type demandé par l'injection de dépendance sinon il enverrai toujours le type `Mock<IWeatherService>` hors nous voulons injecter `IWeatherService`.

Une fois cela fait, nous mettons à disposition dans notre scenarioContext la clé qui nous permettra d'utiliser l'objet de Mock pour surcharger les réponses de nos méthodes/fonction.

Et pour finir, nous ajoutons cette méthode `ReplaceExternalServices` dans notre `BeforeScenario`

```cs
    [BeforeScenario]
    public async Task BeforeScenario(ScenarioContext scenarioContext, IObjectContainer objectContainer)
    {
        await InitializeRespawnAsync();

        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    ReplaceLogging(services);
                    ReplaceDatabase(services, objectContainer);
                    ReplaceExternalServices(services, scenarioContext);
                });
            });


        var client = application.CreateClient();

        scenarioContext.TryAdd(HttpClientKey, client);
        scenarioContext.TryAdd(ApplicationKey, application);
    }
```

Lancez vos tests et vous devriez normalement avoir ce résultat:

IMAGE

```
git clone https://github.com/CroquetMickael/SoapDotNetIntegrationTests.git --branch feature/module2
```

[suivant >](../../modules/Module%203%20Usage%20de%20Microcks/readme.md)
