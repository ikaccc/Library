using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Domain.Loans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Library.Lending.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLendingApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddOptions<LendingOptions>();
        services.AddSingleton<LoanPolicy>(provider => provider.GetRequiredService<IOptions<LendingOptions>>().Value.ToPolicy());
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidatingCommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidatingQueryHandler<,>));

        return services;
    }
}
