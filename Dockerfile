# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos solo lo necesario para restaurar/publicar (los proyectos publicables viven en src/).
COPY *.slnx ./
COPY src/ ./src/

RUN dotnet restore src/FundedEdge.Web/FundedEdge.Web.csproj
RUN dotnet publish src/FundedEdge.Web/FundedEdge.Web.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production

# Render inyecta el puerto en la variable PORT (por defecto 10000). Kestrel debe escuchar en
# 0.0.0.0:$PORT (no en localhost). El entrypoint expande PORT en tiempo de ejecución con fallback
# a 10000; `exec` deja a dotnet como PID 1 para que reciba correctamente las señales.
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "exec dotnet FundedEdge.Web.dll --urls http://0.0.0.0:${PORT:-10000}"]
