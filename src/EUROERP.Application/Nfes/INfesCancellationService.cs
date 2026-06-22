namespace EUROERP.Application.Nfes;

public interface INfesCancellationService
{
    Task<CancelNfesResult> CancelAsync(CancelNfesRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NfesCanceledReceiptDto>> GetTodayCanceledAsync(CancellationToken cancellationToken = default);
}

public class CancelNfesRequest
{
    public DateTime CancelDate { get; set; } = DateTime.Today;
    public int NfesNo { get; set; }
    public string Memo { get; set; } = "";
    /// <summary>1=Erro emissão, 2=Serviço não prestado, 9=Outros (Simpliss e101101).</summary>
    public string MotivoCode { get; set; } = "9";
    public string UserId { get; set; } = "SYS";
    public string ApplicationId { get; set; } = "EUROERP";
}

public class CancelNfesResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? OrderId { get; set; }
}

public class NfesCanceledReceiptDto
{
    public DateTime CancelDate { get; set; }
    public int ReceiptNo { get; set; }
    public string Memo { get; set; } = "";
}
