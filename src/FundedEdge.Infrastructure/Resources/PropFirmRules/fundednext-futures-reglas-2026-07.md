# FundedNext Futures — Reglas completas (Flex, Rapid Pro, Rapid Daily)
Fuente: helpfutures.fundednext.com. Extraído 2026-07-16.

Tres líneas activas: **Flex** (máxima flexibilidad, 1 fase), **Rapid Pro** y **Rapid Daily** (challenge único con 2 caminos elegidos en la compra). La línea **Bolt** fue **discontinuada el 10/jul/2026** (sin nuevas compras ni resets, cuentas existentes siguen operando).

---

## 1. FLEX CHALLENGE (50K/100K/150K)

Modelo de entrada más barato, con el profit target más bajo del mercado.

| Tamaño | Profit Target | Max Loss Limit | MLL se congela en |
|---|---|---|---|
| 50K | $2.500 | $1.500 | $50.100 |
| 100K | $5.000 | $2.500 | $100.100 |
| 150K | $8.000 | $4.000 | $150.100 |

**Contratos máx.**: 50K→3mini/30micro · 100K→5mini/50micro · 150K→8mini/80micro.

- Drawdown: **EOD trailing**, se congela en Inicial+$100 (patrón estándar del sector).
- **Sin DLL, sin regla de buffer.**
- Consistencia: **40% solo en fase Challenge**. Si se supera, el profit target de la fase challenge sube en consecuencia (no falla la cuenta). **En la cuenta FundedNext (funded) no hay consistencia.**
- Reset disponible mientras estés en fase Challenge; no disponible tras pasar a cuenta funded.
- **Reward Share estándar 90%**; add-on opcional sube a **95%** por fee adicional.
- **Camino a Live**: entras al pool de revisión si cumples 1+ de: 5 ciclos de Performance Reward completados en una sola cuenta, historial excepcional en cuentas FundedNext, o experiencia previa en trading live. No garantiza selección (discrecional). Al pasar a Live: **todas las cuentas Flex existentes se cierran**, el beneficio simulado se pierde, y el depósito máximo acumulado en la cuenta live no puede superar **$10.000** combinando todas las cuentas Flex.

---

## 2. RAPID PRO & RAPID DAILY (25K/50K/100K)

Un único challenge, dos caminos elegidos **en el momento de la compra y no modificables después**.

### Parámetros comunes (idénticos en ambos caminos)

| Tamaño | Profit Target | Max Loss Limit | Contratos máx. |
|---|---|---|---|
| 25K | $1.500 | $1.000 | 2mini/20micro |
| 50K | $3.000 | $2.000 | 4mini/40micro |
| 100K | $5.000 | $2.500 | 6mini/60micro |

- **Pasable en 1 día** (1-Day Pass) en ambos caminos.
- Drawdown: **EOD trailing** en ambos.
- **Sin consistencia en fase Challenge** (ambos caminos).
- **Reward Share: 90%** en ambos.
- Cuenta funded se concluye tras el **5º Performance Reward**.
- Reset disponible solo en fase Challenge.

### Comparativa Rapid Pro vs Rapid Daily

| | Rapid Pro | Rapid Daily |
|---|---|---|
| Enfoque | Flexibilidad y control | Velocidad y simplicidad |
| Frecuencia de rewards | **Cada 3 días** | **Diaria** |
| Daily Loss Limit | **Ninguno** (add-on opcional) | **Sí, obligatorio** |
| Consistencia (cuenta funded) | **40%** | **Ninguna** |
| Regla de buffer | No | **Sí** |

### DLL (Daily Loss Limit)

| Tamaño | Rapid Daily (obligatorio) | Rapid Pro (con add-on opcional) |
|---|---|---|
| 25K | $500 | $500 |
| 50K | $1.000 | $1.000 |
| 100K | $1.250 | $1.250 |

El add-on de DLL en Rapid Pro **reduce el precio de compra** (25K: −$20, 50K: −$40, 100K: −$50) pero debe elegirse en la compra y **no se puede añadir/quitar después**.

### Elegibilidad para Performance Reward
Ambos caminos requieren **mínimo $500 de beneficio** en el ciclo actual, más:
- **Rapid Pro**: cumplir la regla de consistencia 40%.
- **Rapid Daily**: el balance EOD debe alcanzar el **buffer** (`Balance inicial + Max Loss Limit + $100`). Solo lo que supera el buffer es retirable; el buffer permanece en la cuenta.

### Retiros (ambos caminos)

| Tamaño | Retiro mínimo | Retiro máximo |
|---|---|---|
| 25K | $250 | $800 |
| 50K | $250 | $1.200 |
| 100K | $250 | $2.500 |

---

## 3. Diferenciador clave: EOD puro en todas las líneas activas

A diferencia de MFFU o TakeProfitTrader, **ninguna línea activa de FundedNext Futures usa trailing intradía** — tanto en evaluación como en cuenta funded, el drawdown es siempre EOD. Esto simplifica la gestión de riesgo: tu excursión adversa intradía no mueve el suelo mientras no cierres la sesión en un nuevo máximo.

---

## 4. Lectura estratégica

| Objetivo | Mejor opción |
|---|---|
| Entrada más barata, profit target más bajo | **Flex** |
| Cash flow rápido con estructura de seguridad (DLL + buffer) | **Rapid Daily** |
| Máxima flexibilidad intradía sin DLL, payouts algo más espaciados | **Rapid Pro** (sin add-on) |
| Maximizar reward share | **Flex + add-on 95%** (el más alto de las 3 líneas) |
| Evitar la regla de consistencia por completo en fase funded | **Flex** o **Rapid Daily** (ambas sin consistencia en funded) |

**Nota sobre Bolt**: si tienes o consideras una cuenta Bolt existente, puede seguir operando pero **no admite resets ni nuevas compras** desde el 10/jul/2026 — su ciclo de vida está cerrado.
