using Weather.Libs.Services;

namespace WebApplication.BackEndApi;

public class MyHostedService : IHostedService, IDisposable
{
    private readonly WeatherService _weatherService;
    
    public MyHostedService(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
         await _weatherService.GenerateRandomWeatherDataAsync();
    }

    private void DoWork(object? state)
    {
       
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
       
    }
}