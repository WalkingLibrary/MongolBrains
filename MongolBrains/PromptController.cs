using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace MongolBrains
{
[ApiController]
public class PromptController : ControllerBase
{
        private readonly Channel<(string requestId, string prompt)> _promptChannel;
        private readonly ConcurrentDictionary<string, PromptRequest> _promptStore;
        private readonly string ApiKey; // Store the API key here
        private const string ApiUrl = "https://api.openai.com/v1/images/generations";

        public PromptController(Channel<(string requestId, string prompt)> promptChannel, ConcurrentDictionary<string, PromptRequest> promptStore)
        {
            _promptChannel = promptChannel;
            _promptStore = promptStore;
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            ApiKey = config["OpenAI:ApiKey"];
        }
    
     [HttpPost("api/submit-prompt")]
        public async Task<IActionResult> SubmitPrompt([FromBody] string prompt)
    {
        var requestId = Guid.NewGuid().ToString();
        String decodedPrompt = Encoding.UTF8.GetString(Convert.FromBase64String(prompt));
        Console.WriteLine(decodedPrompt);
       
            var promptRequest = new PromptRequest
        {
            Prompt = decodedPrompt,
            Status = "waiting",
            Response = null
            };
            _promptStore[requestId] = promptRequest;
    
            await _promptChannel.Writer.WriteAsync((requestId, decodedPrompt));
    
        return Ok(new { RequestId = requestId });
    }

    [HttpGet("api/prompt-status/{id}")]
    public IActionResult GetPromptStatus(string id)
    {
            if (!_promptStore.ContainsKey(id))
            return NotFound(new { Message = "Request not found." });
    
            var promptRequest = _promptStore[id];
    
        if (promptRequest.Status == "waiting")
        {
            return Ok(new { Status = "waiting" });
        }
        else if (promptRequest.Status == "completed")
        {
            return Ok(new { Status = "completed", Response = promptRequest.Response });
        }
        else if (promptRequest.Status == "failed")
        {
            return StatusCode(500, new { Status = "failed" });
        }
    
        return StatusCode(500, new { Status = "unknown" });
    }
    
    
    [HttpGet("api/image")]
    public async Task<IActionResult> GenerateImageAsync(string prompt)
    {
        using (var httpClient = new HttpClient())
        {
            // Set up headers
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            // Define request body
            var requestBody = new
            {
                model = "dall-e-3", // Specify DALL-E 3 model explicitly
                prompt = prompt,
                n = 1,             // Number of images to generate
                size = "1024x1024"   // Requested image size
            };

            // Serialize request body to JSON
            StringContent jsonContent = new StringContent(JsonSerializer.Serialize(requestBody));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Send POST request
            HttpResponseMessage response = await httpClient.PostAsync(ApiUrl, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.Content.ToString());
                Task<String> jsonResponse = response.Content.ReadAsStringAsync();
                JsonDocument parsedResponse = JsonDocument.Parse(jsonResponse.Result);

                // Extract image URL
                var imageUrl = parsedResponse.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("url")
                    .GetString();

                    return Ok(new { ImageUrl = imageUrl });
            }
            else
            {
                var errorResponse =  response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { Error = errorResponse });
            }
        }
    }
    
    
    
        }
    }