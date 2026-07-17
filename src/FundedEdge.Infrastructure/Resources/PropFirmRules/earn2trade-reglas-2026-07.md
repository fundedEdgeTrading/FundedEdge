# Earn2Trade — Reglas completas (TCP y Gauntlet Mini)
Fuente: help.earn2trade.com + earn2trade.com. Extraído 2026-07-16.

Earn2Trade es distinto al resto: cobra **suscripción mensual** (no pago único) y ofrece elección entre **LiveSim®** (capital simulado) o **Live** (capital real en broker real) tras pasar. Único de los evaluados con cuentas Live reales genuinas junto con Alpha Direct.

---

## 1. EVALUACIONES

### Trader Career Path® (TCP) — progresión escalonada 25K→400K

| | TCP25 |
|---|---|
| Balance virtual | $25.000 |
| Profit Goal | $1.750 |
| EOD Drawdown | $1.500 |
| Daily Loss Limit | $550 |
| Max contratos | 3 (10 micros = 1 estándar) |
| Consistencia | **30%** |
| Máx. cuentas eval simultáneas | 5 |
| Duración mínima | Sin mínimo de días declarado en specs, pero FAQ indica **mínimo 10 días** para completar en la práctica |
| Precio | desde $150/mes |
| Reset | Gratis en cada rebilling; TCP100 reset fijo $100, TCP25/50 dinámico |

**Contrato especial**: hasta **10 micro-contratos cuentan como 1 estándar** en la progression ladder — muy relevante para MGC.

### Gauntlet Mini™ (GM) — evaluación de fase única, 50K-200K

| | GAU50 |
|---|---|
| Balance virtual | $50.000 |
| Profit Goal | $3.000 |
| EOD Drawdown | $2.000 |
| Daily Loss Limit | $1.100 |
| Max contratos | 6 |
| Consistencia | 30% |
| Máx. cuentas eval simultáneas | 5 |
| Duración mínima | 10 días (según FAQ) |
| Precio | desde $170/mes |

Ambos (TCP y GM) comparten la regla de consistencia del 30% y el mismo mecanismo EOD.

### Regla de consistencia (Maintain Consistency)
`Mayor día / PnL total del examen < 30%`. No falla la cuenta al superarla, solo pospone el pass. **No aplica en LiveSim/Live** — solo en fase de evaluación.

---

## 2. CUENTAS FONDEADAS (LiveSim® o Live — se elige tras pasar)

Mismo tamaño y parámetros de riesgo que la evaluación superada. Diferencia clave entre las dos:
- **LiveSim®**: capital simulado, datos de mercado reales.
- **Live**: capital real en broker real (vía firma proprietary partner).

### TCP25 Funded (LiveSim o Live)

| | Valor |
|---|---|
| Profit Goal para avanzar | $1.750 |
| Drawdown | $1.500 (LiveSim: EOD / Live: **Trailing**, no EOD) |
| DLL | $550 |
| Max contratos | 3 |
| Split | **50% bajo $1.500 de retiro, 80% en la parte que exceda $1.500** — por tamaño de la solicitud, no acumulado |
| Máx. cuentas simultáneas | 3 LiveSim, o 2 LiveSim + 1 Live |
| Consistencia | Ninguna |
| Buffer | Ninguno |
| Payout | Semanal |

⚠️ **Diferencia crítica LiveSim vs Live**: en LiveSim el drawdown es EOD; en Live pasa a ser **trailing** (recalculado con cada nuevo máximo, no solo al cierre). Se congela en el balance inicial en ambos casos.

### Growth Plan — progresión de capital TCP (ejemplo desde 25K)

| Fase | Balance | Profit Goal | Drawdown | DLL | Max contratos |
|---|---|---|---|---|---|
| Eval | $25.000 | $1.750 | $1.500 EOD | $550 | 3 |
| LiveSim/Live | $25.000 | $1.750 | $1.500 | $550 | 3 |
| Live | $50.000 | $3.000 | $2.000 trailing | $1.100 | 6 |
| Live | $100.000 | $6.000 | $3.500 trailing | $2.200 | 12 |
| Live | $200.000 | $11.000 | Fijo en $194.000 | $4.400 | 16 |

Al llegar a $200K y cumplir el profit goal, se recibe una oferta personalizada (hasta TCP400 con split fijo 60/40).

### GAU50 Funded (LiveSim o Live)

