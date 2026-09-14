using System.Reflection;
using Library.Lending.Application;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;

namespace Library.ArchitectureTests;

public class DesignRuleTests
{
    private static readonly Assembly Application = typeof(LendingOptions).Assembly;

    public static TheoryData<Type> Aggregates => [typeof(Book), typeof(Borrower), typeof(Loan)];

    [Theory]
    [MemberData(nameof(Aggregates))]
    public void Aggregates_expose_no_public_setters_and_no_public_constructors(Type aggregate)
    {
        aggregate.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetSetMethod(nonPublic: false) is not null)
            .Select(property => property.Name)
            .ShouldBeEmpty($"{aggregate.Name} must change state only through its own methods");

        aggregate.GetConstructors(BindingFlags.Public | BindingFlags.Instance).ShouldBeEmpty($"{aggregate.Name} must be created through a factory method");
    }

    [Theory]
    [MemberData(nameof(Aggregates))]
    public void Aggregates_derive_from_Entity_and_are_sealed(Type aggregate)
    {
        aggregate.IsSubclassOf(typeof(Entity)).ShouldBeTrue($"{aggregate.Name} must be an Entity so identity and equality are defined once");
        aggregate.IsSealed.ShouldBeTrue($"{aggregate.Name} is not designed for inheritance");
    }

    [Fact]
    public void Every_use_case_handler_is_internal_and_sealed()
    {
        var handlers = Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces().Any(IsHandlerInterface))
            .ToList();

        handlers.ShouldNotBeEmpty();
        handlers.ShouldAllBe(type => type.IsSealed && !type.IsPublic);
    }

    [Fact]
    public void Every_command_and_query_has_a_validator()
    {
        var requests = Application.GetExportedTypes()
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICommand<>) || i.GetGenericTypeDefinition() == typeof(IQuery<>))))
            .ToList();

        var validated = Application.GetTypes()
            .Where(type => type.BaseType is { IsGenericType: true } baseType && baseType.GetGenericTypeDefinition() == typeof(FluentValidation.AbstractValidator<>))
            .Select(type => type.BaseType!.GetGenericArguments()[0])
            .ToHashSet();

        requests.ShouldNotBeEmpty();
        requests.Where(request => !validated.Contains(request)).Select(request => request.Name).ShouldBeEmpty();
    }

    private static bool IsHandlerInterface(Type type) =>
        type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) || type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
}
