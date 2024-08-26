

using System.Reflection;
using MassTransit;
using MassTransit.Logging;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
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
builder.Services.AddSingleton<WeatherMetrics>();

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

var redisConnect = ConnectionMultiplexer.Connect("localhost:6379");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FrontEndService"))
            .AddSource("FrontEndService")
            .AddSource(DiagnosticHeaders.DefaultListenerName)
            .SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRedisInstrumentation(redisConnect, opt =>
            {
                opt.Enrich = (activity, command) => activity.SetTag("redis.connection", "localhost:6379");
                opt.Enrich = (activity, command) => activity.SetTag("peer.service", "redis");
                opt.FlushInterval = TimeSpan.FromSeconds(1);
                opt.EnrichActivityWithTimingEvents = true;
            });
        
        tracerProviderBuilder.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://localhost:4317");
        });
    })
    .WithMetrics(builder =>
    {
        builder
            .AddMeter(WeatherMetrics.InstrumentSourceName)
            .SetExemplarFilter(ExemplarFilterType.TraceBased)
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
        
        builder.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://localhost:4317");
        });
             
    });


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