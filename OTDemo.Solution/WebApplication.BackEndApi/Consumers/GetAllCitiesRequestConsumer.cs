using MassTransit;
using Weather.Libs.Models;
using Weather.Libs.Services;

namespace WebApplication.BackEndApi.Consumers;

public class GetAllCitiesRequestConsumer : 
    IConsumer<GetAllCitiesRequest>
{
    private WeatherService _weatherService;

    public GetAllCitiesRequestConsumer(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task Consume(ConsumeContext<GetAllCitiesRequest> context)
    {
        List<WeatherData>? allcities = await _weatherService.GetAllCitiesAsync();

        var result = new GetAllCitiesResponse
        {
            Data = allcities
        };

        await context.RespondAsync<GetAllCitiesResponse>(result);
    }
}