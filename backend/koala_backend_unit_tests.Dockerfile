FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . .

CMD ["dotnet", "test", "koala-unit-tests.csproj", "--logger", "trx"]