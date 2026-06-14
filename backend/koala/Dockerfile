FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY . ./
RUN dotnet restore
#FIXME: MAKE IT REALEASE ON REALEASE AND FIX ALL WARNINGS !!!
RUN dotnet publish -c Debug -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/public/InternalStorage/
RUN apt-get update && apt-get install -y curl

ENTRYPOINT ["dotnet", "koala.dll"]