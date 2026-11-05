FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ backend/
RUN dotnet restore backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj
RUN dotnet publish backend/src/CulinaryBlog.Api/CulinaryBlog.API.csproj -c Debug -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]
