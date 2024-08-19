namespace Weather.Libs.Models;

public record GetAllCitiesRequest
{
}

public record GetAllCitiesResponse
{
    public List<WeatherData>? Data { get; set; }
}