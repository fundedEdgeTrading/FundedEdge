# Tradeify — Reglas completas (Select, Growth, Lightning)
Fuente: help.tradeify.co. Extraído 2026-07-16. Vigente tras Tradeify 3.0.

Tres líneas de producto: **Select** (evaluación única + eliges Flex/Daily al pasar), **Growth** (evaluación clásica de 1 día), **Lightning** (sin evaluación, funded directo). Todas usan **EOD Trailing Drawdown** y split **90/10**.

---

## 1. EVALUACIONES

| | Select Eval | Growth Eval |
|---|---|---|
| Días mínimos | **3** (por regla de consistencia) | **1** (sin consistencia) |
| Consistencia | **40%** | Ninguna |
| Daily Loss Limit | **Ninguno** | Sí (soft breach) |
| Elección de payout | Se elige **después** de pasar (Flex o Daily) | Fija, sin elección |
| Drawdown | EOD Trailing | EOD Trailing |
| Límite de tiempo | Ninguno | Ninguno |

### Parámetros por tamaño

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Profit Target (ambos) | $1.500 | $3.000* | $6.000 | $9.000 |
| Max DD Select | $1.000 | $2.000 | $3.000 | $4.500 |
| Max DD Growth | $1.000 | $2.000 | $3.500 | $5.000 |
| DLL Growth (soft) | $600 | $1.250 | $2.500 | $3.750 |
| Max contratos (eval, ambos) | 1/10micro | 4/40micro | 8/80micro | 12/120micro |

*El 50K Select volvió a $3.000 (antes trial de $2.500; cuentas ya compradas mantienen el valor anterior).

**Compras/activaciones Growth**: máx. **5 cuentas funded activas simultáneas**, hasta 5 activaciones/día (rolling 24h UTC).
**Compras Select**: máx. **15 evaluaciones/30 días**, cada eval reseteable hasta 10 veces/30 días, máx. **5 funded simultáneas**.

### Consistencia 40% Select (ejemplo)
`Mayor día / Beneficio total ≤ 40%`. No limita cuánto ganas en un día — solo cuándo puedes pasar. Por eso Select exige mínimo 3 días.

---

## 2. CUENTAS FONDEADAS

### 2.1 Select Flex (5-Day Payout Policy)

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Max Drawdown | $1.000 | $2.000 | $3.000 | $4.500 |
| DLL | **Ninguno** | | | |
| Consistencia | **Ninguna** (solo aplicaba en eval) | | | |
| Contratos iniciales | 1/10micro | 2/20micro | 3/30micro | 3/30micro |
| Contratos máx (scaled) | 2/20micro | 4/40micro | 8/80micro | 12/120micro |

### 2.2 Select Daily

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Max Drawdown | $1.000 | $2.000 | $2.500 | $3.500 |
| DLL | $500 | $1.000 | $1.250 | $1.750 |
| Buffer | $1.100 | $2.100 | $2.600 | $3.600 |
| Consistencia | Ninguna | | | |

> El drawdown de Daily es **más estrecho** que Flex en 100K/150K a cambio de DLL activo — trade-off intraday flexibilidad vs. estructura.

### 2.3 Growth Funded (política fija)

Mismos parámetros de drawdown/DLL que la evaluación Growth. **DLL sube al 6% de beneficio** (no desaparece, a diferencia de cuentas legacy pre-12/09/2025 donde sí desaparecía):

| Tamaño | Balance requerido (6%) | DLL pasa a |
|---|---|---|
| 25K | $26.500 | $1.000 |
| 50K | $53.000 | $2.000 |
| 100K | $106.000 | $3.500 |
| 150K | $159.000 | $5.000 |

Consistencia funded Growth: **35%**.

### 2.4 Lightning Funded (sin evaluación, directo)

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Max Drawdown | $1.000 | $2.000 | $4.000 | $5.250 |
| DLL | **Ninguno** | $1.250 | $2.500 | $3.000 |
| Días para payout | Ninguno (sin mínimo) | | | |

