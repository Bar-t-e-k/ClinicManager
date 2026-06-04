namespace ClinicManager.Web.Models;

public class VisitProcedure
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public int ProcedureId { get; set; }
    public Procedure Procedure { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    /// <summary>Snapshot kosztu świadczenia w momencie wykonania procedury.</summary>
    public decimal UnitCost { get; set; }
}
