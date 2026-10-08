FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BankingApi/BankingApi.csproj BankingApi/
RUN dotnet restore BankingApi/BankingApi.csproj
COPY BankingApi/ BankingApi/
RUN dotnet publish BankingApi/BankingApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BankingApi.dll"]
