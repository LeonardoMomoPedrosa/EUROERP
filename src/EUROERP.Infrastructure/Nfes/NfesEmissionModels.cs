namespace EUROERP.Infrastructure.Nfes;

public sealed class NfesOrderRow
{
    public int OrderId { get; init; }
    public int ClientId { get; init; }
    public string Status { get; init; } = "";
    public string? CarDescription { get; init; }
    public string? CarPlate { get; init; }
    public string? CarProblem { get; init; }
}

public sealed class NfesClientRow
{
    public string? CnpjPf { get; init; }
    public string? PersonType { get; init; }
    public string? SocialName { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressBlock { get; init; }
    public string? AddressNumber { get; init; }
    public string? AddressComplement { get; init; }
    public string? AddressZipCode { get; init; }
    public string? CMun { get; init; }
    public string? AddressStateCode { get; init; }
    public string? StateInscr { get; init; }
    public string? MunInscr { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public sealed class NfesServiceLineRow
{
    public string Name { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

public sealed class NfesEmissionWorkItem
{
    public int OrderId { get; init; }
    public int RpsNumber { get; init; }
    public bool Reprint { get; init; }
    public decimal NetAmount { get; init; }
    public NfesOrderRow Order { get; init; } = null!;
    public NfesClientRow Client { get; init; } = null!;
    public IReadOnlyList<NfesServiceLineRow> ServiceLines { get; init; } = Array.Empty<NfesServiceLineRow>();
    public IReadOnlyList<DateTime> BtrDueDates { get; init; } = Array.Empty<DateTime>();
}

public sealed class NfesEmissionOutcome
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? NfesNo { get; init; }
    public string? RpsNo { get; init; }
    public string? CheckCode { get; init; }
    /// <summary>Full nacional access key (Simpliss); stored in ORDER.NFE_RECEIPT when longer than NFES_CHECK_CODE.</summary>
    public string? ChaveAcesso { get; init; }
    public string? PdfUrl { get; init; }
    public string? XmlPath { get; init; }

    public static NfesEmissionOutcome Fail(string message) => new() { Success = false, Message = message };
}
