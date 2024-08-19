namespace WebApplication.FrontApi;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Weather.Libs.Models;

public class WeatherApiService : IWeatherApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<WeatherData?> GetWeatherByCityAsync(string city)
    {
        var client = _httpClientFactory.CreateClient("WeatherApiClient");
        HttpResponseMessage response = await client.GetAsync($"GetByCity?city={city}");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WeatherData>();
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            return null;
        }
    }

    public async Task<List<WeatherData>?> GetAllCitiesAsync()
    {
        var client = _httpClientFactory.CreateClient("WeatherApiClient");
        HttpResponseMessage response = await client.GetAsync("GetAll");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<WeatherData>>();
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            return null;
        }
    }
}