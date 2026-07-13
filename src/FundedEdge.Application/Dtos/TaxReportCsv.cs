using System.Globalization;
using System.Text;

namespace FundedEdge.Application.Dtos;

/// <summary>
/// Serializa el informe fiscal a CSV con formato neutro (separador coma, decimales con punto,
/// fechas ISO yyyy-MM-dd): lo abre cualquier Excel/Sheets sin depender de la cultura local.
/// Los importes van siempre en positivo; la columna Type distingue ingreso (Payout) de gasto (Cost).
/// </summary>
public static class TaxReportCsv
{
    public static string Build(TaxYearReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Date,Firm,Account,Concept,Amount,Notes");

        foreach (var p in report.Payouts.OrderBy(p => p.PaidOn))
            AppendLine(sb, "Payout", p.PaidOn, p.FirmName, p.AccountName, "Payout", p.AmountReceived, p.Notes);

        foreach (var c in report.Costs.OrderBy(c => c.PaidOn))
            AppendLine(sb, "Cost", c.PaidOn, c.FirmName, c.AccountName, c.Kind.ToString(), c.Amount, c.Notes);

        return sb.ToString();
    }

    private static void AppendLine(StringBuilder sb, string type, DateOnly date, string firm, string account, string concept, decimal amount, string? notes)
    {
        sb.Append(type).Append(',');
        sb.Append(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(Escape(firm)).Append(',');
        sb.Append(Escape(account)).Append(',');
        sb.Append(Escape(concept)).Append(',');
        sb.Append(amount.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(Escape(notes ?? string.Empty));
        sb.AppendLine();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
