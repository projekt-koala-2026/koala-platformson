FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project file first to cache dependency resolution
COPY ["koala/koala.csproj", "./"]
RUN dotnet restore -r linux-musl-x64

# Copy source and publish a self-contained release build
COPY . .
RUN dotnet publish -c Release -r linux-musl-x64 --no-restore -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "koala.dll"]