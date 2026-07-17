# TakeProfitTrader — Reglas completas (Test, PRO, PRO+)
Fuente: takeprofittraderhelp.zendesk.com. Extraído 2026-07-16.

Estructura de 2-3 fases: **Test** (evaluación) → **PRO** (Sim Funded, split 80/20) → **PRO+** (upgrade opcional, split 90/10).

---

## 1. EVALUACIÓN (Test Account) — Las 6 Reglas Core

### Parámetros por tamaño

| Tamaño | Max contratos | Profit Target | Max Trailing Drawdown |
|---|---|---|---|
| $25.000 | 3 | $1.500 | $1.500 |
| $50.000 | 6 | $3.000 | $2.000 |
| $75.000 | 9 | $4.500 | $2.500 |
| $100.000 | 12 | $6.000 | $3.000 |
| $150.000 | 15 | $9.000 | $4.500 |

### Regla 3 — EOD Maximum Trailing Drawdown
- Se calcula **solo al cierre de sesión**, no intradía.
- El "Minimum Account Balance" (aparece así en el Control Center) sigue el mayor balance EOD hacia arriba, nunca baja.
- **Se congela al llegar al balance inicial de la cuenta** (no +$100, sino el balance inicial exacto).
- Aunque el cálculo es EOD, el **enforcement es en tiempo real**: si el balance toca el umbral en cualquier momento intradía (realizado o no realizado), liquidación inmediata.

Ejemplo 25K: drawdown $1.500 → balance mínimo inicial $23.500. Al cerrar un día en $26.000, el mínimo sube a $24.500. Se congela cuando llega a $25.000 (el balance inicial).

### Regla 5 — Consistencia (Be Consistent)
Dos requisitos combinados:
1. **Mínimo 5 días de trading** (día = al menos 1 trade), sin importar cuánto tardes en completarlos.
2. **Ningún día puede superar el 50% del beneficio neto total**: `Mayor día / PnL neto ≤ 50%`.

Si se supera el 50%, no falla la cuenta — solo pospone el pase. **Fórmula de objetivo actualizado**: `Nuevo Profit Goal = PnL neto × 2`.

### Otras reglas del Test
- Regla 1: alcanzar el profit target.
- Regla 2: no exceder el tamaño máximo de posición.
- Regla 4: solo productos e instrumentos aprobados, en horario permitido.
- Regla 6: sin posiciones contrarias (no hedging) — aplica a Test, PRO y PRO+.

---

## 2. CUENTA PRO (Sim Funded)

### Diferencia crítica vs. Test: el drawdown cambia de EOD a INTRADAY

**Reglas de la cuenta PRO:**

1. **Sin bots/algos** — todas las operaciones deben ejecutarse manualmente.
2. **Evitar limit up/down**: debes salir de posiciones abiertas antes de que el mercado alcance el límite diario de precio (CME); si se alcanza el límite y sigues dentro, pierdes la cuenta PRO.
3. **Requisito de trading semanal**: operar al menos 1 día por semana calendario (domingo–viernes) para mantener la cuenta activa. Excepciones disponibles contactando soporte.
4. **No posiciones contrarias** (igual que en Test).
5. **Intraday Trailing Drawdown**:
   - Se calcula **intradía** usando tu balance pico (peak balance), que incluye ganancias realizadas y no realizadas.
   - El drawdown nunca supera el balance inicial de la cuenta.
   - **Drawdown máximo en PRO = el mismo que superaste en la evaluación** (no cambia).
   - Se congela al llegar al balance inicial.
6. **Prohibido operar durante noticias específicas**: FOMC (miércoles 14:00 ET), Non-Farm Payroll (primer viernes de mes, 8:30 ET), CPI, y para instrumentos específicos: inventarios de crudo (Crude Oil) y subastas de bonos (10-Year Note, 30-Year Bond). Debes estar plano 1 minuto antes/durante/1 minuto después.

**Ejemplo de funcionamiento del drawdown intradía (25K):**
Drawdown $1.500 → si el beneficio no realizado llega a $1.000, el balance mínimo sube en tiempo real a $24.500. Si cierras con $500 realizado (balance $25.500), el mínimo sigue en $24.500 — quedan $1.000 de margen.

### Split de beneficios PRO
**80% para el trader / 20% para la firma.**

---

## 3. PRO+ (upgrade opcional)

- **Split 90/10** — el trader se queda con el 90%.
- Trading directamente vinculado a la firma (mayor integración).
- Balance disponible para retiro sujeto a los mismos términos de profit split estándar tras el upgrade.
- Proceso de upgrade con guías propias — consultar "PRO+ Account Upgrade Process" para requisitos específicos.

---

## 4. RETIROS (Withdrawals)

- **PRO**: split 80/20.
- **PRO+**: split 90/10.
- Comisiones: TakeProfitTrader exime todas las comisiones de retiro desde el wallet superiores a $250. Si solicitas $250 o menos, se aplica una comisión.
- Impuestos: TakeProfitTrader es una empresa con base en EE.UU. y retiene según normativa fiscal de EE.UU. — los retiros están sujetos a impuestos.
- Existe un flujo específico "How to Withdraw from PRO Account to the Wallet" — el cash disponible se calcula automáticamente en el Control Center según la cuenta.

*(Nota: no se pudo confirmar el umbral mínimo de retiro por solicitud ni la cadencia exacta de payouts — la fuente no lo detalla explícitamente en las páginas indexadas; contactar soporte para confirmar antes de operar.)*

---

## 5. Otras reglas transversales
- **Universal Trading Policies (UTP)**: política marco que cubre todas las cuentas.
- **Independent Trade Execution Policy**: cada operación debe ejecutarse de forma independiente.
- **Trade Copier Policy**: reglas específicas sobre copiadores de operaciones — revisar antes de usar cualquier herramienta de replicación entre cuentas.
- **Reglas para múltiples cuentas**: existen restricciones específicas — consultar "Rules for Multiple Accounts" si gestionas varias cuentas TPT simultáneamente.

---

## 6. Lectura estratégica

| Aspecto | Nota para tu operativa |
|---|---|
| Drawdown cambia de EOD (eval) a Intraday (PRO) | Muy relevante: tu margen de excursión adversa se reduce al pasar a fondeada — el mismo cambio que ves en MFFU Rapid, pero aquí ocurre en la fase PRO en vez de mantenerse |
| Prohibición de bots/algos en PRO | Si automatizas parte de tu operativa vía `mt5-algo-trader-agent`, no es compatible con TakeProfitTrader PRO — ejecución manual obligatoria |
| Requisito de trading semanal | Menos estricto que la inactividad de 7 días de otras firms — aquí basta con 1 operación/semana |
| Ventana de noticias prohibidas | Coincide en gran medida con las ventanas que ya evitas en tu edge de sesión NY — verificar solapamiento con tus horarios de A1/A2 |
| Split 80/20 → 90/10 en PRO+ | Vale la pena evaluar el upgrade si operas de forma sostenida, dado el salto de 10 puntos porcentuales |
