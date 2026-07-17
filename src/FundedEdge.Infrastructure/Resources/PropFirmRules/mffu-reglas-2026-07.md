# MyFundedFutures (MFFU) — Reglas completas
Fuente: help.myfundedfutures.com. Extraído 2026-07-16.

MFFU tiene **cuatro líneas de producto** con estructuras muy distintas entre sí: **Rapid**, **Flex**, **Pro**, **Builder**. Todas terminan en cuenta **Live** (capital real), no solo Sim Funded — es el diferenciador clave frente a Apex/Lucid/Tradeify.

---

## 1. RAPID PLAN

### Evaluación (EOD Trailing)

| | 25K | 50K | 100K | 150K* |
|---|---|---|---|---|
| Profit Target | $1.500 | $3.000 | $6.000 | $9.000 |
| Max Loss (EOD) | $1.000 | $2.000 | $3.000 | $4.500 |
| DLL | Ninguno | | | |
| Max contratos | 3/30micro | 5/50micro | 8/80micro | 12/120micro |
| Consistencia | 50% (solo eval) | | | |
| Días mínimos | **2** | | | |

*150K por patrón, no verificado explícitamente en fuente.

### Sim Funded — ⚠️ cambia a Intraday Trailing (no EOD)
- Drawdown: **Intraday trailing** sobre el high-water mark de equity (incluye no realizado).
- Distancia fija mientras trailea (igual al Max Loss de la eval).
- **Se bloquea a $100** de suelo absoluto (no relativo al balance inicial — la cuenta arranca en $0). Balance debe mantenerse siempre ≥$100.
- Excepción 25K: bloqueo declarado en $25.100 (verificar en dashboard, posible inconsistencia de doc).
- Inactividad: sin trade en 7 días naturales → posible cierre.

### Payouts Rapid
- Buffer requerido: **Max Loss + $100** ($1.100/$2.100/$3.100/…)
- Sin regla de consistencia en fase de payout.
- **Frecuencia: diaria** (cada 24h desde el primer trade).
- Payout mínimo $500. Split **90/10**.

---

## 2. FLEX PLAN (25K y 50K)

### Evaluación (EOD Trailing)

| | 50K |
|---|---|
| Profit Target | $3.000 |
| Max Loss (EOD) | $2.000 |
| DLL | Ninguno (add-on opcional $1.000 soft pause) |
| Max contratos | 3/30micro |
| Consistencia | 50% (solo eval) |
| Días mínimos | 2 |
| Activación | $0 |

### Sim Funded
- Arranca en **$0** (balance puede ir negativo hasta que el MLL trailee sobre breakeven — normal).
- **Sin buffer requerido.**
- Scaling por balance: $0–1.499 → 1 mini/10micro · $1.500–1.999 → 2/20 · $2.000+ → 3/30 (máx).
- Inactividad: 7 días naturales.

### Payout Flex
- Cualificación: **5 días ganadores con ≥$150 PnL neto cada uno**.
- Neto requerido entre payouts: $500.
- **Máximo por payout: 50% del beneficio total, tope $2.000.**
- **Split 80/20** (no 90/10).
- **Máx. 5 payouts sim**, luego transición a Live.
- ⚠️ Tras el **primer payout**, el MLL se mueve a **$100 fijo permanentemente** (mucho antes que Apex/Lucid/Tradeify que fijan en +$100 del balance inicial).

### Transición a Live (Flex)
- Trigger: 5 payouts consecutivos aprobados, o cap de $100.000 en sim payouts, o revisión discrecional del equipo de riesgo.
- Cuenta Live: balance inicial $2.000, mínimo $156, **sin DLL**, EOD drawdown, payout mínimo $250, 3/30micro, split 80/20, **cadencia diaria**.
- **Cooldown de 21 días** tras breach en Live: sin trading sim, sin nuevas evals ni resets durante ese periodo.

---

## 3. PRO PLAN (50K/100K/150K)

Diseñado para traders experimentados — sin DLL en ninguna fase, sin consistencia en sim funded.

### Sim Funded

| | 50K | 100K | 150K |
|---|---|---|---|
| Split | 80/20 | 80/20 | 80/20 |
| Max Loss (EOD) | $2.000 | $3.000 | $4.500 |
| DLL | Ninguno | | |
| Consistencia | Ninguna | | |
| Max contratos | 5/5micro | 10/10micro | 15/15micro |
| T1 News | **No permitido** | | |

### Payouts Pro
- Cualificación: **14 días naturales** desde el primer trade + buffer despejado.
- Buffer: $2.100 / $3.100 / $4.600.
- Payout mínimo: **$1.000**.
- **Cap de por vida por usuario: $100.000** (no por cuenta — acumulado).
- MLL tras primer payout: se fija en Inicial+$100 ($50.100/$100.100/$150.100), permanente.
- Excepción: **retiro único (one-time)** antes de completar el buffer: hasta 60% del beneficio, min $1.000, el 40% restante queda en la cuenta.

