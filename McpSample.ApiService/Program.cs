using McpSample.ApiService.Models;
using Scalar.AspNetCore;
using Asp.Versioning;
using Microsoft.SemanticKernel;
using OllamaSharp;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("api-version");
});

// Register OllamaApiClient using Aspire connection name
builder.AddOllamaApiClient("ollama");

// Register Semantic Kernel with Ollama chat completion
builder.Services.AddSingleton(sp =>
{
    var ollamaClient = sp.GetRequiredService<IOllamaApiClient>();

    var kernel = Kernel.CreateBuilder()
        .AddOllamaChatCompletion((OllamaApiClient)ollamaClient)
        .Build();

    return kernel;
});

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

// Define an API version set
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

// Minimal API endpoints for chatbot
var apiGroup = app.MapGroup("/api")
    .WithApiVersionSet(apiVersionSet)
    .WithTags("Chatbot");

apiGroup.MapPost("/chat", async (ChatRequest request, Kernel kernel) =>
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest("Message cannot be empty.");
        }

        var result = await kernel.InvokePromptAsync(request.Message);
        return Results.Ok(new ChatResponse(result.ToString()));
    })
    .WithName("Chat")
    .HasApiVersion(new ApiVersion(1, 0))
    .WithSummary("Chat with the chatbot.")
    .WithDescription("Sends a message to the chatbot and receives a response.");

app.Run();
