namespace ClinicManager.Web.DTOs;

public class MonthlyReportDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public List<DailyCostSummaryDto> DailySummaries { get; set; } = new();

    public decimal TotalMedicationsCost => DailySummaries.Sum(x => x.MedicationsCost);
    public decimal TotalProceduresCost => DailySummaries.Sum(x => x.ProceduresCost);
    public decimal TotalCost => DailySummaries.Sum(x => x.TotalCost);
}

public class DailyCostSummaryDto
{
    public DateTime Date { get; set; }
    public decimal MedicationsCost { get; set; }
    public decimal ProceduresCost { get; set; }
    public decimal TotalCost { get; set; }
}