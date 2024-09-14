using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MongolBrains;

[ApiController]
public class PromptController : ControllerBase
{



    public string promptContext = "You Are a Discord Bot. I have hooked you up to the Mongoloid Khanate Discord Channel. A User Has Said the Following... Please do not mention you are a s ";
    
    
    [HttpPost("api/submit-prompt")]
    public IActionResult SubmitPrompt([FromBody] string prompt)
    {
        var requestId = Guid.NewGuid().ToString();
    
        // Store the request with "waiting for response" status
        PromptStore.Add(requestId, new PromptRequest
        {
            Prompt = prompt,
            Status = "waiting",
            Response = null
        });
    
        // Run the prompt in the background
        Task.Run(() => RunPromptThroughModel(requestId, promptContext +  prompt));
    
        return Ok(new { RequestId = requestId });
    }

    
    public static Dictionary<string, PromptRequest> PromptStore = new Dictionary<string, PromptRequest>();

    public class PromptRequest
    {
        public string Prompt { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
    }
    
    
    private void RunPromptThroughModel(string requestId, string prompt)
    {
        try
        {
            // Call your Ollama AI model here with the prompt
            string aiResponse = RunOllamaModel(prompt); // Placeholder for actual interaction with Ollama
        
            // Update the response and status in the store
            if (PromptStore.ContainsKey(requestId))
            {
                PromptStore[requestId].Response = aiResponse;
                PromptStore[requestId].Status = "completed";
            }
        }
        catch (Exception ex)
        {
            // Log the error and mark the request as failed
            PromptStore[requestId].Status = "failed";
        }
    }

    
    [HttpGet("api/prompt-status/{id}")]
    public IActionResult GetPromptStatus(string id)
    {
        if (!PromptStore.ContainsKey(id))
            return NotFound(new { Message = "Request not found." });
    
        var promptRequest = PromptStore[id];
    
        if (promptRequest.Status == "waiting")
        {
            return Ok(new { Status = "waiting for response" });
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

    
    private string RunOllamaModel(string prompt)
    {
        /*I need this to Become a Single Instance that w*/
        var psi = new ProcessStartInfo
        {
            FileName = "wsl", // Using 'wsl' to run the process in the Linux subsystem
            Arguments = $"-e bash -c \"ollama run gdisney/orca2-uncensored '{prompt}'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(psi))
        {
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            return output;
        }
    }
   


}