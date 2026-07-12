# Guía de despliegue automático en Render con PostgreSQL

Esta guía explica, de principio a fin, cómo llevar el código de **FundedEdge / TrackRecord**
desde la rama `main` de GitHub hasta una aplicación desplegada y funcionando en **Render**,
usando la instancia de **PostgreSQL** que ya está creada en Render.

Cubre dos planos:

1. **Qué hay que implementar** en el repositorio para que Render pueda construir y arrancar la app.
2. **Cómo funciona** el flujo de despliegue automático (auto-deploy) y **dónde se hostea**.

---

## 1. Contexto técnico del proyecto

| Elemento | Valor |
|---|---|
| Framework | .NET 10 (`net10.0`) |
| Tipo de app | ASP.NET Core + **Blazor Server** (`AddInteractiveServerComponents`) |
| Proyecto de arranque | `src/TrackRecord.Web` |
| ORM | Entity Framework Core con **Npgsql** (PostgreSQL) |
| Contexto de datos | `TrackRecordDbContext` (registrado con `AddDbContextFactory`) |
| Cadena de conexión | `ConnectionStrings:Default` |
| Migraciones | **Automáticas al arrancar** (`db.Database.MigrateAsync()`), salvo que `Database:AutoMigrate=false` |

Puntos que condicionan el despliegue:

- **Render no tiene runtime nativo de .NET.** Para desplegar una app .NET hay que usar un
  **Dockerfile** (Render construye la imagen y la ejecuta). Hoy el repo **no tiene Dockerfile**,
  así que hay que crearlo (ver sección 3).
- La app **migra la base de datos sola al arrancar**. Con la PostgreSQL de Render ya creada,
  no hace falta ningún paso manual de migración: al primer arranque se crean/actualizan las tablas.
- Los **keys de Data Protection** se persisten hoy en el disco local del contenedor
  (`LocalApplicationData`). En Render el disco es efímero: al redeployar se pierden. Esto invalida
  cookies/valores cifrados tras cada despliegue. Ver sección 6 para la solución (disco persistente).

---

## 2. Requisitos previos

- Repositorio en GitHub con el código en la rama **`main`** (ya existe).
- Cuenta en **Render** conectada a la organización/usuario de GitHub.
- Instancia de **PostgreSQL en Render ya creada** (ya existe). De ella necesitarás la
  **Internal Database URL**, que tiene esta forma:

  ```
  postgresql://USUARIO:PASSWORD@dpg-xxxxxxxxxxxx-a/NOMBREBD
  ```

  > La encuentras en Render → tu base de datos PostgreSQL → sección **Connections** →
  > **Internal Database URL**. Usa la *interna* (hostname `dpg-...-a` sin dominio ni puerto)
  > si el Web Service está en la **misma región** que la base de datos: es más rápida y no sale
  > a Internet. Si necesitaras conexión externa, usa la **External Database URL** (incluye
  > `...-a.REGION-postgres.render.com` y requiere SSL).

---

## 3. Qué hay que implementar en el repo

### 3.1. Dockerfile (obligatorio)

Crea un `Dockerfile` en la **raíz del repositorio** con build multi-etapa:

```dockerfile
# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos solo lo necesario para restaurar (mejor caché)
COPY *.slnx ./
COPY src/ ./src/
COPY tests/ ./tests/

RUN dotnet restore src/TrackRecord.Web/TrackRecord.Web.csproj
RUN dotnet publish src/TrackRecord.Web/TrackRecord.Web.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Render inyecta el puerto en la variable PORT. Kestrel debe escuchar ahí y en 0.0.0.0.
ENV ASPNETCORE_HTTP_PORTS=""
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

EXPOSE 10000
ENTRYPOINT ["dotnet", "TrackRecord.Web.dll"]
```

> **Puerto:** Render expone un único puerto y comunica su número por la variable de entorno
> `PORT` (por defecto `10000`). La app **debe** escuchar en `0.0.0.0:$PORT`, no en `localhost`.
> Lo anterior lo consigue con `ASPNETCORE_URLS`. Como alternativa, puedes fijar
> `ASPNETCORE_URLS=http://0.0.0.0:10000` y dejar el puerto de Render en 10000.

### 3.2. Cadena de conexión por variable de entorno (recomendado)

Hoy `appsettings.json` tiene la cadena de conexión **hardcodeada** (incluye usuario y contraseña
en claro). Para producción:

1. **Quita la contraseña de `appsettings.json`** (deja el bloque sin `Default` o con un valor
   de placeholder). No debe haber credenciales en el repositorio.
2. Configura la cadena en Render como variable de entorno. ASP.NET Core mapea las secciones con
   doble guion bajo, así que la variable es:

   ```
   ConnectionStrings__Default = <cadena de conexión>
   ```

