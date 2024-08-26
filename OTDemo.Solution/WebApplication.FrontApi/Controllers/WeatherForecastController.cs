using System.Diagnostics;
using System.Net;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Weather.Libs.Metrics;
using Weather.Libs.Models;


namespace WebApplication.FrontApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly ActivitySource _activitySource =
        new ActivitySource(nameof(WeatherForecastController), "1.0.0");

    private IWeatherApiService _weatherApiService;
    IRequestClient<GetAllCitiesRequest> _client;
    private readonly WeatherMetrics _metrics;

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherApiService weatherApiService,
        IRequestClient<GetAllCitiesRequest> client, WeatherMetrics metrics)
    {
        _logger = logger;
        _weatherApiService = weatherApiService;
        _client = client;
        _metrics = metrics;
    }


    [HttpGet("GetAllFromBackEndApi")]
    public async Task<List<WeatherData>?> GetAll()
    {
        _metrics.SummaryRequestByCityCounter.Add(1, new KeyValuePair<string, object?>("city", "all"));
        var result = await _weatherApiService.GetAllCitiesAsync();
        return result;
    }

    [HttpGet("GetAllFromBackEndApiEventually")]
    public async Task<List<WeatherData>?> GetAllEventually(string? orderId)
    {
        if (!string.IsNullOrEmpty(orderId))
            Activity.Current.SetTag("orderId", orderId);

        _metrics.SummaryRequestByCityCounter.Add(1, new KeyValuePair<string, object?>("city", "all"));
        var response = await _client.GetResponse<GetAllCitiesResponse>(new GetAllCitiesRequest());
        return response.Message.Data;
    }


    [HttpGet("GetByCityFromBackEndApi")]
    public async Task<WeatherData?> GetByCity(string city)
    {
        try
        {
            WeatherData? result = await _weatherApiService.GetWeatherByCityAsync(city);
            _metrics.SummaryRequestByCityCounter.Add(1, new KeyValuePair<string, object?>("city", result?.City));
            _metrics.SetWeather(result!);
            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }
}