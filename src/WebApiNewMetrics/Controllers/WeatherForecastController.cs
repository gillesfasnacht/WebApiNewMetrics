using Microsoft.AspNetCore.Mvc;

namespace WebApiNewMetrics.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("weather/{city}")]
        public async Task<IActionResult> Get([FromRoute] string city, [FromQuery] int days = 5)
        {
            _logger.LogInformation("Gather weather information");

            if (Random.Shared.Next(1, 1000) == 10)
            {
                throw new Exception("Shit hit the fan");
            }

            var forecasts = Enumerable.Range(1, days).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            await Task.Delay(Random.Shared.Next(5, 100));

            _logger.LogInformation("City and forcasts {forecasts}", forecasts);

            return Ok(new WeatherForecastResponse
            {
                City = city,
                Forecasts = forecasts
            });
        }

        private class WeatherForecastResponse
        {
            public string? City { get; set; }
            public WeatherForecast[]? Forecasts { get; set; }
        }
    }
}