DLL sube al 6%: 50K→$2.000, 100K→$4.000, 150K→$5.250 (25K nunca tiene DLL).
Consistencia progresiva: **20% (payout 1) → 25% (payout 2) → 30% (payout 3+)**. Cuentas pre-12/09/2025 se quedan fijas en 20% siempre.

---

## 3. DRAWDOWN — mecánica común (EOD Trailing, todas las líneas)

- Se recalcula **una vez al cierre de sesión**, sigue el **máximo histórico de balance EOD** (high-water mark), solo sube.
- **Hard breach**: tocarlo en cualquier momento intradía = cuenta fallada permanentemente, sin recuperación posible — aunque el cálculo sea EOD, el enforcement es en tiempo real.
- **Bloqueo del drawdown** (solo cuentas Sim Funded, no evaluaciones): cuando el balance EOD supera el drawdown + $100, el suelo se fija en **Balance inicial + $100** y ya no sube más.

### Balance EOD que dispara el bloqueo (por línea)

| Tamaño | Growth | Lightning | Select Flex | Select Daily |
|---|---|---|---|---|
| 25K | $26.100 | $26.100 | $26.100 | $26.100 |
| 50K | $52.100 | $52.100 | $52.100 | $52.100 |
| 100K | $103.600 | $104.100 | $103.100 | $102.600 |
| 150K | $155.100 | $156.100 | $154.600 | $153.600 |

Suelo final siempre: **Inicial + $100** ($25.100/$50.100/$100.100/$150.100).

---

## 4. DAILY LOSS LIMIT — mecánica común

- **Soft breach**: pausa el trading hasta la siguiente sesión (18:00 ET), la cuenta sigue viva.
- ⚠️ **No usar como stop-loss**: no es un corte duro, puede haber slippage que rompa el Trailing Drawdown (hard breach) antes de que el DLL actúe.
- Si el Trailing Drawdown está más cerca que el DLL, puedes fallar la cuenta por drawdown antes de que el DLL se dispare.
- Aplica a: Growth, Lightning, Select Daily. **Select Flex no tiene DLL.**

---

## 5. PAYOUTS

### 5.1 Select Flex

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Frecuencia | Cada 5 días ganadores | | | |
| Umbral día ganador | $100 | $150 | $200 | $250 |
| Cap por payout | 50% del beneficio total, tope **$1.250** | tope **$3.000** | tope **$4.000** | tope **$5.000** |
| Balance mínimo | **No requerido** | | | |

El 50% se calcula sobre **beneficio total** (balance actual − inicial), no solo el ciclo actual. Tras cada payout, sigues necesitando neto positivo en el ciclo para el siguiente.

### 5.2 Select Daily — Regla de Continuidad Diaria

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Cap por payout | 2× beneficio del ciclo, tope **$600** | tope **$1.000** | tope **$1.500** | tope **$2.500** |
| Payout mínimo | $250 | | | |
| Buffer | $1.100 | $2.100 | $2.600 | $3.600 |
| Procesado | Garantía 24h | | | |

### 5.3 Growth Funded

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Min balance para solicitar | $26.500 | $53.000 | $104.500 | $156.500 |
| Min trading days | 5 días con beneficio > $100/$150/$200/$250 | | | |
| Consistencia | 35% | | | |
| Payout mín. | $250 | $500 | $1.000 | $1.500 |

**Cap por número de payout:**

| Payout # | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| 1 | $1.000 | $1.500 | $2.000 | $2.500 |
| 2 | $1.000 | $2.000 | $2.500 | $3.000 |
| 3 | $1.000 | $2.500 | $3.000 | $4.000 |
| 4+ | $1.000 | $3.000 | $4.000 | $5.000 |

### 5.4 Lightning Funded

