using AgentGuard.Api.Endpoints;
using AgentGuard.Api.GitHub;
using AgentGuard.Core;
using AgentGuard.Core.PolicyEngine;

var builder = WebApplication.CreateBuilder(args);

const string FrontendDevCorsPolicy = "FrontendDev";

// Render (and similar platforms) assign the listen port dynamically via PORT at container
// start — only known at runtime, so it can't be baked into an image-level ASPNETCORE_URLS.
// Left untouched for local development, where PORT is normally unset and launchSettings.json
// / --urls continues to control the port as before.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// FRONTEND_ORIGIN is set in render.yaml as a full URL; the scheme check is defensive in case
// it's ever set to a bare hostname instead.
var allowedOrigins = new List<string> { "http://localhost:5173", "http://127.0.0.1:5173" };
var frontendOrigin = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN");
if (!string.IsNullOrEmpty(frontendOrigin))
{
    allowedOrigins.Add(frontendOrigin.Contains("://") ? frontendOrigin : $"https://{frontendOrigin}");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendDevCorsPolicy, policy =>
        policy
            .WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// V1 ships with no forbidden dependency relationships configured (FR-006, research.md §5).
builder.Services.AddSingleton(ForbiddenDependencyConfig.Empty);
builder.Services.AddSingleton<AgentGuardAnalyzer>();

// GitHub requires a User-Agent on every request; base address keeps GitHubPullRequestClient's
// call sites relative (003-github-pr-import plan.md, research.md §1).
builder.Services.AddHttpClient<IGitHubPullRequestClient, GitHubPullRequestClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AgentGuard");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendDevCorsPolicy);

// Independent of AgentGuardAnalyzer/the rules pipeline, so a healthy response here means the
// process itself is up — suitable for Render's health check (FR-003).
app.MapHealthChecks("/health");

app.MapPrRiskAnalysisEndpoint();
app.MapPrReferenceAnalysisEndpoint();

app.Run();

public partial class Program;
