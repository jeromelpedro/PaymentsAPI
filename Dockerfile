# Multi-stage build para .NET 9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PaymentsAPI/PaymentsAPI.csproj", "PaymentsAPI/"]
RUN dotnet restore "PaymentsAPI/PaymentsAPI.csproj"
COPY . .
WORKDIR "/src/PaymentsAPI"
RUN dotnet publish "PaymentsAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentsAPI.dll"]
