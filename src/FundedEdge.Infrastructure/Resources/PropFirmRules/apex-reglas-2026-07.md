# Apex Trader Funding — Reglas completas (EOD e Intraday)
Fuente: help center oficial. Extraído 2026-07-16.

Apex tiene **dos vías paralelas**: **EOD Trailing Drawdown** e **Intraday Trailing Drawdown**. Cada una: Evaluación → Performance Account (PA) → Payouts.

---

## 1. EVALUACIONES

| | EOD Eval | Intraday Eval |
|---|---|---|
| Trailing | 1x/día al cierre (16:59:59 ET), fijo durante la sesión siguiente | Tiempo real, sigue Peak Balance (incluye PnL no realizado) |
| Daily Loss Limit | **Sí** (fijo durante la sesión) | **No** |
| Días mínimos | 0 (se puede pasar en 1 día) | 0 |
| Acceso | 30 días naturales (sin prórroga) | 30 días naturales |
| Consistencia / Scaling | No aplica | No aplica |
| Activar PA tras pasar | 7 días naturales | 7 días naturales |

### Parámetros por tamaño (idénticos en ambas)

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Profit Target | $1.500 | $3.000 | $6.000 | $9.000 |
| Max Drawdown | $1.000 | $2.000 | $3.000 | $4.000 |
| Daily Loss Limit (solo EOD) | $500 | $1.000 | $1.500 | $2.000 |
| Max contratos | 4 | 6 | 8 | 12 |

### Para pasar
1. Alcanzar el profit target dentro de los 30 días naturales desde la compra.
2. No tocar nunca el threshold (EOD o Intraday).

### Reglas
- **Threshold tocado = fallo inmediato** + liquidación automática. Se hace cumplir *en tiempo real* incluso en EOD.
- Si la liquidación llena ligeramente por encima/debajo del threshold, da igual: se considera breach.
- **DLL (solo EOD)**: tocarlo **no** falla la cuenta; pausa el trading el resto de la sesión. Reset del día de trading a las **18:00 ET**.
- DLL y threshold son reglas independientes.
- Prohibido hedgear evaluaciones entre sí. Cualquier circumvención = cierre inmediato.

### Cuándo deja de trailar el threshold (Evaluación)
- **Rithmic y WealthCharts**: se congela al alcanzar el nivel = Profit Target Balance. Ej. 50K: se fija en $53.000 cuando el balance máximo llega a $55.000.
- **Tradovate**: trailea **indefinidamente**. No se congela nunca. ⚠️ Diferencia crítica según plataforma.

---

## 2. PERFORMANCE ACCOUNTS (PA)

Cuenta **Sim Funded** (simulada), del mismo tamaño que la eval superada, con la misma estructura de drawdown.

| | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| Max Drawdown | $1.000 | $2.000 | $3.000 | $4.000 |
| Max contratos | 2 | 4 | 6 | 10 |
| Scaling | Tier-based (sube según balance EOD) | | | |
| DLL | Tier-based | | | |
| Inactividad | Sí | | | |

### Reglas PA
- Balance (**incl. PnL no realizado**) nunca puede tocar el threshold → liquidación + **cierre permanente** de la PA.
- **DLL**: tocarlo pausa la sesión, no cierra la cuenta. Resetea en la siguiente apertura.
- **Máx. 20 PAs activas simultáneas** (sumando EOD + Intraday + Legacy, cualquier tamaño).
- **Regla de inactividad**: si no registras al menos **2 días con $50 de beneficio neto en 30 días naturales consecutivos**, la cuenta se cierra.
- Violación de la regla de hedging/instrumentos correlacionados = cierre inmediato.
- Split de payout: **100%**.

### Cuándo deja de trailar el threshold (PA) ⭐
Se congela cuando el threshold alcanza **Balance Inicial + $100**.
- Ej. 50K: nivel de parada = **$50.100**, alcanzado cuando el Highest Balance llega a **$52.100** (Inicial + Max DD + $100).
- 25K → $25.100 (con balance $26.100) · 100K → $100.100 ($103.100) · 150K → $150.100 ($154.100)
- Es el mismo nivel que el **Safety Net**. A partir de ahí el drawdown deja de ser trailing y pasa a ser fijo.

---

## 3. PAYOUTS

Comunes a ambos tipos:
- Split **100%**, hasta payouts semanales.
- Mínimo **5 días de trading cualificados** (no consecutivos, sin fecha límite).
- Payout mínimo **$500** por solicitud.
- **Regla de consistencia 50%**: ningún día rentable puede representar ≥50% del beneficio total desde el último payout aprobado. Si no se cumple, la opción de solicitar no aparece (la cuenta sigue activa).
- **Safety Net** = drawdown limit + $100. Se mantiene **durante toda la vida de la PA**, no desaparece tras el primer payout. Solo el beneficio por encima del Safety Net es retirable.
- **Máx. 6 payouts por PA**. Tras el 6º, la PA se cierra y hay que volver a evaluar.
- Se puede seguir operando tras solicitar, pero opera como si el dinero ya estuviera fuera: si el balance cae por debajo del umbral, el payout se **deniega automáticamente**.

### Umbrales

| Tamaño | Min Trade Days | Min Daily Profit (EOD) | Min Daily Profit (Intraday) | Safety Net | Min Balance para solicitar |
|---|---|---|---|---|---|
| 25K | 5 | $100 | $100 | $26.100 | $26.600 |
| 50K | 5 | $250 | $200 | $52.100 | $52.600 |
| 100K | 5 | $300 | $250 | $103.100 | $103.600 |
| 150K | 5 | $350 | $300 | $154.100 | $154.600 |

> Solo cuentan como día cualificado los días que alcanzan el **Min Daily Profit**.
> Intraday tiene requisito de beneficio diario **más bajo** que EOD en 50K/100K/150K.

### Máximo por payout (secuencial)

**EOD**

| Payout # | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| 1 | $1.000 | $1.500 | $2.000 | $2.500 |
| 2 | $1.000 | $1.500 | $2.500 | $3.000 |
| 3 | $1.000 | $2.000 | $2.500 | $3.000 |
| 4 | $1.000 | $2.500 | $3.000 | $3.000 |
| 5 | $1.000 | $2.500 | $4.000 | $4.000 |
| 6 | $1.000 | $3.000 | $4.000 | $5.000 |

**Intraday**

| Payout # | 25K | 50K | 100K | 150K |
|---|---|---|---|---|
| 1 | $1.000 | $1.500 | $2.000 | $2.500 |
| 2 | $1.000 | $2.000 | $2.500 | $3.000 |
| 3 | $1.000 | $2.500 | $3.000 | $3.000 |
| 4 | $1.000 | $2.500 | $3.000 | $4.000 |
| 5 | $1.000 | $3.000 | $4.000 | $4.000 |
| 6 | $1.000 | $3.000 | $4.000 | $5.000 |

---

## 4. Diferencias clave EOD vs Intraday

| Aspecto | EOD | Intraday |
|---|---|---|
| Cálculo threshold | 1x/día, 16:59:59 ET | Continuo, con PnL no realizado |
| Margen intradía | Amplio: puedes drawdear dentro de la sesión sin que el threshold suba contra ti | El PnL flotante **sube el threshold** aunque no cierres |
| DLL en Eval | Sí | No |
| Min Daily Profit payout | Más alto | Más bajo |
| Ventaja | Mejor para operativa con excursión adversa | Mejor si escalas rápido y cierras en máximos |
