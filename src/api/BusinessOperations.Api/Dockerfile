FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY BusinessOperations.sln ./
COPY src/api/BusinessOperations.Domain/BusinessOperations.Domain.csproj src/api/BusinessOperations.Domain/
COPY src/api/BusinessOperations.Application/BusinessOperations.Application.csproj src/api/BusinessOperations.Application/
COPY src/api/BusinessOperations.Infrastructure/BusinessOperations.Infrastructure.csproj src/api/BusinessOperations.Infrastructure/
COPY src/api/BusinessOperations.Api/BusinessOperations.Api.csproj src/api/BusinessOperations.Api/
RUN dotnet restore src/api/BusinessOperations.Api/BusinessOperations.Api.csproj
COPY src/api/ ./src/api/
RUN dotnet publish src/api/BusinessOperations.Api/BusinessOperations.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BusinessOperations.Api.dll"]
