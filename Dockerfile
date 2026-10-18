# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props
COPY Directory.Build.props ./

# Copy project files and restore
COPY src/NextGenCoaching.Shared/NextGenCoaching.Shared.csproj src/NextGenCoaching.Shared/
COPY src/NextGenCoaching.Web/NextGenCoaching.Web.csproj src/NextGenCoaching.Web/
RUN dotnet restore src/NextGenCoaching.Web/NextGenCoaching.Web.csproj

# Copy all source files
COPY src/NextGenCoaching.Shared/ src/NextGenCoaching.Shared/
COPY src/NextGenCoaching.Web/ src/NextGenCoaching.Web/

# Publish web app
WORKDIR /src/src/NextGenCoaching.Web
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Cloud platforms (Render, Koyeb, Railway) set the PORT environment variable
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NextGenCoaching.Web.dll"]
