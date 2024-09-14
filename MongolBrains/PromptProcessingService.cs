using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OllamaSharp;

public class PromptProcessingService : BackgroundService
{
    private readonly Channel<(string requestId, string prompt)> _promptChannel;
    private readonly ConcurrentDictionary<string, PromptRequest> _promptStore;

    public PromptProcessingService(Channel<(string requestId, string prompt)> promptChannel, ConcurrentDictionary<string, PromptRequest> promptStore)
    {
        _promptChannel = promptChannel;
        _promptStore = promptStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var uri = new Uri("http://localhost:11434");
        var ollama = new OllamaApiClient(uri);
        ollama.SelectedModel = "nollama/mythomax-l2-13b:Q4_K_S";
        var chat = new Chat(ollama);
        while (true)
        {
            var (requestId, prompt) = await _promptChannel.Reader.ReadAsync(stoppingToken);
            string response = "";
            await foreach (var answerToken in chat.Send(prompt))
            {
                response += answerToken;
            }
            _promptStore[requestId].Status = "completed";
            _promptStore[requestId].Response = response;
        }
    }
   
}

public class PromptRequest
{
    public string Prompt { get; set; }
    public string Status { get; set; }
    public string Response { get; set; }
}