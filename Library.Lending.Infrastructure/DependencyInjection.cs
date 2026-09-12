using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Infrastructure.Persistence;
using Library.Lending.Infrastructure.Persistence.Analytics;
using Library.Lending.Infrastructure.Persistence.Repositories;
using Library.Lending.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Lending.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLendingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{DatabaseOptions.ConnectionStringName}' is not configured.");

        services.AddDbContext<LendingDbContext>(options => LendingDbContextOptions.Configure(options, connectionString));
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBorrowerRepository, BorrowerRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IAnalyticsQueries, AnalyticsQueries>();
        services.AddScoped<SampleDataSeeder>();

        return services;
    }
}
