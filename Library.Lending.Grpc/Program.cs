using Library.Lending.Application;
using Library.Lending.Grpc;
using Library.Lending.Grpc.Interceptors;
using Library.Lending.Infrastructure.Persistence;
using Library.Lending.Infrastructure;
using Library.Lending.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.Interceptors.Add<ExceptionHandlingInterceptor>();
});

builder.Services.AddGrpcHealthChecks()
    .AddDbContextCheck<LendingDbContext>("database");

var reflectionEnabled = builder.Configuration.GetValue<bool?>("Grpc:Reflection") ?? builder.Environment.IsDevelopment();
if (reflectionEnabled)
{
    builder.Services.AddGrpcReflection();
}

builder.Services.AddLendingApplication();
builder.Services.Configure<LendingOptions>(builder.Configuration.GetSection(LendingOptions.SectionName));
builder.Services.AddLendingInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGrpcService<BooksGrpcService>();
app.MapGrpcService<BorrowersGrpcService>();
app.MapGrpcService<LoansGrpcService>();
app.MapGrpcService<AnalyticsGrpcService>();
app.MapGrpcHealthChecksService();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Library Lending gRPC service (library.lending.v1). Talk to it with a gRPC client; HTTP health lives at /health.");

await app.Services.InitializeLendingDatabaseAsync();

await app.RunAsync();
