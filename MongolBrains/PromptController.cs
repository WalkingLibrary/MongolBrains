﻿using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace MongolBrains
{
[ApiController]
public class PromptController : ControllerBase
{
        private readonly Channel<(string requestId, string prompt)> _promptChannel;
        private readonly ConcurrentDictionary<string, PromptRequest> _promptStore;

        public PromptController(Channel<(string requestId, string prompt)> promptChannel, ConcurrentDictionary<string, PromptRequest> promptStore)
        {
            _promptChannel = promptChannel;
            _promptStore = promptStore;
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
        }
    }