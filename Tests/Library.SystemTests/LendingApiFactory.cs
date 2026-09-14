// ai-touched
using Library.Api;
using Library.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Library.SystemTests;

public sealed class LendingApiFactory(LendingGrpcFactory lending, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<ApiEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("LendingService:Address", lending.Server.BaseAddress.ToString());
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handler => handler.PrimaryHandler = lending.Server.CreateHandler())));
    }
}
