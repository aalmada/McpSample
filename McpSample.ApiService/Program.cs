using System.Text.Json;
using McpSample.ApiService.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Scalar.AspNetCore;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("api-version");
});

// Register chatbot service
builder.Services.AddHttpClient();
// builder.Services.AddScoped<McpSample.ApiService.Services.IChatbotService, McpSample.ApiService.Services.ChatbotService>();

// Register OllamaSharp client using Aspire connection name
builder.AddOllamaApiClient("ollama");

var app = builder.Build();

app.MapOpenApi();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference("/api-reference", options => options
        .WithTitle("Backend API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
    );
}

// Map endpoints
app.MapDefaultEndpoints();

// Define an API version set using the builder extension
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var chatGroup = app.MapGroup("/api/chat")
    .WithApiVersionSet(apiVersionSet)
    .WithTags("Chatbot");

// Minimal API endpoints for chatbot
chatGroup.MapPost("stream",
    async (HttpContext context, Kernel kernel) =>
    {
        var userMessage = await JsonSerializer.DeserializeAsync<ChatInput>(context.Request.Body);

        if (userMessage == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid or missing chat input.");
            return;
        }

        context.Response.Headers.Append("Content-Type", "text/event-stream");

        var agent = new ChatCompletionAgent
        {
            Instructions = "You are a helpful assistant.",
            Kernel = kernel
        };

        var thread = new ChatHistoryAgentThread();
        var message = new ChatMessageContent(AuthorRole.User, userMessage.Text);

        await foreach (var chunk in agent.InvokeStreamingAsync(message, thread))
        {
            var data = chunk.Message.Content?.Replace("\n", "\\n"); // Escape newlines for SSE
            if (data is not null)
            {
                await context.Response.WriteAsync($"data: {data}\n\n");
            }
            await context.Response.Body.FlushAsync();
        }
    })
    .WithName("ChatbotChat")
    .HasApiVersion(new ApiVersion(1, 0))
    .WithSummary("Chat with the chatbot.")
    .WithDescription("Sends a message to the chatbot and receives a response.");

app.Run();
