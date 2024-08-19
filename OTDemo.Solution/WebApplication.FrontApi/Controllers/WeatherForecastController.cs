using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Weather.Libs.Models;

namespace WebApplication.FrontApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private IWeatherApiService _weatherApiService;
    IRequestClient<GetAllCitiesRequest> _client;

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherApiService weatherApiService, IRequestClient<GetAllCitiesRequest> client)
    {
        _logger = logger;
        _weatherApiService = weatherApiService;
        _client = client;
    }

    
    [HttpGet("GetAllFromBackEndApi")]
    public async Task<List<WeatherData>?> GetAll()
    {
        var result = await _weatherApiService.GetAllCitiesAsync();
        return result;
    }
    
    [HttpGet("GetAllFromBackEndApiEventually")]
    public async Task<List<WeatherData>?> GetAllEventually()
    {
        var response = await _client.GetResponse<GetAllCitiesResponse>(new GetAllCitiesRequest());
        
       
        return response.Message.Data;
    }
    
    
    
    [HttpGet("GetByCityFromBackEndApi")]
    public async Task<WeatherData?> GetByCity(string city)
    {
        var result = await _weatherApiService.GetWeatherByCityAsync(city);
        return result;
    }
  
}