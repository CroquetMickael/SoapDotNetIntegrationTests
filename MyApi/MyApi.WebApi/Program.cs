using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MyApi.WebApi;
using MyApi.WebApi.Configurations;
using MyApi.WebApi.Services;
using System.ServiceModel;
using System.Web.Services.Description;
using WeatherReference;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.MapType<DateOnly>(() => new OpenApiSchema
{
    Type = "string",
    Format = "date"
}));

var connectionString = builder.Configuration.GetSection("ConnectionStrings")["WeatherContext"];

builder.Services.AddDbContext<WeatherContext>(options =>
    options.UseSqlServer(connectionString, providerOptions =>
    {
        providerOptions.EnableRetryOnFailure();
    }));


var weatherSoapConfigurationUrl = builder.Configuration.GetSection("WeatherSoapService")["BaseUrl"];

builder.Services.AddHttpClient<WeatherService>();

builder.Services.AddSingleton<IWeatherService>(provider => new WeatherService(provider.GetRequiredService<HttpClient>(), weatherSoapConfigurationUrl));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
