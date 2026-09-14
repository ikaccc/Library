using System.Text.Json.Serialization;
using Library.Api;
using Library.Api.Endpoints;
using Library.Api.Errors;
using Library.Api.Grpc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.AddObservability();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RpcExceptionHandler>();
builder.Services.AddOpenApi("v1");

builder.Services.AddLendingServiceClients(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<LendingServiceHealthCheck>("lending-service");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Library API"));
app.MapGet("/", () => TypedResults.Redirect("/scalar")).ExcludeFromDescription();

var v1 = app.MapGroup("/api/v1");
v1.MapBooks();
v1.MapBorrowers();
v1.MapLoans();
v1.MapAnalytics();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

await app.RunAsync();
