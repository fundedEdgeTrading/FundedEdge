// Service worker mínimo para que la app sea instalable como PWA (PLAN_IMPLEMENTACION_MERCADO.md
// M7.1). Deliberadamente sin caché ni intercepción de fetch: FundedEdge es Blazor Server (estado
// vivo en el circuito SignalR), así que un modo offline no tiene sentido y cachear rompería más
// de lo que aporta. Si algún día se añaden notificaciones push, este es el sitio.
self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", (event) => event.waitUntil(self.clients.claim()));
