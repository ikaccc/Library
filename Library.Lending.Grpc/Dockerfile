FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the NuGet layer is cached until a project file changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY Library.Lending.Domain/Library.Lending.Domain.csproj Library.Lending.Domain/
COPY Library.Lending.Application/Library.Lending.Application.csproj Library.Lending.Application/
COPY Library.Lending.Infrastructure/Library.Lending.Infrastructure.csproj Library.Lending.Infrastructure/
COPY Library.Lending.Contracts/Library.Lending.Contracts.csproj Library.Lending.Contracts/
COPY Library.Lending.Grpc/Library.Lending.Grpc.csproj Library.Lending.Grpc/
RUN dotnet restore Library.Lending.Grpc/Library.Lending.Grpc.csproj

COPY Library.Lending.Domain/ Library.Lending.Domain/
COPY Library.Lending.Application/ Library.Lending.Application/
COPY Library.Lending.Infrastructure/ Library.Lending.Infrastructure/
COPY Library.Lending.Contracts/ Library.Lending.Contracts/
COPY Library.Lending.Grpc/ Library.Lending.Grpc/
RUN dotnet publish Library.Lending.Grpc/Library.Lending.Grpc.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is only here for the container health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 5001 5002
ENTRYPOINT ["dotnet", "Library.Lending.Grpc.dll"]