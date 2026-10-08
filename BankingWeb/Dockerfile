FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BankingWeb/BankingWeb.csproj BankingWeb/
RUN dotnet restore BankingWeb/BankingWeb.csproj
COPY BankingWeb/ BankingWeb/

# No --no-restore here: the earlier restore step only saw BankingWeb.csproj,
# before the Razor components existed. For this project's static web assets
# (notably the framework-provided _framework/blazor.web.js), publish must
# re-run restore now that the full source is present, or that script is
# silently missing from the output and the app has zero interactivity.
RUN dotnet publish BankingWeb/BankingWeb.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# Trust the private CA that signs the internal API's certificate
# (https://api.dev.banking.internal). Must run as root, before USER below.
COPY BankingWeb/certs/banking-root-ca.crt /usr/local/share/ca-certificates/banking-root-ca.crt
RUN update-ca-certificates
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BankingWeb.dll"]
