FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/CulinaryBlog.Api/CulinaryBlog.Api.csproj
RUN dotnet publish src/CulinaryBlog.Api/CulinaryBlog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.Api.dll"]
