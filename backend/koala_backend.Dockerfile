FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy the koala project folder into the container
COPY ["koala/", "koala/"]

# Restore and publish strictly the koala.csproj project
RUN dotnet restore "koala/koala.csproj" -r linux-musl-x64
RUN dotnet publish "koala/koala.csproj" -c Release -r linux-musl-x64 --no-restore -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# FIXME: do smth about the user permisisons so it does not run as a root
# RUN chown -R $APP_UID:$APP_UID /app
# USER $APP_UID

COPY --from=build /app/publish .

# cause csproj does not makr it to be copied to a publish dir
# COPY koala/appsettings.json /app/appsettings.json

EXPOSE 8080

ENTRYPOINT ["dotnet", "koala.dll"]