| | Valor |
|---|---|
| Drawdown | $2.000 (LiveSim EOD / Live Trailing) |
| DLL | $1.100 |
| Max contratos | 6 |
| Split | 50% bajo $2.250, 80% sobre $2.250 |
| Máx. cuentas simultáneas | 3 LiveSim, o 2 LiveSim+1 Live |
| Consistencia | Ninguna |
| Buffer | Ninguno |
| Payout | Semanal |

El tamaño de la cuenta fondeada **coincide siempre** con el de la evaluación superada (no hay escalón intermedio como en TCP).

---

## 3. DRAWDOWN — mecánica

- **EOD** en evaluación y en LiveSim: se calcula al cierre, sigue el mayor balance EOD, solo sube, y **se bloquea al llegar al balance inicial** (no +$100 como en otras firms — como Alpha Futures).
- **Trailing** en cuentas Live reales: se recalcula continuamente con cada nuevo máximo (más exigente que EOD), también se bloquea en el balance inicial.
- Tocar el drawdown en cualquier momento = breach inmediato.

## 4. DAILY LOSS LIMIT
- Basado en equity abierta, calculado según calendario CME.
- Al tocarlo: cuenta bloqueada el resto del día, reanuda al día siguiente (soft breach).

---

## 5. PAYOUTS Y RETIROS

### Split — el más particular del sector
No depende del beneficio acumulado sino del **tamaño de cada retiro individual**:

**TCP**

| Cuenta | 50% split | 80% split |
|---|---|---|
| TCP25 | bajo $1.500 | $1.500+ |
| TCP50 | bajo $2.250 | $2.250+ |
| TCP100 | bajo $3.000 | $3.000+ |
| TCP150 | bajo $4.000 | $4.000+ |
| TCP200 | bajo $5.000 | $5.000+ |
| TCP400 | fijo 60/40 | fijo 60/40 |

**Gauntlet Mini**

| Cuenta | 50% split | 80% split |
|---|---|---|
| GM50 | bajo $2.250 | $2.250+ |
| GM100 | bajo $3.000 | $3.000+ |
| GM150 | bajo $4.000 | $4.000+ |
| GM200 | bajo $5.000 | $5.000+ |

> Truco: dividir un retiro grande en varios de menor tamaño **no** ayuda a maximizar el split, ya que se calcula por solicitud, no por sesión.

### Condiciones comunes
- **Sin buffer requerido, sin consistencia en fase funded.**
- Retiro mínimo neto: **$100**.
- Procesado **semanalmente los miércoles**. Solicitud por email a la prop firm antes de las 14:00 CT del viernes anterior.
- **Fee de activación de $139**: se descuenta solo del **primer retiro exitoso** (no se paga por adelantado).
- Métodos: Rise ($50 no-US / 1.5% US), Deel ($50), Bayzat ($5-40), Crypto directo (0.735%). Rise no disponible en Iowa, Minnesota, Carolina del Sur, Guam, Puerto Rico, Islas Vírgenes de EE.UU. o Haití.

---

## 6. Diferenciadores frente al resto de firms evaluadas

| Aspecto | Earn2Trade | Resto (Apex/Lucid/Tradeify/MFFU/Alpha) |
|---|---|---|
| Modelo de cobro | **Suscripción mensual** | Mayormente pago único |
| Elección Sim vs Real | **Sí** (LiveSim o Live tras pasar) | Solo Alpha Direct ofrece algo similar |
| Split | Por tamaño de solicitud (50/80%) | Por nº de payout o fijo |
| Bloqueo drawdown | En balance inicial exacto | +$100 sobre inicial (mayoría) |
| Progresión de capital | Sí, TCP escala automáticamente 25K→400K | No (excepto Alpha con múltiples cuentas) |
| Micros vs minis | 10 micros = 1 mini en el límite de posición | Reglas de mezcla más estrictas (Tradeify prohíbe mezclar) |
| Consistencia en funded | **Nunca** (solo en evaluación) | Varía mucho por firm |

---

## 7. Lectura estratégica para tu operativa

- El conteo de **10 micros = 1 contrato estándar** es una ventaja notable operando MGC frente a firms que cuentan cada micro de forma más restrictiva.
- Elegir **Live** en vez de LiveSim te da capital real pero cambia el drawdown a **trailing** (más exigente que EOD) — evalúa si tu excursión adversa intradía encaja con eso.
- El split por tamaño de retiro (no por payout secuencial) significa que **agrupar retiros grandes maximiza el 80%**, al contrario que en firms donde conviene espaciar.
- Suscripción mensual + reset gratuito en cada rebill hace que el coste de "intentos ilimitados" sea distinto a un modelo de pago único con resets de pago.
