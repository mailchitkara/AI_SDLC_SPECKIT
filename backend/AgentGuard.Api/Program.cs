using AgentGuard.Api.Endpoints;
using AgentGuard.Core;
using AgentGuard.Core.PolicyEngine;

var builder = WebApplication.CreateBuilder(args);

const string FrontendDevCorsPolicy = "FrontendDev";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendDevCorsPolicy, policy =>
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// V1 ships with no forbidden dependency relationships configured (FR-006, research.md §5).
builder.Services.AddSingleton(ForbiddenDependencyConfig.Empty);
builder.Services.AddSingleton<AgentGuardAnalyzer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendDevCorsPolicy);

app.MapPrRiskAnalysisEndpoint();

app.Run();

public partial class Program;
