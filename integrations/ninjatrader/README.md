# AddOn de NinjaTrader 8 — TrackRecord Exporter

Envía cada ejecución (fill) de NinjaTrader 8 a TrackRecord en tiempo real, vía HTTP, para
cualquier cuenta conectada a NT8 (incluido Rithmic, el feed habitual de Apex/Lucid cuando no
usan Tradovate). Ver `GUIA_IMPLEMENTACION.md` §6 en la raíz del repo.

> Este AddOn **no forma parte de la solución .NET** (`TrackRecord.slnx`) y `dotnet build` no lo
> compila: depende de los ensamblados propios de NinjaTrader 8 (`NinjaTrader.Cbi`,
> `NinjaTrader.NinjaScript`), que no están en NuGet. Lo compila el propio NT8 al arrancar.

## Instalación

1. Copia `TrackRecordExporter.cs` a:
   ```
   Documents\NinjaTrader 8\bin\Custom\AddOns\TrackRecordExporter.cs
   ```
2. (Opcional pero recomendado) Define las variables de entorno de Windows antes de abrir
   NinjaTrader 8, para no tener que editar el `.cs`:
   - `TRACKRECORD_BASE_URL` — por defecto `http://localhost:5210`. Ajusta el puerto al que use
     tu instancia de TrackRecord.Web (`dotnet run` lo indica en la consola al arrancar).
   - `TRACKRECORD_API_KEY` — debe coincidir exactamente con `Ingest:NinjaTraderApiKey` configurado
     en TrackRecord (ver README.md de la raíz del repo). Sin ella, TrackRecord rechaza la ingesta
     con 401.
3. Abre NinjaTrader 8 → **Tools → Edit NinjaScript → AddOn...** → selecciona `TrackRecordExporter`
   → **Compile** (F5). Debe compilar sin errores; si falla, revisa la consola de salida (`Output`)
   de NT8 para el detalle.
4. Reinicia NinjaTrader 8 para que el AddOn se active (`OnStateChange` se ejecuta al arrancar).

## En TrackRecord

Cada cuenta que quieras sincronizar vía NT8 necesita `TradingAccount.ExternalAccountId` con el
**nombre exacto de la cuenta en NinjaTrader** (el que aparece en el Control Center de NT8 — p.ej.
`Sim101`, o el alias de tu cuenta real de Apex/Lucid conectada vía Rithmic). Configúralo al dar de
alta la cuenta o desde `/settings`.

## Resiliencia

Si TrackRecord no está levantado cuando llega un fill, el AddOn:
1. Intenta el POST HTTP inmediatamente.
2. Si falla (excepción de red o respuesta no 2xx), encola el payload en
   `%AppData%\TrackRecord\ninjatrader-export-queue.jsonl`.
3. Cada 30s, un timer interno reintenta enviar todo lo que quedó en la cola; lo que siga
   fallando se reencola para el siguiente intento.

La ingesta en TrackRecord es idempotente por `(Source, ExternalId)` — reenviar el mismo fill dos
veces (por un reintento, o por un reinicio de NT8) nunca duplica el registro.

## Verificación rápida

Con TrackRecord arrancado y el AddOn compilado, ejecuta un trade en modo Sim (`Sim101`) y
comprueba en `/accounts/{id}` que el trade aparece automáticamente en el journal, sin haberlo
introducido a mano.

## Limitaciones conocidas

- El mapeo `MarketPosition` ("Long"/"Short") → lado de la orden (Buy/Sell) asume la convención
  estándar de la API de ejecuciones de NT8; **verifícalo contra un fill real** antes de confiar
  en los datos para decisiones de negocio (ver Apéndice A de la guía).
- No hay reconexión automática si `Account.All` cambia después de `State.Configure` (p.ej. si
  añades una cuenta nueva a NT8 sin reiniciar el AddOn) — reinicia NT8 tras añadir cuentas.
