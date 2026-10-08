FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BankingWeb/BankingWeb.csproj BankingWeb/
RUN dotnet restore BankingWeb/BankingWeb.csproj
COPY BankingWeb/ BankingWeb/
RUN dotnet publish BankingWeb/BankingWeb.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BankingWeb.dll"]
