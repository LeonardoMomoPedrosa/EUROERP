namespace EUROERP.Application.RevenueReporting;

/// <summary>One row in the daily revenue report (order + payment method line).</summary>
public class DailyRevenueRowDto
{
    public int OrderId { get; set; }
    public string? SentDate { get; set; }
    public string? OrderDate { get; set; }
    public int ClientId { get; set; }
    public string? SocialName { get; set; }
    public string? FantasyName { get; set; }
    public string? Ledge { get; set; }
    public int SpecialClientId { get; set; }
    public byte PaymentMethodId { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? SalesAgent { get; set; }
    public int Receipt { get; set; }
    public int NfesNo { get; set; }
    public string? OfSaler { get; set; }
    public decimal Discount { get; set; }
    public decimal Credit { get; set; }
}
