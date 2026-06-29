# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Accounting.sln ./
COPY src/Accounting.Api/Accounting.Api.csproj src/Accounting.Api/
COPY src/Accounting.Application/Accounting.Application.csproj src/Accounting.Application/
COPY src/Accounting.Domain/Accounting.Domain.csproj src/Accounting.Domain/
COPY src/Accounting.Infrastructure/Accounting.Infrastructure.csproj src/Accounting.Infrastructure/
RUN dotnet restore Accounting.sln
COPY . .
RUN dotnet publish src/Accounting.Api/Accounting.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081
RUN adduser --disabled-password --gecos "" appuser
COPY --from=build /app/publish .
USER appuser
EXPOSE 8081
ENTRYPOINT ["dotnet", "Accounting.Api.dll"]
