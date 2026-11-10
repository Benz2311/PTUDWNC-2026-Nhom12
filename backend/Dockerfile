# syntax=docker/dockerfile:1
# Multi-stage build for CulinaryBlog.Api (SRS CONS-009)
# Stage 1: Build using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching restore layer
COPY ["src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj", "src/CulinaryBlog.Domain/"]
COPY ["src/CulinaryBlog.Application/CulinaryBlog.Application.csproj", "src/CulinaryBlog.Application/"]
COPY ["src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj", "src/CulinaryBlog.Infrastructure/"]
COPY ["src/CulinaryBlog.Api/CulinaryBlog.API.csproj", "src/CulinaryBlog.Api/"]
COPY ["tools/CulinaryBlog.DbSeeder/CulinaryBlog.DbSeeder.csproj", "tools/CulinaryBlog.DbSeeder/"]
COPY ["CulinaryBlog.sln", "./"]

# Restore dependencies
RUN dotnet restore "src/CulinaryBlog.Api/CulinaryBlog.API.csproj"

# Copy remaining source code and publish
ARG CONFIGURATION=Release
COPY . .
WORKDIR "/src/src/CulinaryBlog.Api"
RUN --mount=type=secret,id=sixlabors_license,required=false \
    --mount=type=secret,id=sixlabors_key,required=false \
    if [ -f /run/secrets/sixlabors_license ]; then \
        dotnet publish "CulinaryBlog.API.csproj" -c $CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SixLaborsLicenseFile=/run/secrets/sixlabors_license; \
    elif [ -f /run/secrets/sixlabors_key ]; then \
        dotnet publish "CulinaryBlog.API.csproj" -c $CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SixLaborsLicenseKey="$(cat /run/secrets/sixlabors_key)"; \
    else \
        dotnet publish "CulinaryBlog.API.csproj" -c $CONFIGURATION -o /app/publish /p:UseAppHost=false; \
    fi

# Stage 2: Runtime using .NET 10 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]
