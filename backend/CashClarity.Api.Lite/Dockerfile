FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CashClarity.Api/CashClarity.Api.csproj", "CashClarity.Api/"]
COPY ["CashClarity.Api.Lite/CashClarity.Api.Lite.csproj", "CashClarity.Api.Lite/"]
RUN dotnet restore "CashClarity.Api.Lite/CashClarity.Api.Lite.csproj"

COPY . .
RUN dotnet publish "CashClarity.Api.Lite/CashClarity.Api.Lite.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CashClarity.Api.Lite.dll"]