| Payout # | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| 1–3 | $1.000 | $2.000 | $2.500 | $3.000 |
| 4+ | $1.000 | $2.500 | $3.000 | $3.500 |

**Profit Goal** (lo que hay que ganar para desbloquear, no lo retirable):

| | Payout 1 | Payout 2+ |
|---|---|---|
| 25K | $1.500 | $1.000 |
| 50K | $3.000 | $2.000 |
| 100K | $6.000 | $3.500 |
| 150K | $9.000 | $4.500 |

Sin mínimo de días de trading. Consistencia progresiva 20/25/30%. Payout mínimo $1.000. Procesado en 24h.

### Comunes a todas las líneas
- Split **90/10**.
- Solicitud **final**, no editable/cancelable.
- Máx. **5 cuentas funded activas** (combinando Growth+Select+Lightning).
- Si operas tras solicitar y caes por debajo del umbral, el payout se **deniega**.
- Reconciliación diaria 18:00–20:00 ET.

---

## 6. REGLAS TRANSVERSALES DE TRADING

### Hedging y mezcla de contratos ⚠️ (relevante para ti — operas MGC)
- **Regla 1 — No hedging**: no puedes tener long y short simultáneos en el mismo instrumento (ni entre cuentas distintas).
- **Regla 2 — No mezclar MINI y MICRO**: no puedes tener minis y micros abiertos a la vez, aunque sean instrumentos distintos. GC (mini) y MGC (micro) **no pueden coexistir abiertos**.
- Sí puedes alternar entre sesiones (cerrar MGC por la mañana, abrir GC por la tarde), pero no solaparlos.
- Gracia de 10 segundos para cerrar posiciones en conflicto.
- Detección automática en evaluaciones: se rompe la cuenta solo si se cumplen **las 3 condiciones a la vez**: posiciones opuestas + duración >10s + beneficio del hedge >$250.
- Consecuencias: descalificación, denegación de payout, cuenta FAILED (incluidas todas las cuentas involucradas en violaciones cruzadas).

### Consistencia — fórmula general
`Mayor beneficio de un día / Beneficio total del periodo ≤ %`
Los días perdedores **empeoran** el ratio (reducen el denominador). No limita cuánto puedes ganar un día — solo pospone la elegibilidad de payout.

---

## 7. Tradeify 3.0 — Elite Live y Reward Pool (contexto adicional)

Al pasar a cuenta **Elite Live**, cada cuenta obtiene un **Performance Reward Pool** independiente:

| Tamaño | Pool estándar | Con multiplicador 1.5x (solo Select, consistencia <40% y nunca >75% del DD) |
|---|---|---|
| 25K | $2.000 | $3.000 |
| 50K | $4.000 | $6.000 |
| 100K | $8.000 | $12.000 |
| 150K | $12.000 | $18.000 |

Requisitos mensuales por cuenta para desbloquear reward: 5 días con ≥$250 beneficio + acabar el mes con beneficio > drawdown trailing.

**Umbral para ser considerado para Elite Live**: 3 payouts en una sola cuenta, o 10 payouts totales desde la última transición a Live. Es un mínimo, no garantía automática — Tradeify evalúa de forma discrecional.

---

## 8. Lectura estratégica para tu operativa (oro/MGC, sesión NY)

| Objetivo | Mejor opción |
|---|---|
| Margen intradía amplio en excursiones adversas | **Select Flex** (sin DLL, sin consistencia en funded) |
| Payouts frecuentes y pequeños | **Select Daily** o **Lightning** |
| Pasar la eval en 1 día | **Growth** |
| Empezar a cobrar sin evaluación | **Lightning** (pero drawdown más ajustado que Growth en 100K/150K) |
| Evitar journaling de consistencia post-funded | **Select** (cualquiera de las dos), **Growth** conserva 35% |
| Cuidado con MGC | Nunca combines con GC (mini) en la misma cuenta ni entre cuentas — violación automática |