### Transición a Live (Pro)
- Trigger: 3 payouts consecutivos, **o** exceso de beneficio sobre el cap de $100K se transfiere a Live hasta un máximo ($5.000/$7.500/$10.000 según tamaño).
- Live: balance estático $2.000–$10.000 según tamaño, DLL $700–$3.000 (rango, escala), contratos 2–6 (rango), payout mín $250.
- **Desbloqueo del balance inicial en Live**: tras 20 días ganadores (cada uno ≥4% del balance inicial) + 3 payouts, puedes retirar hasta dejar $140 de balance inicial.
- Hito de $20.000 de beneficio dispara revisión (no garantiza transición).

---

## 4. BUILDER PLAN (25K/50K) — dos variantes de MLL

Evaluación **sin consistencia**, pasable en **1 día**.

| | Default | Add-On (más barato) |
|---|---|---|
| Profit Target | $3.000 | $3.000 |
| Max Loss (EOD) | $2.000 | $1.500 |
| DLL | $1.000 soft pause | $1.000 soft pause |
| Max contratos | 4/40micro | 4/40micro |
| Consistencia | Ninguna | Ninguna |
| Días mínimos | 1 | 1 |

### Sim Funded
- Arranca en $0, **solo 1 cuenta Builder activa por usuario**. Si se rompe, la siguiente solo puede activarse al día de trading siguiente.
- Payout disponible 48h tras el primer trade.

### Payout Builder
- Split **80/20**.
- Consistencia payout: **50%** (mayor día ≤50% del beneficio del ciclo).
- Mínimo 2 días cualificados/ciclo.
- Beneficio neto requerido: $500 (primer payout, sobre buffer) / $500 (siguientes, desde el último payout).
- Buffer = MLL + $100 ($2.100 default / $1.600 add-on).
- **Cap $2.000/ciclo**, máx. **5 payouts sim**.

### Transición a Live (Builder)
- Tras el 5º payout aprobado.
- Live: mismo MLL que elegiste (EOD trailing), se fija estático al llegar a $0, payout mín $250, split 80/20, **cadencia diaria**, DLL $1.000, sin consistencia.
- Mismo cooldown de 21 días tras breach en Live.

---

## 5. MECÁNICA COMÚN

### EOD Trailing (Rapid/Flex/Pro/Builder evaluación y Pro/Flex/Builder sim)
- Se recalcula al cierre de sesión, solo sube con nuevos máximos EOD.
- Se **bloquea permanentemente** al llegar a Inicial + $100 (excepto Flex, que se bloquea en $100 absolutos tras el primer payout — ver arriba).
- Enforcement en tiempo real aunque el cálculo sea EOD.

### Intraday Trailing (solo Rapid Sim Funded) ⚠️
- Único entre las 4 líneas: en fase Sim Funded, Rapid usa trailing **intradía** (incluye PnL flotante), no EOD.
- Esto es más estricto que EOD — un pico de beneficio flotante sube el suelo aunque no cierres la posición.

### Cooldown tras breach en Live
- **21 días naturales**: sin trading sim, sin comprar nuevas evals/resets. Se levanta automáticamente tras el periodo.

---

## 6. Comparativa rápida entre planes MFFU

| | Rapid | Flex | Pro | Builder |
|---|---|---|---|---|
| DLL | No | No (opcional) | No | Sí ($1.000 soft) |
| Consistencia eval | 50% | 50% | 50% | **Ninguna** |
| Días mín. eval | 2 | 2 | 2 | **1** |
| Drawdown sim funded | **Intraday** | EOD | EOD | EOD |
| Split | 90/10 | 80/20 | 80/20 | 80/20 |
| Buffer payout | Sí | **No** | Sí | Sí |
| Frecuencia payout | Diaria | Por ciclo (5 días) | 14 días | Por ciclo (2 días min) |
| Cap payout | Sin tope declarado | $2.000/ciclo | $100K de por vida | $2.000/ciclo |
| Trigger a Live | — | 5 payouts o $100K sim | 3 payouts o exceso sobre cap | 5 payouts |

---

## 7. Lectura estratégica

- **Rapid** es el más parecido a "cash flow rápido": payouts diarios, split 90/10 (el mejor split de las 4 líneas), pero el drawdown intradía en sim funded es más exigente para tu estilo de excursión adversa en NY.
- **Flex** no tiene buffer — a cambio, el MLL se clava en $100 absolutos nada más el primer payout, mucho antes que en otras firms.
- **Pro** es la única sin DLL en ninguna fase y sin consistencia en sim — pero **no permite T1 news trading** y tiene ciclo de payout largo (14 días).
- **Builder** es la única con evaluación de 1 día sin consistencia, pero introduce DLL obligatorio (único con DLL fijo, no opcional) y solo 1 cuenta activa a la vez.
