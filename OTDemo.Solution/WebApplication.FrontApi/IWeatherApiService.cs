using Weather.Libs.Models;

namespace WebApplication.FrontApi;

public interface IWeatherApiService
{
    Task<WeatherData?> GetWeatherByCityAsync(string city);
    Task<List<WeatherData>?> GetAllCitiesAsync();
}