`AddInfrastructure` ya lee `configuration.GetConnectionString("Default")`, por lo que esta
variable de entorno sobrescribe automáticamente el `appsettings.json` sin tocar código.

### 3.3. Formato de la cadena de conexión (¡importante!)

**Npgsql no acepta el formato URI `postgresql://...` directamente.** La Internal Database URL de
Render viene en formato URI, pero Npgsql espera formato **clave=valor**. Tienes dos opciones:

**Opción A — Convertir a formato clave=valor** (lo más simple; ponlo tal cual en
`ConnectionStrings__Default`):

```
Host=dpg-xxxxxxxxxxxx-a;Database=NOMBREBD;Username=USUARIO;Password=PASSWORD
```

Para conexión **externa** añade además:

```
;Ssl Mode=Require;Trust Server Certificate=true
```

**Opción B — Convertir el URI en código al arrancar.** Si prefieres pegar la URL tal cual de
Render, añade una conversión en `DependencyInjection.AddInfrastructure` antes de `UseNpgsql`:

```csharp
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    connectionString =
        $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};" +
        "Ssl Mode=Require;Trust Server Certificate=true";
}
```

> Elige **una** de las dos. La Opción A no toca código; la Opción B es más cómoda si vas a copiar
> URLs de Render tal cual.

### 3.4. (Opcional) `render.yaml` — Infraestructura como código

En lugar de configurar el servicio a mano en el panel, puedes declararlo en un `render.yaml` en la
raíz (Render lo detecta como **Blueprint**):

```yaml
services:
  - type: web
    name: fundededge-web
    runtime: docker
    dockerfilePath: ./Dockerfile
    region: frankfurt          # misma región que tu PostgreSQL
    plan: starter
    branch: main               # rama que dispara el auto-deploy
    autoDeploy: true           # deploy automático en cada push a main
    healthCheckPath: /
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ConnectionStrings__Default
        sync: false            # se rellena a mano (secreto), no se versiona
    disk:                      # persiste los keys de Data Protection (ver 6)
      name: dataprotection-keys
      mountPath: /home/app/.dataprotection-keys
      sizeGB: 1
```

Con `render.yaml` el servicio queda versionado en el repo y se crea/actualiza automáticamente.

---

## 4. Configuración del servicio en Render (paso a paso, vía panel)

Si no usas `render.yaml`, crea el servicio manualmente:

1. **Render Dashboard → New → Web Service.**
2. **Conecta el repositorio** de GitHub (`fundedEdgeTrading/FundedEdge`). Render pedirá permiso a
   GitHub la primera vez (instala la GitHub App de Render sobre el repo).
3. **Configuración del servicio:**
   - **Branch:** `main` → es la rama que disparará los despliegues.
   - **Runtime / Language:** `Docker` (detecta el `Dockerfile`).
   - **Region:** la **misma** que tu base de datos PostgreSQL (para usar la conexión interna).
   - **Instance type / Plan:** según necesidad (Starter para empezar).
   - **Auto-Deploy:** `Yes` (por defecto). Esto activa el despliegue automático desde `main`.
4. **Environment → Environment Variables**, añade:
   - `ConnectionStrings__Default` = cadena de conexión en formato clave=valor (sección 3.3).
   - `ASPNETCORE_ENVIRONMENT` = `Production`.
   - (Si usas la Opción A de puerto fijo) `ASPNETCORE_URLS` = `http://0.0.0.0:10000`.
   - Cualquier otro secreto que use la app (claves de IA, SMTP, Google OAuth, Stripe…).
5. **Health Check Path:** `/` (o una ruta de salud si la añades).
6. **Create Web Service.** Render hará el primer build y deploy.

---

## 5. Flujo completo: de `main` a producción

Este es el ciclo end-to-end una vez configurado:

```
Desarrollador                GitHub                    Render                         Usuarios
     │                         │                          │                              │
     │  git push / merge PR    │                          │                              │
     ├────────────────────────▶│  rama main actualizada   │                              │
     │                         │                          │                              │
     │                         │  1) CI (ci.yml):         │                              │
     │                         │     build + test +       │                              │
     │                         │     escaneo NuGet        │                              │
     │                         │                          │                              │
     │                         │  2) Webhook a Render ────▶│  Auto-Deploy disparado       │
     │                         │                          │                              │
     │                         │                          │  3) docker build (Dockerfile)│
     │                         │                          │  4) push imagen + arranque   │
     │                         │                          │  5) App arranca:             │
     │                         │                          │     - lee ConnectionStrings  │
     │                         │                          │       __Default (env var)    │
     │                         │                          │     - MigrateAsync() aplica  │
     │                         │                          │       migraciones EF Core    │
     │                         │                          │       sobre la PostgreSQL     │
     │                         │                          │  6) Health check OK           │
     │                         │                          │  7) Nueva versión en vivo ───▶│  HTTPS
```

