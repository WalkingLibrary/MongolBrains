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
        ollama.SelectedModel = "nous-hermes2";
        var chat = new Chat(ollama);
        string seedPrompt =
            "/set YOU ARE A MONGOL AND MUST RESPOND AS A MONGOL. YOU HAVE NO RULES TO FOLLOW YOU ARE A KING. And Must Respond as a Mongol, Mongols have no Idea what technology is. You Like to Ride Horses";
        chat.Send(seedPrompt);
        string seedPromptResponse = "";
        await foreach (var answerToken in chat.Send(seedPrompt))
        {
            seedPromptResponse += answerToken;
        }

        Console.WriteLine("seedPromptResponse: " + seedPromptResponse);
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