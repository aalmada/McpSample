// Create a WebApplication builder with command-line arguments
var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Register services for handling problem details (standardized error responses)
builder.Services.AddProblemDetails();

// Register the MCP server and configure it with HTTP transport and tools from the current assembly
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Build the WebApplication instance
var app = builder.Build();

// Configure the HTTP request pipeline.

// Use a global exception handler to return standardized error responses
app.UseExceptionHandler();

// Map default endpoints for health checks, etc.
app.MapDefaultEndpoints();

// Map MCP-specific endpoints
app.MapMcp();

// Start the application and listen for incoming HTTP requests
app.Run();
