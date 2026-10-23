FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . .

CMD ["dotnet", "test", "koala-integration-tests.csproj", "--logger", "trx"]