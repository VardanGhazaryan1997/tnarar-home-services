# The API image. Build from the repository root (the build reads the repo's .editorconfig):
#   docker build -f backend/Dockerfile -t home-services-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first, from the project files only, so code changes reuse the cached packages.
COPY .editorconfig ./
COPY backend/global.json backend/Directory.Build.props backend/Directory.Packages.props backend/
COPY backend/src/HomeServices.Domain/HomeServices.Domain.csproj backend/src/HomeServices.Domain/
COPY backend/src/HomeServices.Application/HomeServices.Application.csproj backend/src/HomeServices.Application/
COPY backend/src/HomeServices.Infrastructure/HomeServices.Infrastructure.csproj backend/src/HomeServices.Infrastructure/
COPY backend/src/HomeServices.Api/HomeServices.Api.csproj backend/src/HomeServices.Api/
RUN dotnet restore backend/src/HomeServices.Api/HomeServices.Api.csproj

COPY backend/src backend/src
RUN dotnet publish backend/src/HomeServices.Api/HomeServices.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Listens on 8080 (IPv4 and IPv6, which Railway's private network uses). Runs as the image's non-root user.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "HomeServices.Api.dll"]