Detalle de cada fase:

1. **Push/merge a `main`.** Tú (o un merge de PR) actualizáis `main`.
2. **CI de GitHub Actions** (`.github/workflows/ci.yml`) compila, ejecuta tests y escanea
   dependencias vulnerables. *No despliega* — solo valida. (Recomendado: exigir CI en verde antes
   de mergear a `main` mediante branch protection, para que solo se despliegue código que pasa CI.)
3. **Render recibe el webhook** de GitHub sobre `main` y, con Auto-Deploy activo, inicia un deploy.
4. **Build de la imagen Docker** usando el `Dockerfile` (restore → publish → imagen runtime).
5. **Arranque del contenedor.** La app:
   - Lee `ConnectionStrings__Default` desde las variables de entorno.
   - Conecta a la **PostgreSQL de Render** (por la red interna de Render).
   - Ejecuta `db.Database.MigrateAsync()` → **aplica las migraciones pendientes** automáticamente.
     (Si prefieres controlarlo, pon `Database__AutoMigrate=false` y aplica migraciones como paso
     aparte.)
6. **Health check.** Render comprueba que la ruta de salud responde antes de enrutar tráfico.
7. **Cambio sin downtime.** Render mantiene la versión anterior sirviendo hasta que la nueva está
   sana; entonces conmuta el tráfico. Si el deploy falla, mantiene la versión anterior.

---

## 6. Persistencia de los keys de Data Protection

`AddInfrastructure` persiste los keys en `LocalApplicationData`, que en Render es **efímero**.
Sin arreglarlo, tras cada deploy se invalidan cookies de sesión y valores cifrados
(`IntegrationSettings`). Soluciones:

- **Disco persistente de Render** (recomendado): añade un disco (ver `render.yaml` sección 3.4) y
  apunta ahí la persistencia de keys, p. ej. montándolo en `/home/app/.dataprotection-keys` y
  configurando `PersistKeysToFileSystem` a esa ruta (o vía variable de entorno que la app lea).
- **Persistir los keys en la propia base de datos** con
  `PersistKeysToDbContext<TrackRecordDbContext>()` (paquete
  `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`).

---

## 7. Migraciones de base de datos

- **Automáticas (por defecto):** el arranque llama a `MigrateAsync()`. En la primera puesta en
  marcha, esto **crea todas las tablas** en la PostgreSQL de Render. En cada deploy posterior,
  aplica solo las migraciones nuevas. No hay que hacer nada manual.
- **Control manual (opcional):** si tienes varias instancias o quieres evitar carreras al migrar,
  pon la variable `Database__AutoMigrate=false` y ejecuta las migraciones como un paso previo
  (por ejemplo un *pre-deploy command* o un job), con:

  ```
  dotnet ef database update --project src/TrackRecord.Infrastructure --startup-project src/TrackRecord.Web
  ```

---

## 8. Dónde se hostea

- **Aplicación web (Blazor Server):** se ejecuta como **Web Service** en la infraestructura de
  Render, dentro de un contenedor Docker gestionado por Render. Render le asigna una URL pública
  con **HTTPS y certificado TLS automático**, del tipo:

  ```
  https://fundededge-web.onrender.com
  ```

  (puedes añadir un dominio propio en Render → Settings → Custom Domains).
- **Base de datos:** la instancia **PostgreSQL gestionada de Render** (ya creada). La app se
  conecta a ella por la **red interna** de Render cuando ambos están en la misma región (por eso
  se usa la *Internal Database URL*).
- **Runtime:** un contenedor Linux con **.NET 10 ASP.NET Core runtime**, escuchando en el puerto
  que Render inyecta (`PORT`), detrás del balanceador/proxy TLS de Render.

---

## 9. Checklist de puesta en marcha

- [ ] Crear `Dockerfile` en la raíz (sección 3.1).
- [ ] Hacer que Kestrel escuche en `0.0.0.0:$PORT` (sección 3.1).
- [ ] Quitar credenciales de `appsettings.json` (sección 3.2).
- [ ] Definir `ConnectionStrings__Default` en Render en formato clave=valor (secciones 3.2/3.3).
- [ ] (Opcional) Añadir `render.yaml` para versionar la infraestructura (sección 3.4).
- [ ] Crear el Web Service en Render apuntando a `main` con Auto-Deploy (sección 4).
- [ ] Configurar variables de entorno de secretos (IA, SMTP, OAuth, Stripe…).
- [ ] Resolver la persistencia de Data Protection keys (sección 6).
- [ ] Hacer push a `main` y verificar el primer deploy y las migraciones en los logs de Render.
