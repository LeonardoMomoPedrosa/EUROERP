namespace EUROERP.Application.Nfes;

public interface INfesConfigService
{
    Task<NfesConfigDto> GetConfigAsync(CancellationToken cancellationToken = default);
    Task SaveConfigAsync(NfesConfigDto config, CancellationToken cancellationToken = default);
}

public class NfesConfigDto
{
    public string Provider { get; set; } = "Simpliss";
    public string Environment { get; set; } = "test";
    public string CodigoMunicipio { get; set; } = "3547304";
    public string EmitCnpj { get; set; } = "";
    public string EmitIMun { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "";
    public string ApiBaseUrlHomolog { get; set; } = "";
    public string ApiEmitPath { get; set; } = "/nfse";
    public string LoteNfeUrl { get; set; } = "";
    public string CertPath { get; set; } = "";
    /// <summary>Empty on save = keep current password in SYS_CONTROL.</summary>
    public string CertPassword { get; set; } = "";
    public string XmlPath { get; set; } = "";
    public string SchemaPath { get; set; } = "";
    public string SerieDps { get; set; } = "00001";
    public string VersaoDps { get; set; } = "1.01";
    public string VersaoAplicativo { get; set; } = "EUROERP1.0";
    public string CodigoTributacaoNacional { get; set; } = "140101";
    public string CodigoTributacaoMunicipal { get; set; } = "";
    public string CodigoNbs { get; set; } = "";
    public string ServiceCode { get; set; } = "";
    public string ServiceTax { get; set; } = "5";
    public string OpSimpNac { get; set; } = "1";
    public string RegEspTrib { get; set; } = "0";
    public string RegApTribSn { get; set; } = "1";
    public string PercentualTotTribSn { get; set; } = "";
    public bool IncludeIbsCbs { get; set; }
    public string IbsCbsIndOp { get; set; } = "100301";
    public string IbsCbsCst { get; set; } = "000";
    public string IbsCbsClassTrib { get; set; } = "000001";
    public string IbsCbsFinNfse { get; set; } = "0";
    public string IbsCbsIndFinal { get; set; } = "0";
    public string IbsCbsIndDest { get; set; } = "0";
    public bool CertPasswordConfigured { get; set; }
}
