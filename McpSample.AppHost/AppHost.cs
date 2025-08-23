var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

var phi35 = ollama
    .AddModel("phi3.5");
    
var mcpServer = builder.AddProject<Projects.McpSample_McpServer>("mcpserver")
    .WithHttpHealthCheck("/health");

_ = builder.AddProject<Projects.McpSample_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(phi35)
    .WaitFor(phi35)
    .WithReference(mcpServer)
    .WaitFor(mcpServer);

builder.Build().Run();
