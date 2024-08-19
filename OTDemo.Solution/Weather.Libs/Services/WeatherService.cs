using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using StackExchange.Redis;
using Weather.Libs.Models;

namespace Weather.Libs.Services;

public class WeatherService
{
    private readonly IMongoCollection<WeatherData> _weatherCollection;
    private readonly IDatabase _redisDatabase;

    public WeatherService()
    {
        var client = new MongoClient("mongodb://admin:adminpassword@localhost:27017");
        var database = client.GetDatabase("WeatherDb");
        _weatherCollection = database.GetCollection<WeatherData>("WeatherData");

        var redis = ConnectionMultiplexer.Connect("localhost:6379,password=eYVX7EwVmmxKPCDmwMtyKVge8oLd2t81");
        _redisDatabase = redis.GetDatabase();
    }

    public async Task GenerateRandomWeatherDataAsync()
    {
        var cities = new[] { "Москва", "Санкт-Петербург", "Казань", "Новосибирск", "Екатеринбург", "Нижний Новгород", "Самара", "Омск", "Челябинск", "Ростов-на-Дону", "Уфа", "Волгоград", "Пермь", "Красноярск", "Воронеж", "Саратов", "Тольятти", "Краснодар", "Ижевск", "Ульяновск", "Барнаул", "Тюмень", "Иркутск", "Владивосток", "Ярославль", "Махачкала", "Хабаровск", "Оренбург", "Новокузнецк", "Кемерово", "Рязань", "Томск", "Астрахань", "Пенза", "Липецк", "Тула", "Курск", "Калининград", "Улан-Удэ", "Севастополь", "Ставрополь", "Магнитогорск", "Сочи", "Тверь", "Брянск", "Белгород", "Нижний Тагил", "Архангельск", "Вологда" };
        var random = new Random();
       
        await _weatherCollection.DeleteManyAsync(Builders<WeatherData>.Filter.Empty);

        var weatherDataList = new List<WeatherData>();

        foreach (var city in cities)
        {
            var weatherData = new WeatherData
            {
                Id = ObjectId.GenerateNewId().ToString(),
                City = city,
                Temperature = random.Next(-20, 40), 
                Humidity = random.Next(0, 100), 
                Condition = GenerateRandomCondition(random),
                Date = DateTime.Now
            };

            weatherDataList.Add(weatherData);
        }

        await _weatherCollection.InsertManyAsync(weatherDataList);
    }

    private string GenerateRandomCondition(Random random)
    {
        var conditions = new[] { "Sunny", "Rainy", "Cloudy", "Snowy", "Windy" };
        return conditions[random.Next(conditions.Length)];
    }

    public async Task<WeatherData?> GetWeatherByCityAsync(string city)
    {
       
        var cachedWeatherData = await _redisDatabase.StringGetAsync(city);
        if (cachedWeatherData.HasValue)
        {
            return BsonSerializer.Deserialize<WeatherData>(cachedWeatherData.ToString());
        }
        
        var filter = Builders<WeatherData>.Filter.Eq(w => w.City, city);
        WeatherData? weatherData = await _weatherCollection.Find(filter).FirstOrDefaultAsync();
        
        if (weatherData != null)
        {
            await _redisDatabase.StringSetAsync(city, weatherData.ToJson(), TimeSpan.FromMinutes(30)); 
          
        }
        return weatherData;
    }


    public async Task<List<WeatherData>?> GetAllCitiesAsync()
    {
        
        var cachedWeatherData = await _redisDatabase.StringGetAsync("all_cities");
        if (cachedWeatherData.HasValue)
        {
            return BsonSerializer.Deserialize<List<WeatherData>>(cachedWeatherData.ToString());
        }

        
        var weatherDataList = await _weatherCollection.Find(Builders<WeatherData>.Filter.Empty).ToListAsync();

        if (weatherDataList != null && weatherDataList.Count > 0)
        {
            await _redisDatabase.StringSetAsync("all_cities", weatherDataList.ToJson(), TimeSpan.FromMinutes(30)); // Кэш на 30 минут
        }

        return weatherDataList;
    }


}
