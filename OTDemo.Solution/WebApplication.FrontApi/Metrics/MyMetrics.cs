using System.Diagnostics.Metrics;
using Weather.Libs.Models;

namespace WebApplication.FrontApi.Metrics;

public class MyMetrics
{
    public static readonly string GlobalSystemName = Environment.MachineName;
    public static readonly string ApplicationName = AppDomain.CurrentDomain.FriendlyName;
    public static readonly string InstrumentSourceName = nameof(MyMetrics);


    private int _temperature;
    public Counter<int> SummaryRequestCounter { get; }

    public MyMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(InstrumentSourceName, "1.0.0");

        SummaryRequestCounter = meter.CreateCounter<int>(name: "weather get request",
            unit:"Requests",
            description:"number of requests to get weather for the city");


        meter.CreateObservableGauge<int>(name: "weather.forecast.temperature",
            observeValue: ()=> new Measurement<int>(_temperature),
            unit: "Celcius",
            description: "temperature today");

    }

    public void SetWeather(WeatherData weatherData) => _temperature = weatherData.Temperature;
}