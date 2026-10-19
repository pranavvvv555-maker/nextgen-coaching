FROM node:22-bookworm-slim AS styles
WORKDIR /ui
COPY src/NextGenCoaching.Web/package*.json ./
RUN npm ci
COPY src/NextGenCoaching.Web/ ./
RUN npm run build:css

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/NextGenCoaching.Shared/NextGenCoaching.Shared.csproj src/NextGenCoaching.Shared/
COPY src/NextGenCoaching.Web/NextGenCoaching.Web.csproj src/NextGenCoaching.Web/
RUN dotnet restore src/NextGenCoaching.Web/NextGenCoaching.Web.csproj
COPY src/NextGenCoaching.Shared/ src/NextGenCoaching.Shared/
COPY src/NextGenCoaching.Web/ src/NextGenCoaching.Web/
COPY --from=styles /ui/wwwroot/css/app.output.css src/NextGenCoaching.Web/wwwroot/css/app.output.css
RUN dotnet publish src/NextGenCoaching.Web -c Release --no-restore -o /published

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /published .
RUN mkdir -p /app/state/keys && chown -R $APP_UID:$APP_UID /app/state
ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production DataProtection__KeyPath=/app/state/keys
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "NextGenCoaching.Web.dll"]
