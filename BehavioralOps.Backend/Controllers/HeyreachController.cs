using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace BehavioralOps.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeyreachController : ControllerBase
{
  private readonly IConfiguration _config;
  private readonly string _apiKey;

  public HeyreachController(IConfiguration config)
  {
    _config = config;
    // In a real app, this should be in appsettings.json or an environment variable
    _apiKey = _config["Heyreach:ApiKey"] ?? "";
  }

  [HttpGet("status")]
  public async Task<IActionResult> GetStatus()
  {
    try
    {
      using var http = new HttpClient();
      http.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

      var response = await http.GetAsync("https://api.heyreach.io/api/public/auth/CheckApiKey");
      if (response.IsSuccessStatusCode)
      {
        return Ok(new { status = "connected", message = "API Key is valid" });
      }

      return StatusCode((int)response.StatusCode, new { status = "error", message = "API Key check failed" });
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { status = "error", message = ex.Message });
    }
  }

  [HttpGet("campaign/{id}")]
  public async Task<IActionResult> GetCampaign(long id)
  {
    try
    {
      using var http = new HttpClient();
      http.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

      var response = await http.GetAsync($"https://api.heyreach.io/api/public/campaign/GetById?campaignId={id}");
      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        return Ok(data);
      }

      var error = await response.Content.ReadAsStringAsync();
      return StatusCode((int)response.StatusCode, new { status = "error", message = error });
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { status = "error", message = ex.Message });
    }
  }
}
