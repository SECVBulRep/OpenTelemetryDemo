

using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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


builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Weather_forecast_service"))
            .AddMeter(WeatherMetrics.InstrumentSourceName)
            .AddInstrumentation<WeatherMetrics>()
            //.AddPrometheusExporter()
            .AddOtlpExporter(op =>
            {
                op.Endpoint = new Uri("http://localhost:4317");
                op.BatchExportProcessorOptions = new BatchExportActivityProcessorOptions()
                {
                    MaxQueueSize = 2048,      // Максимальный размер очереди
                    ScheduledDelayMilliseconds = 1000, // Интервал отправки (5 секунд)
                    ExporterTimeoutMilliseconds = 1000, // Таймаут экспорта (30 секунд)
                    MaxExportBatchSize = 512  // Максимальный размер пакета данных
                };
            });
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseOpenTelemetryPrometheusScrapingEndpoint("metrics");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();