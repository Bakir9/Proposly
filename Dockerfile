FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Proposly.Shared/Proposly.Shared.csproj src/Proposly.Shared/
COPY src/Proposly.Domain/Proposly.Domain.csproj src/Proposly.Domain/
COPY src/Proposly.Application/Proposly.Application.csproj src/Proposly.Application/
COPY src/Proposly.Infrastructure/Proposly.Infrastructure.csproj src/Proposly.Infrastructure/
COPY src/Proposly.API/Proposly.API.csproj src/Proposly.API/
RUN dotnet restore src/Proposly.API/Proposly.API.csproj
COPY . .
RUN dotnet publish src/Proposly.API/Proposly.API.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet Proposly.API.dll"]
