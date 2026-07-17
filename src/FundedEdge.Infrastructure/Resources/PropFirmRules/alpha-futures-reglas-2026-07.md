# Alpha Futures — Reglas completas
Fuente: help.alpha-futures.com. Extraído 2026-07-16.

Cuatro tipos de cuenta: **Advanced**, **Zero**, **Direct** (activas), **Standard** (retirada 1/may/2026, incluida por contexto histórico). Todas usan **MLL en EOD trailing puro** — Alpha destaca que ninguna cuenta usa trailing intradía.

---

## 1. MLL (Maximum Loss Limit) — mecánica común a las 4

- Se calcula sobre el **máximo balance EOD** (no equity intradía).
- Se recalcula solo al cierre; un mal día intradía no mueve el MLL hacia abajo mientras el EOD no marque nuevo mínimo relevante.
- **Se congela exactamente en el balance inicial** (no en Inicial+$100 como Apex/Lucid/Tradeify/MFFU). Ej. 50K: MLL sube hasta $50.000 y ahí se fija para siempre.
- Rotura de MLL (floating o cerrado) → liquidación inmediata de todo, cuenta cerrada (o eval inválida).
- ⚠️ **Sin regla "MLL a $0 al primer payout"** — a diferencia de otras firms, retirar no adelanta el bloqueo del MLL.

---

## 2. TIPOS DE CUENTA

### 2.1 Advanced (sin DLG, sin scaling, sin consistencia en funded)

| | 50K | 100K | 150K |
|---|---|---|---|
| Profit Target | $4.000 | $8.000 | $12.000 |
| MLL | $1.750 | $3.500 | $5.250 |
| Daily Loss Guard | **Ninguno** (eval y funded) | | |
| Consistencia eval | 40% | | |
| Consistencia funded | **Ninguna** | | |
| Scaling | **No** — contrato máximo desde el día 1 | | |
| News trading | Sin restricciones | | |
| Activación | $0 (compras tras 8/jul/2026) | | |
| Split | **90% desde el inicio**, sin escalado por payouts | | |
| Retiro | Hasta 50% del beneficio cada 5 días ganadores ≥$200 | | |
| Límite retiro | $1.000 mín / $15.000 máx | | |
| Frecuencia | Hasta 4x/mes, sin fechas fijas | | |

### 2.2 Zero ($0 activación permanente, pasable en 1 día)

| | 25K | 50K | 100K |
|---|---|---|---|
| Profit Target | $1.500 | $3.000 | $6.000 |
| MLL | $1.000 | $2.000 | $3.000 |
| Daily Loss Guard | $500 | $1.000 | $2.000 |
| Consistencia eval | **Ninguna** — pasable en 1 día | | |
| Consistencia funded | 40% | | |
| Scaling funded | No (25K) / Sí (50K, 100K) | | |
| Split | 90% desde el inicio | | |
| Límite retiro | $200 mín / $1.000–2.500 máx según tamaño | | |

### 2.3 Direct (sin evaluación, funded directo)

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| MLL | $1.000 | $2.000 | $3.000 | $4.500 |
| Daily Loss Guard | $500 | $1.000 | $2.000 | $3.000 |
| Cuota única | $349 | $519 | $689 | $859 |
| Consistencia | **20%** (la más exigente de las 4) | | | |
| Scaling | No | | | |
| News trading | **Con restricciones** en eventos mayores (única línea con restricción en funded) | | | |
| 1er objetivo de retiro | $1.500 | $3.000 | $6.000 | $9.000 |
| 2º objetivo+ | $1.000 | $2.000 | $4.000 | $6.000 |
| Límite retiro | $500 mín / $1.000–3.000 máx | | | |
| Split | 90% en cada retiro | | | |

> Los "Withdrawal Profit Targets" de Direct son beneficio a **ganar** entre solicitudes (resetean cada vez), no importe retirable — análogo al "Profit Goal" de Tradeify Lightning.

### 2.4 Standard (RETIRADA — contexto histórico, no comprable)

| | 50K | 100K | 150K |
|---|---|---|---|
| Profit Target | $3.000 | $6.000 | $9.000 |
| MLL | $2.000 | $4.000 | $6.000 |
| DLG | **Ninguno en eval**; $1.000/$2.000/$3.000 en funded | | |
| Consistencia | 50% eval / 40% funded | | |
| Split | Escalado: **70%** (payouts 1-2) → **80%** (3-4) → **90%** (5+) | | |
| Retiro | Cada 14 días desde 1er trade, sujeto a consistencia 40% | | |
| Límite retiro | $200 mín / **$15.000 máx** (el mayor cap del sector) | | |
| Activación | $149 | | |

---

## 3. DAILY LOSS GUARD (DLG) — mecánica común

- **Soft breach**: bloquea la cuenta hasta la siguiente sesión (18:00 ET), no la rompe.
- Basado en P&L del día (realizado + no realizado, incluye comisiones simuladas).
- Al tocar el -2% (Zero) o el valor fijo (Direct), se lanza liquidación a mercado de todo lo abierto — **no garantiza fill exacto** en el nivel.
- **Advanced no tiene DLG preestablecido** ni en eval ni en funded (puedes configurar uno tú mismo en la plataforma).
- Zero sí tiene DLG en evaluación (única firm de las revisadas con DLG activo en fase de evaluación combinado con 1 día para pasar).

---

## 4. CONSISTENCIA — comparativa

| Cuenta | Eval | Funded |
|---|---|---|
| Advanced | 40% | Ninguna |
| Zero | Ninguna (pasa en 1 día) | 40% |
| Direct | — (sin eval) | **20%** |
| Standard (legacy) | 50% | 40% |

Fórmula estándar: `Mayor día / Beneficio neto desde el último retiro ≤ %`. Superarla no rompe la cuenta, solo bloquea el botón de retiro hasta corregirlo con más días.

---

## 5. Lectura estratégica

| Objetivo | Mejor cuenta Alpha |
|---|---|
| Sin DLG en ninguna fase, contrato máximo desde el día 1 | **Advanced** |
| Pasar la eval en 1 día, coste de entrada bajo | **Zero** |
| Saltarte la evaluación completamente | **Direct** (pero consistencia 20% más exigente y restricciones de news) |
| Máximo cap de retiro por solicitud | Standard (histórico, $15K) — actualmente Advanced/Standard comparten ese tope si sigue vigente |
| MLL más "generoso" tras payouts | Cualquiera — es rasgo transversal de Alpha (se congela en el balance inicial exacto, sin penalización por retirar) |

**Diferenciador clave de Alpha vs. las demás firms del compendio**: el MLL se congela en el **balance inicial exacto** (no +$100), y **retirar no adelanta ni penaliza** ese bloqueo — es la política más favorable de las 4 firms para preservar buffer post-payout que has revisado hasta ahora.
