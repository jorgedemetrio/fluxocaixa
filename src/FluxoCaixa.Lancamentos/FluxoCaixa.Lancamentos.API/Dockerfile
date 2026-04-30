FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["src/FluxoCaixa.Shared/FluxoCaixa.Shared.Kernel/FluxoCaixa.Shared.Kernel.csproj", "src/FluxoCaixa.Shared/FluxoCaixa.Shared.Kernel/"]
COPY ["src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.Domain/FluxoCaixa.Lancamentos.Domain.csproj", "src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.Domain/"]
COPY ["src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.Application/FluxoCaixa.Lancamentos.Application.csproj", "src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.Application/"]
COPY ["src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.Infrastructure/FluxoCaixa.Lancamentos.Infrastructure.csproj", "src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.Infrastructure/"]
COPY ["src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj", "src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API/"]

RUN dotnet restore "src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj"

COPY . .
WORKDIR "/src/src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API"
RUN dotnet build "FluxoCaixa.Lancamentos.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FluxoCaixa.Lancamentos.API.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Non-root user for security
RUN adduser -D appuser && chown -R appuser /app
USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FluxoCaixa.Lancamentos.API.dll"]
