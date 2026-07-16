using System.Text.Json.Serialization;

namespace EUROERP.Application.NFe;

/// <summary>Response for GET /api/NF?id=&param=SYNC (Lambda contract: exact property names).</summary>
public class NFScheduleResultDto
{
    [JsonPropertyName("RESULT_CODE")]
    public string RESULT_CODE { get; set; } = "";

    [JsonPropertyName("RESULT_MESSAGE")]
    public string RESULT_MESSAGE { get; set; } = "";

    [JsonPropertyName("PDF_FILE_NAME")]
    public string? PDF_FILE_NAME { get; set; }

    [JsonPropertyName("XML_FILE_NAME")]
    public string? XML_FILE_NAME { get; set; }
}
