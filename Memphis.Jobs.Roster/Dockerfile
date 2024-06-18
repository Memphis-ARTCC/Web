#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Memphis.Jobs.Roster/Memphis.Jobs.Roster.csproj", "Memphis.Jobs.Roster/"]
COPY ["Memphis.Shared/Memphis.Shared.csproj", "Memphis.Shared/"]
RUN dotnet restore "./Memphis.Jobs.Roster/Memphis.Jobs.Roster.csproj"
COPY . .
WORKDIR "/src/Memphis.Jobs.Roster"
RUN dotnet build "./Memphis.Jobs.Roster.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Memphis.Jobs.Roster.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Memphis.Jobs.Roster.dll"]