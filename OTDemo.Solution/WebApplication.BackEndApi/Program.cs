using MassTransit;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
using Weather.Libs.Metrics;
using Weather.Libs.Services;
using WebApplication.BackEndApi;
using WebApplication.BackEndApi.Consumers;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddHostedService<MyHostedService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(GetAllCitiesRequestConsumer).Assembly);
    
    x.UsingRabbitMq((context,cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
        
        
    });
});


var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);


var clientSettings = MongoClientSettings.FromConnectionString("mongodb://admin:adminpassword@localhost:27017"); 
var options = new InstrumentationOptions { CaptureCommandText = true };
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber(options));
MongoClient mongoClient = new MongoClient(clientSettings);

builder.Services.AddSingleton<MongoClient>(mongoClient);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("BackEndService"))
            .AddSource("BackEndService")
            .SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
            .AddRedisInstrumentation(redisConnection, opt =>
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