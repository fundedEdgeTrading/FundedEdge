# Lucid Trading — Reglas completas (LucidFlex y LucidPro)
Fuente: support.lucidtrading.com. Extraído 2026-07-16.

Ambos planes usan **EOD Trailing Drawdown** para el Max Loss Limit (MLL) y **split 90/10** en payouts.

---

## 1. EVALUACIONES

| | LucidFlex Eval | LucidPro Eval |
|---|---|---|
| Profit Target | $1.250 / $3.000 / $6.000 / $9.000 (25K/50K/100K/150K) | Idéntico |
| Max Loss Limit | $1.000 / $2.000 / $3.000 / $4.500 | Idéntico |
| Daily Loss Limit | **Ninguno** | **Fijo**: 25K none · 50K $1.200 · 100K $1.800 · 150K $2.700 |
| Consistencia | **50%** obligatoria para upgrade | **No aplica** |
| Max size | 2/4/6/10 minis (20/40/60/100 micros) | Idéntico |
| Scaling | No | No |
| Cuota | Pago único, sin rebilling, sin límite de tiempo | Idéntico |
| Activación funded | 5–30 min, sin fee | Idéntico |

**Consistencia LucidFlex (solo eval)**: `Mayor día / Beneficio total ≤ 50%`. Hay un *cushion* (~+4%) que permite pasar en 2 días. Ej. 50K: target $3.000, 50% = $1.500, cushion ≈ $1.560. El cushion es **porcentual sobre el beneficio real**, no un importe fijo.

> **Diferencia clave**: Flex te impone consistencia en la eval pero no DLL. Pro te impone DLL pero no consistencia. Se invierte en fase funded.

---

## 2. CUENTAS FONDEADAS (Funded)

| | LucidFlex Funded | LucidPro Funded |
|---|---|---|
| Max Loss Limit | $1.000 / $2.000 / $3.000 / $4.500 | Idéntico |
| Daily Loss Limit | **Ninguno** | Fijo ($1.200/$1.800/$2.700) → **LucidScale DLL** tras superar Initial Trail |
| Consistencia | **Ninguna** | **40%** por ciclo de payout (35% si compra/reset ≤ 28/11/2025 15:00 EST) |
| Buffer de payout | **Ninguno** | **Sí** (MLL inicial + $100) |
| Scaling plan | **Sí** (limita contratos por beneficio) | **No** — max size desde el minuto uno |
| Payouts máx. por cuenta | **5**, luego pasa a Live | Sin cap de payouts simulados |

### LucidScale DLL (solo Pro)
Al cerrar por encima del **Initial Trail Balance**, el DLL fijo se sustituye por:
`Mayor beneficio EOD × 60% = LucidScale DLL`
Solo sube, nunca baja. Ej. beneficio EOD máx $4.000 → DLL $2.400.
En 25K no hay DLL en ningún momento.

### Scaling plan LucidFlex (solo funded, actualiza al cierre de sesión)

| Beneficio simulado | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| $0–999 | 1 mini | 2 minis | 3 minis | 4 minis |
| $1.000–1.999 | 2 minis | 3 minis | 4 minis | 5 minis |
| $2.000–2.999 | — | 4 minis | 5 minis | 6 minis |
| $3.000–4.499 | — | — | 6 minis | 8 minis |
| $4.500+ | — | — | — | 10 minis |

> ⚠️ Un payout reduce el balance simulado → **puede bajarte de tier de scaling**. Circumvenir los límites puede suponer borrado del beneficio del día o revisión de la cuenta.

---

## 3. DRAWDOWN (EOD, idéntico en Flex y Pro)

- El MLL se recalcula al cierre de cada sesión sobre el **mayor balance de cierre**.
- Sube con el balance hasta que la cuenta supera el **Initial Trail Balance**; ahí el MLL **se bloquea permanentemente** en Balance inicial + $100.
- Tocar el MLL = cuenta breacheada.

| Tamaño | MLL | Initial Trail Balance | Locked MLL Balance |
|---|---|---|---|
| 25K | $1.000 | $26.100 | $25.100 |
| 50K | $2.000 | $52.100 | $50.100 |
| 100K | $3.000 | $103.100 | $100.100 |
| 150K | $4.500 | $154.600 | $150.100 |

### ⚠️ Diferencia crítica Flex vs Pro
> **LucidFlex**: *"Once you request a payout from LucidFlex, your MLL automatically adjusts to the Locked MLL Balance."*
> Es decir: al solicitar payout, el MLL salta directamente al Locked MLL Balance **aunque no hubieras llegado al Initial Trail**. En Pro el bloqueo solo ocurre al superar el Initial Trail de forma natural.

---

## 4. PAYOUTS

| | LucidFlex | LucidPro |
|---|---|---|
| Split | 90/10 | 90/10 (100% sobre los primeros $10K si compra/reset < 28/11/2025) |
| Días con beneficio mínimo | **5 días** por ciclo | **No aplica** |
| Beneficio mínimo por día | $100/$150/$200/$250 | — |
| Objetivo de beneficio por ciclo | Neto positivo (basta $1) | **$250/$500/$750/$1.000** |
| Consistencia | Ninguna | **40%** (mayor día / beneficio del ciclo) |
| Buffer | **Ninguno** | MLL inicial + $100 ($26.100/$52.100/$103.100/$154.600) |
| Mínimo por solicitud | $500 | $500 |
| Máximo por solicitud | 50% del beneficio, tope $1.000/$2.000/$2.500/$3.000 | Payout 1: $1.000/$2.000/$2.500/$3.000 · Payout 2+: $1.500/$2.500/$3.000/$3.500 |
| Nº de payouts | **Máx. 5**, luego a Live | Sin límite indicado |
| Escalado de máximos | **No escala** con más payouts | Sí (payout 2+ superior) |
| Ventana | Cualquier día al cumplir criterios | Igual |
| Procesado | Deducción en minutos, abono ≤2 días hábiles | Igual |

**Comunes**: las solicitudes son **finales** (no se editan ni cancelan). Si operas tras solicitar y el balance cae por debajo del umbral/buffer, la solicitud se **deniega**. Opera como si el dinero ya estuviera fuera.

**Métodos de cobro**: Plaid (USA, instantáneo), WorkMarket by ADP (USA + internacional, banco o PayPal, ~1 día hábil), **Crypto (BTC/ETH/LTC/USDT/USDC — solo internacional)** ← el relevante para ti desde España.

**Fees**: pago único, sin suscripción ni rebilling. No hay fee de activación a funded. Los **resets no son gratis ni automáticos**, se compran desde el dashboard.

---

## 5. Lectura estratégica Flex vs Pro

| Objetivo | Mejor plan |
|---|---|
| Payout rápido y simple | **Flex** (sin buffer, sin consistencia en funded, solo 5 días de $X) |
| Acumulación de cartera a largo plazo | **Pro** (sin cap de 5 payouts, sin scaling plan, DLL que crece al 60%) |
| Tamaño de posición completo desde el día 1 en funded | **Pro** (Flex te limita por scaling) |
| Preservar buffer post-payout | **Pro** — en Flex el primer payout te clava el MLL en Locked MLL Balance |
| Pasar la eval con un día bueno | **Pro** (sin regla de consistencia en eval) |
