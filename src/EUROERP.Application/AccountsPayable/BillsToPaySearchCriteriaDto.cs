namespace EUROERP.Application.AccountsPayable;

/// <summary>Search criteria for AP (Contas a Pagar) report.</summary>
public class BillsToPaySearchCriteriaDto
{
    public DateTime? FirstDate { get; set; }
    public DateTime? LastDate { get; set; }
    /// <summary>True = filter by due date; false = filter by order date.</summary>
    public bool DueDateCriteria { get; set; } = true;
    public int SupplierId { get; set; }
    public byte SupplierGroupId { get; set; }
    /// <summary>Comma-separated bill IDs (e.g. "108, 111"). When set, date range is ignored.</summary>
    public string? IdStr { get; set; }
    /// <summary>P = paid only, U = unpaid only, null = both.</summary>
    public string? Status { get; set; }
    public byte PaymentMethodId { get; set; }
    public bool Abc { get; set; }
    public bool AbcGroup { get; set; }
}
