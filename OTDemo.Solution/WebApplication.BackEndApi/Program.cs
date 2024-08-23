using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
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

app.UseAuthorization();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapControllers();

app.Run();