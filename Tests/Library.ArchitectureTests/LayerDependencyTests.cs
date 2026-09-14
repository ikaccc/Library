using System.Reflection;
using Library.Api;
using Library.Lending.Application;
using Library.Lending.Domain.Books;
using Library.Lending.Grpc;
using Library.Lending.Infrastructure;

namespace Library.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Book).Assembly;
    private static readonly Assembly Application = typeof(LendingOptions).Assembly;
    private static readonly Assembly Infrastructure = typeof(DatabaseOptions).Assembly;
    private static readonly Assembly Contracts = typeof(Lending.Contracts.V1.Book).Assembly;
    private static readonly Assembly GrpcHost = typeof(GrpcEntryPoint).Assembly;
    private static readonly Assembly HttpApi = typeof(ApiEntryPoint).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_but_the_base_class_library()
    {
        References(Domain).ShouldAllBe(name => name.StartsWith("System", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_knows_the_domain_but_no_persistence_or_transport_technology()
    {
        var references = References(Application);

        references.ShouldContain("Library.Lending.Domain");
        references.Where(IsSolutionAssembly).ShouldBe(["Library.Lending.Domain"]);
        references.ShouldNotContain(name => name.Contains("EntityFrameworkCore", StringComparison.Ordinal));
        references.ShouldNotContain(name => name.Contains("Npgsql", StringComparison.Ordinal));
        references.ShouldNotContain(name => name.Contains("Grpc", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_implements_application_ports_and_never_sees_the_transport()
    {
        var references = References(Infrastructure);

        references.Where(IsSolutionAssembly).ShouldBe(["Library.Lending.Application", "Library.Lending.Domain"], ignoreOrder: true);
        references.ShouldNotContain(name => name.Contains("Grpc", StringComparison.Ordinal));
    }

    [Fact]
    public void The_grpc_host_composes_the_service_and_never_references_the_http_api()
    {
        var references = References(GrpcHost);

        references.Where(IsSolutionAssembly).ShouldBe(
            ["Library.Lending.Application", "Library.Lending.Contracts", "Library.Lending.Domain", "Library.Lending.Infrastructure"],
            ignoreOrder: true);
    }

    [Fact]
    public void The_http_api_depends_only_on_the_contract_never_on_the_implementation()
    {
        References(HttpApi).Where(IsSolutionAssembly).ShouldBe(["Library.Lending.Contracts"]);
    }

    [Fact]
    public void The_contract_carries_its_version_in_the_namespace()
    {
        Contracts.GetExportedTypes()
            .Select(type => type.Namespace)
            .Distinct()
            .ShouldBe(["Library.Lending.Contracts.V1"]);
    }

    private static List<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name!).OrderBy(name => name, StringComparer.Ordinal).ToList();

    private static bool IsSolutionAssembly(string name) => name.StartsWith("Library.", StringComparison.Ordinal);
}
