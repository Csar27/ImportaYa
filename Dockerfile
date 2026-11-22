FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ImportaYa.Domain/*.csproj src/ImportaYa.Domain/
COPY src/ImportaYa.Application/*.csproj src/ImportaYa.Application/
COPY src/ImportaYa.Infrastructure/*.csproj src/ImportaYa.Infrastructure/
COPY src/ImportaYa.Api/*.csproj src/ImportaYa.Api/
RUN dotnet restore src/ImportaYa.Api/ImportaYa.Api.csproj

COPY src/ ./
RUN dotnet publish src/ImportaYa.Api/ImportaYa.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5080
ENV ASPNETCORE_URLS=http://+:5080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ImportaYa.Api.dll"]
