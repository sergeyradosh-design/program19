using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<WeatherApiClient>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Weather}/{action=Index}/{id?}");

app.Run();

public class WeatherInfoModel
{
    public string CityName { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
}

public class WeatherApiClient
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "YOUR_OPENWEATHERMAP_API_KEY";

    public WeatherApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherInfoModel?> GetWeatherAsync(string cityName)
    {
        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(cityName)}&units=metric&appid={ApiKey}&lang=ua";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>();
            if (data == null) return null;

            return new WeatherInfoModel
            {
                CityName = data.Name,
                Temperature = data.Main.Temp,
                Description = data.Weather.FirstOrDefault()?.Description ?? "Немає опису",
                Humidity = data.Main.Humidity,
                WindSpeed = data.Wind.Speed
            };
        }
        catch
        {
            return null;
        }
    }
}

public class OpenWeatherResponse
{
    public string Name { get; set; } = string.Empty;
    public MainData Main { get; set; } = new();
    public List<WeatherDescription> Weather { get; set; } = new();
    public WindData Wind { get; set; } = new();
}

public class MainData
{
    public double Temp { get; set; }
    public int Humidity { get; set; }
}

public class WeatherDescription
{
    public string Description { get; set; } = string.Empty;
}

public class WindData
{
    public double Speed { get; set; }
}

public class WeatherController : Controller
{
    private readonly WeatherApiClient _weatherClient;

    public WeatherController(WeatherApiClient weatherClient)
    {
        _weatherClient = weatherClient;
    }

    [HttpGet]
    public IActionResult Index(string? city, string? error)
    {
        var weatherCard = string.Empty;

        if (!string.IsNullOrWhiteSpace(city))
        {
    
            var weatherTask = _weatherClient.GetWeatherAsync(city);
            weatherTask.Wait();
            var weather = weatherTask.Result;

            if (weather != null)
            {
                weatherCard = $@"
                    <div class='card shadow-sm mt-4 border-success'>
                        <div class='card-body text-center'>
                            <h3 class='card-title text-success'>Погода в м. {weather.CityName}</h3>
                            <p class='fs-3 my-2'><strong>{weather.Temperature}°C</strong></p>
                            <p class='text-capitalize text-muted'>{weather.Description}</p>
                            <hr />
                            <div class='row'>
                                <div class='col-6'>Вологість: <strong>{weather.Humidity}%</strong></div>
                                <div class='col-6'>Швидкість вітру: <strong>{weather.WindSpeed} м/с</strong></div>
                            </div>
                        </div>
                    </div>";
            }
            else
            {
                error = "Не вдалося отримати дані про погоду для вказаного міста. Перевірте назву або спробуйте пізніше[cite: 15].";
            }
        }
        var errorHtml = !string.IsNullOrWhiteSpace(error) ? $"<div class='alert alert-danger mt-3'>{error}</div>" : "";

        var html = $@"
            <!DOCTYPE html>
            <html lang='uk'>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1'>
                <title>Прогноз погоди через Web API</title>
                <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
            </head>
            <body class='bg-light py-5'>
                <div class='container' style='max-width: 550px;'>
                    <div class='card shadow p-4'>
                        <h2 class='mb-4 text-center'>Погода в містах</h2>
                        
                        <form action='/Weather/Index' method='get'>
                            <div class='mb-3'>
                                <label class='form-label'>Введіть назву міста[cite: 15]:</label>
                                <input type='text' name='city' value='{city ?? ""}' class='form-control' placeholder='Наприклад, Київ' required />
                            </div>
                            <button type='submit' class='btn btn-primary w-100'>Отримати погоду</button>
                        </form>

                        {errorHtml}
                        {weatherCard}
                    </div>
                </div>
            </body>
            </html>";

        return Content(html, "text/html", Encoding.UTF8);
    }
}
