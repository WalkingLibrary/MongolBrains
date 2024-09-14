using System.Collections.Concurrent;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var promptChannel = Channel.CreateUnbounded<(string requestId, string prompt)>();
builder.Services.AddSingleton(promptChannel);

var promptStore = new ConcurrentDictionary<string, PromptRequest>();
builder.Services.AddSingleton(promptStore);

builder.Services.AddHostedService(provider => new PromptProcessingService(promptChannel, promptStore));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();