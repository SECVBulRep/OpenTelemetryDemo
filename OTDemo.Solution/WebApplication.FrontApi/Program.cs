

using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Weather.Libs.Metrics;
using WebApplication.FrontApi;


var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("WeatherApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5254/WeatherForecast/");
});

builder.Services.AddScoped<IWeatherApiService, WeatherApiService>();
builder.Services.AddSingleton<MyMetrics>();


builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context,cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder
        .AddService(MyMetrics.ApplicationName, serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["EnvironmentName"] = MyMetrics.GlobalSystemName
        })
    )
    .WithMetrics(providerBuilder => providerBuilder
        .AddMeter(MyMetrics.InstrumentSourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter(opt =>
        {
            
        })
    );



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseOpenTelemetryPrometheusScrapingEndpoint();


app.UseAuthorization();

app.MapControllers();

app.Run();