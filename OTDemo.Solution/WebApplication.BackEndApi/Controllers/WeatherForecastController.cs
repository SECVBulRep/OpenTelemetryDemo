using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Weather.Libs.Models;
using Weather.Libs.Services;

namespace WebApplication.BackEndApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    
    private static readonly ActivitySource _activitySource =
        new ActivitySource(nameof(WeatherForecastController), "1.0.0");
    
    private WeatherService _weatherService;

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, WeatherService weatherService)
    {
        _logger = logger;
        _weatherService = weatherService;
    }

    //[HttpGet(Name = "GetWeatherForecast")]
    [HttpGet("GetAll")]
    public async Task<List<WeatherData>?> GetAll()
    {
        using var activity = _activitySource.StartActivity(nameof(GetAll));
        var result = await _weatherService.GetAllCitiesAsync();
        return result;
    }
    
   // [HttpGet(Name = "GetWeatherForecastForCity")]
   [HttpGet("GetByCity")]
    public async Task<WeatherData?> GetByCity(string city)
    {
        using var activity = _activitySource.StartActivity(nameof(GetByCity));
        activity?.SetTag("city", city);
        var result = await _weatherService.GetWeatherByCityAsync(city);
        return result;
    }
}