FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the NuGet layer is cached until a project file changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY Library.Lending.Contracts/Library.Lending.Contracts.csproj Library.Lending.Contracts/
COPY Library.Api/Library.Api.csproj Library.Api/
RUN dotnet restore Library.Api/Library.Api.csproj

COPY Library.Lending.Contracts/ Library.Lending.Contracts/
COPY Library.Api/ Library.Api/
RUN dotnet publish Library.Api/Library.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is only here for the container health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Library.Api.dll"]