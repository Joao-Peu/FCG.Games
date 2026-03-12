FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FCG.Games.Api/FCG.Games.Api.csproj", "FCG.Games.Api/"]
COPY ["src/FCG.Games.Application/FCG.Games.Application.csproj", "FCG.Games.Application/"]
COPY ["src/FCG.Games.Domain/FCG.Games.Domain.csproj", "FCG.Games.Domain/"]
COPY ["src/FCG.Games.Infrastructure/FCG.Games.Infrastructure.csproj", "FCG.Games.Infrastructure/"]
RUN dotnet restore "FCG.Games.Api/FCG.Games.Api.csproj"
COPY src/ .
RUN dotnet publish "FCG.Games.Api/FCG.Games.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FCG.Games.Api.dll"]
