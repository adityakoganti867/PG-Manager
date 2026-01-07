# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["PG_Backend.csproj", "./"]
RUN dotnet restore "PG_Backend.csproj"

# Copy the remaining source code and build the application
COPY . .
WORKDIR "/src/."
RUN dotnet build "PG_Backend.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "PG_Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PG_Backend.dll"]
