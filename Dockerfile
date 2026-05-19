# School Inventory Manager - Docker build
# .NET 8 ASP.NET Core Razor Pages app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SchoolInventoryManager.csproj", "./"]
RUN dotnet restore "SchoolInventoryManager.csproj"

COPY . .
RUN dotnet publish "SchoolInventoryManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /app/App_Data/imports

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "SchoolInventoryManager.dll"]
