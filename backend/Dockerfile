FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["CashClarity.Api/CashClarity.Api.csproj", "CashClarity.Api/"]
RUN dotnet restore "CashClarity.Api/CashClarity.Api.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "CashClarity.Api/CashClarity.Api.csproj" -c Release -o /app/build
RUN dotnet publish "CashClarity.Api/CashClarity.Api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "CashClarity.Api.dll"] 