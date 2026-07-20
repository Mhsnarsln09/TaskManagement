FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src
COPY ["TaskManagement.sln", "Directory.Build.props", "./"]
COPY ["TaskManagement.Api/TaskManagement.Api.csproj", "TaskManagement.Api/"]
COPY ["TaskManagement.Application/TaskManagement.Application.csproj", "TaskManagement.Application/"]
COPY ["TaskManagement.Domain/TaskManagement.Domain.csproj", "TaskManagement.Domain/"]
COPY ["TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj", "TaskManagement.Infrastructure/"]
COPY ["TaskManagement.UnitTests/TaskManagement.UnitTests.csproj", "TaskManagement.UnitTests/"]
COPY ["TaskManagement.IntegrationTests/TaskManagement.IntegrationTests.csproj", "TaskManagement.IntegrationTests/"]
COPY ["TaskManagement.Api/", "TaskManagement.Api/"]
COPY ["TaskManagement.Application/", "TaskManagement.Application/"]
COPY ["TaskManagement.Domain/", "TaskManagement.Domain/"]
COPY ["TaskManagement.Infrastructure/", "TaskManagement.Infrastructure/"]
RUN dotnet restore TaskManagement.sln
RUN dotnet publish TaskManagement.Api/TaskManagement.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data/attachments && chown -R app:app /app
USER app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl --fail http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
