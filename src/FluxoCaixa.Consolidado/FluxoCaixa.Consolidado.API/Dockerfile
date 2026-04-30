FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["src/FluxoCaixa.Shared/FluxoCaixa.Shared.Kernel/FluxoCaixa.Shared.Kernel.csproj", "src/FluxoCaixa.Shared/FluxoCaixa.Shared.Kernel/"]
COPY ["src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.Domain/FluxoCaixa.Consolidado.Domain.csproj", "src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.Domain/"]
COPY ["src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.Application/FluxoCaixa.Consolidado.Application.csproj", "src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.Application/"]
COPY ["src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.Infrastructure/FluxoCaixa.Consolidado.Infrastructure.csproj", "src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.Infrastructure/"]
COPY ["src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API/FluxoCaixa.Consolidado.API.csproj", "src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API/"]

RUN dotnet restore "src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API/FluxoCaixa.Consolidado.API.csproj"

COPY . .
WORKDIR "/src/src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API"
RUN dotnet build "FluxoCaixa.Consolidado.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FluxoCaixa.Consolidado.API.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser -D appuser && chown -R appuser /app
USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FluxoCaixa.Consolidado.API.dll"]
