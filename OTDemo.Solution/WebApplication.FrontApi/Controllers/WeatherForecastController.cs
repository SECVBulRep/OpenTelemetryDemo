using System.Diagnostics;
using System.Net;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Weather.Libs.Models;
using WebApplication.FrontApi.Metrics;

namespace WebApplication.FrontApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{

    private static readonly ActivitySource _activitySource =
        new ActivitySource(nameof(WeatherForecastController), "1.0.0");
    
    private IWeatherApiService _weatherApiService;
    IRequestClient<GetAllCitiesRequest> _client;
    private readonly MyMetrics _metrics;

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherApiService weatherApiService,
        IRequestClient<GetAllCitiesRequest> client, MyMetrics metrics)
    {
        _logger = logger;
        _weatherApiService = weatherApiService;
        _client = client;
        _metrics = metrics;
    }

    
    [HttpGet("GetAllFromBackEndApi")]
    public async Task<List<WeatherData>?> GetAll()
    {
        using var activity = _activitySource.StartActivity(nameof(GetAll));
        var result = await _weatherApiService.GetAllCitiesAsync();
        return result;
    }
    
    [HttpGet("GetAllFromBackEndApiEventually")]
    public async Task<List<WeatherData>?> GetAllEventually()
    {
        using var activity = _activitySource.StartActivity(nameof(GetAllEventually));
        var response = await _client.GetResponse<GetAllCitiesResponse>(new GetAllCitiesRequest());
        return response.Message.Data;
    }
    
    
    
    [HttpGet("GetByCityFromBackEndApi")]
    public async Task<WeatherData?> GetByCity(string city)
    {
        using var activity = _activitySource.StartActivity(nameof(GetByCity));
        try
        {
            WeatherData? result = await _weatherApiService.GetWeatherByCityAsync(city);
            activity?.AddEvent(new ActivityEvent($"weather for {city} is ready"));
            
            _metrics.SummaryRequestCounter.Add(1,new KeyValuePair<string, object?>("city",result.City));
            _metrics.SetWeather(result!);
            
            return result;
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            return null;
        }
    }
  
}