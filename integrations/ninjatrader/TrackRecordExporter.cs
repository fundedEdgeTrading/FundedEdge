// TrackRecordExporter.cs — AddOn de NinjaTrader 8 que empuja cada ejecución a TrackRecord.
//
// Este archivo vive FUERA de la solución .NET (no lo compila `dotnet build`): depende de
// ensamblados propios de NinjaTrader 8 (NinjaTrader.Cbi, NinjaTrader.NinjaScript) que solo
// existen dentro de la instalación de NT8, no están en NuGet, y NT8 lo compila con su propio
// compilador interno al arrancar. Ver README.md de esta carpeta para instrucciones de instalación.
//
// Corresponde a GUIA_IMPLEMENTACION.md §6, Opción A.

#region Using declarations
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Timers;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
    public class TrackRecordExporter : AddOnBase
    {
        // --- Configuración: edita estos valores o defínelos como variables de entorno del ---
        // --- usuario de Windows (TRACKRECORD_BASE_URL / TRACKRECORD_API_KEY) antes de abrir NT8. ---
        private static readonly string BaseUrl =
            Environment.GetEnvironmentVariable("TRACKRECORD_BASE_URL") ?? "http://localhost:5210";
        private static readonly string ApiKey =
            Environment.GetEnvironmentVariable("TRACKRECORD_API_KEY") ?? "";

        private static readonly string QueueFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrackRecord", "ninjatrader-export-queue.jsonl");

        private HttpClient _http;
        private System.Timers.Timer _retryTimer;
        private readonly object _queueLock = new object();

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "TrackRecord Exporter";
                Description = "Envía cada ejecución a TrackRecord vía HTTP para el track record automático.";
            }
            else if (State == State.Configure)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(QueueFilePath));

                _http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };
                if (!string.IsNullOrEmpty(ApiKey))
                {
                    _http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
                }

                lock (Account.All)
                {
                    foreach (var account in Account.All)
                    {
                        account.ExecutionUpdate += OnExecutionUpdate;
                    }
                }

                // Reintenta la cola local cada 30s: cubre el caso de que TrackRecord no esté
                // levantado cuando llega un fill (ver GUIA_IMPLEMENTACION.md §6 "Resiliencia").
                _retryTimer = new System.Timers.Timer(30_000);
                _retryTimer.Elapsed += (s, e) => FlushQueue();
                _retryTimer.AutoReset = true;
                _retryTimer.Start();
            }
            else if (State == State.Terminated)
            {
                lock (Account.All)
                {
                    foreach (var account in Account.All)
                    {
                        account.ExecutionUpdate -= OnExecutionUpdate;
                    }
                }

                _retryTimer?.Stop();
                _retryTimer?.Dispose();
                _http?.Dispose();
            }
        }

        private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
        {
            var payload = BuildPayload(e.Execution);
            _ = SendOrEnqueueAsync(payload);
        }

        private string BuildPayload(Execution execution)
        {
            // MarketPosition en una ExecutionEventArgs de NT8 refleja el sentido del propio fill
            // ("Long"/"Short"), no la posición resultante — TrackRecord lo mapea a Buy/Sell.
            var payload = new
            {
                externalId = execution.ExecutionId,
                accountName = execution.Account.Name,
                symbol = execution.Instrument.FullName,
                side = execution.MarketPosition.ToString(),
                quantity = execution.Quantity,
                price = (double)execution.Price,
                executedAt = execution.Time.ToString("O"),
                commission = (double)execution.Commission,
            };

            return JsonSerializer.Serialize(payload);
        }

        private async System.Threading.Tasks.Task SendOrEnqueueAsync(string jsonPayload)
        {
            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/ingest/ninjatrader/executions", content);

                if (!response.IsSuccessStatusCode)
                {
                    Enqueue(jsonPayload);
                }
            }
            catch (Exception)
            {
                // TrackRecord caído/inalcanzable: se encola en disco y se reintenta más tarde.
                Enqueue(jsonPayload);
            }
        }

        private void Enqueue(string jsonPayload)
        {
            lock (_queueLock)
            {
                File.AppendAllText(QueueFilePath, jsonPayload + Environment.NewLine);
            }
        }

        private void FlushQueue()
        {
            List<string> pending;

            lock (_queueLock)
            {
                if (!File.Exists(QueueFilePath)) return;
                pending = new List<string>(File.ReadAllLines(QueueFilePath));
                if (pending.Count == 0) return;
                File.WriteAllText(QueueFilePath, string.Empty); // vaciar; lo que falle se reencola abajo
            }

            var stillFailing = new List<string>();

            foreach (var line in pending)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var content = new StringContent(line, Encoding.UTF8, "application/json");
                    var response = _http.PostAsync("/api/ingest/ninjatrader/executions", content)
                        .GetAwaiter().GetResult();

                    if (!response.IsSuccessStatusCode)
                    {
                        stillFailing.Add(line);
                    }
                }
                catch (Exception)
                {
                    stillFailing.Add(line);
                }
            }

            if (stillFailing.Count > 0)
            {
                lock (_queueLock)
                {
                    File.AppendAllLines(QueueFilePath, stillFailing);
                }
            }
        }
    }
}
