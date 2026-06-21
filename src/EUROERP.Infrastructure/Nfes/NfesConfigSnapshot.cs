using System.Globalization;
using EUROERP.Application.Nfes;
using Microsoft.Extensions.Configuration;

namespace EUROERP.Infrastructure.Nfes;

public sealed class NfesConfigSnapshot
{
    public string Provider { get; init; } = "Simpliss";
    public string Environment { get; init; } = "test";
    public string CodigoMunicipio { get; init; } = "3547304";
    public string EmitCnpj { get; init; } = "";
    public string EmitIMun { get; init; } = "";
    public string ApiBaseUrl { get; init; } = "";
    public string ApiBaseUrlHomolog { get; init; } = "";
    public string ApiEmitPath { get; init; } = "/nfse";
    public string LoteNfeUrl { get; init; } = "";
    public string CertPath { get; init; } = "";
    public string CertPassword { get; init; } = "";
    public string XmlPath { get; init; } = "";
    public string SchemaPath { get; init; } = "";
    public string SerieDps { get; init; } = "00001";
    public string VersaoDps { get; init; } = "1.01";
    public string VersaoAplicativo { get; init; } = "EUROERP1.0";
    public string CodigoTributacaoNacional { get; init; } = "140101";
    public string CodigoTributacaoMunicipal { get; init; } = "";
    public string CodigoNbs { get; init; } = "";
    public string ServiceCode { get; init; } = "";
    public decimal ServiceTax { get; init; } = 5m;
    public string OpSimpNac { get; init; } = "1";
    public string RegEspTrib { get; init; } = "0";
    public string RegApTribSn { get; init; } = "1";
    public string PercentualTotTribSn { get; init; } = "";
    public bool IncludeIbsCbs { get; init; }
    public string IbsCbsIndOp { get; init; } = "100301";
    public string IbsCbsCst { get; init; } = "000";
    public string IbsCbsClassTrib { get; init; } = "000001";
    public string IbsCbsFinNfse { get; init; } = "0";
    public string IbsCbsIndFinal { get; init; } = "0";
    public string IbsCbsIndDest { get; init; } = "0";

    public bool IsTestEnvironment =>
        Environment.Equals("test", StringComparison.OrdinalIgnoreCase);

    public bool UseSimpliss =>
        !Provider.Equals("PrefeituraSp", StringComparison.OrdinalIgnoreCase);

    public static NfesConfigSnapshot From(IReadOnlyDictionary<string, string> db, IConfiguration configuration)
    {
        string Pick(string code, string configKey, string defaultValue = "") =>
            GetDb(db, code) ?? configuration[configKey] ?? defaultValue;

        return new NfesConfigSnapshot
        {
            Provider = Pick(NfesConfigCodes.Provider, "Nfes:Provider", "Simpliss"),
            Environment = Pick(NfesConfigCodes.Environment, "Nfes:Environment", "test"),
            CodigoMunicipio = Pick(NfesConfigCodes.CodigoMunicipio, "Nfes:CodigoMunicipio", "3547304"),
            EmitCnpj = Pick(NfesConfigCodes.EmitCnpj, "Nfes:EmitCnpj", configuration["NFe:EmitCnpj"] ?? ""),
            EmitIMun = Pick(NfesConfigCodes.EmitIMun, "Nfes:EmitIMun", configuration["NFe:EmitIMun"] ?? ""),
            ApiBaseUrl = Pick(NfesConfigCodes.ApiBaseUrl, "Nfes:ApiBaseUrl", "https://nfsesantanadeparnaiba.simplissweb.com.br"),
            ApiBaseUrlHomolog = Pick(NfesConfigCodes.ApiBaseUrlHomolog, "Nfes:ApiBaseUrlHomolog", "https://producaorestrita.simplissweb.com.br"),
            ApiEmitPath = Pick(NfesConfigCodes.ApiEmitPath, "Nfes:ApiEmitPath", "/nfse"),
            LoteNfeUrl = Pick(NfesConfigCodes.LoteNfeUrl, "Nfes:LoteNfeUrl", "https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx"),
            CertPath = Pick(NfesConfigCodes.CertPath, "Nfes:CertPath", configuration["NFe:CertPath"] ?? ""),
            CertPassword = Pick(NfesConfigCodes.CertPassword, "Nfes:CertPassword", configuration["NFe:CertPassword"] ?? ""),
            XmlPath = Pick(NfesConfigCodes.XmlPath, "Nfes:XmlPath", configuration["NFe:NfeXmlPath"] ?? "NFE_files"),
            SchemaPath = Pick(NfesConfigCodes.SchemaPath, "Nfes:SchemaPath", configuration["NFe:NfeXsdPath"] ?? "schemas"),
            SerieDps = Pick(NfesConfigCodes.SerieDps, "Nfes:SerieDps", "00001"),
            VersaoDps = Pick(NfesConfigCodes.VersaoDps, "Nfes:VersaoDps", "1.01"),
            VersaoAplicativo = Pick(NfesConfigCodes.VersaoAplicativo, "Nfes:VersaoAplicativo", "EUROERP1.0"),
            CodigoTributacaoNacional = Pick(NfesConfigCodes.CodigoTributacaoNacional, "Nfes:CodigoTributacaoNacional", "140101"),
            CodigoTributacaoMunicipal = Pick(NfesConfigCodes.CodigoTributacaoMunicipal, "Nfes:CodigoTributacaoMunicipal"),
            CodigoNbs = Pick(NfesConfigCodes.CodigoNbs, "Nfes:CodigoNbs", ""),
            ServiceCode = Pick(NfesConfigCodes.ServiceCode, "Nfes:ServiceCode", "07285"),
            ServiceTax = decimal.Parse(Pick(NfesConfigCodes.ServiceTax, "Nfes:ServiceTax", "5"), CultureInfo.InvariantCulture),
            OpSimpNac = Pick(NfesConfigCodes.OpSimpNac, "Nfes:OpSimpNac", "1"),
            RegEspTrib = Pick(NfesConfigCodes.RegEspTrib, "Nfes:RegEspTrib", "0"),
            RegApTribSn = Pick(NfesConfigCodes.RegApTribSn, "Nfes:RegApTribSn", "1"),
            PercentualTotTribSn = Pick(NfesConfigCodes.PercentualTotTribSn, "Nfes:PercentualTotTribSn", ""),
            IncludeIbsCbs = ParseBool(Pick(NfesConfigCodes.IncludeIbsCbs, "Nfes:IncludeIbsCbs", "false")),
            IbsCbsIndOp = Pick(NfesConfigCodes.IbsCbsIndOp, "Nfes:IbsCbsIndOp", "100301"),
            IbsCbsCst = Pick(NfesConfigCodes.IbsCbsCst, "Nfes:IbsCbsCst", "000"),
            IbsCbsClassTrib = Pick(NfesConfigCodes.IbsCbsClassTrib, "Nfes:IbsCbsClassTrib", "000001"),
            IbsCbsFinNfse = Pick(NfesConfigCodes.IbsCbsFinNfse, "Nfes:IbsCbsFinNfse", "0"),
            IbsCbsIndFinal = Pick(NfesConfigCodes.IbsCbsIndFinal, "Nfes:IbsCbsIndFinal", "0"),
            IbsCbsIndDest = Pick(NfesConfigCodes.IbsCbsIndDest, "Nfes:IbsCbsIndDest", "0")
        };
    }

    public NfesConfigDto ToDto(bool certPasswordConfigured)
    {
        return new NfesConfigDto
        {
            Provider = Provider,
            Environment = Environment,
            CodigoMunicipio = CodigoMunicipio,
            EmitCnpj = EmitCnpj,
            EmitIMun = EmitIMun,
            ApiBaseUrl = ApiBaseUrl,
            ApiBaseUrlHomolog = ApiBaseUrlHomolog,
            ApiEmitPath = ApiEmitPath,
            LoteNfeUrl = LoteNfeUrl,
            CertPath = CertPath,
            CertPassword = "",
            XmlPath = XmlPath,
            SchemaPath = SchemaPath,
            SerieDps = SerieDps,
            VersaoDps = VersaoDps,
            VersaoAplicativo = VersaoAplicativo,
            CodigoTributacaoNacional = CodigoTributacaoNacional,
            CodigoTributacaoMunicipal = CodigoTributacaoMunicipal,
            CodigoNbs = CodigoNbs,
            ServiceCode = ServiceCode,
            ServiceTax = ServiceTax.ToString(CultureInfo.InvariantCulture),
            OpSimpNac = OpSimpNac,
            RegEspTrib = RegEspTrib,
            RegApTribSn = RegApTribSn,
            PercentualTotTribSn = PercentualTotTribSn,
            IncludeIbsCbs = IncludeIbsCbs,
            IbsCbsIndOp = IbsCbsIndOp,
            IbsCbsCst = IbsCbsCst,
            IbsCbsClassTrib = IbsCbsClassTrib,
            IbsCbsFinNfse = IbsCbsFinNfse,
            IbsCbsIndFinal = IbsCbsIndFinal,
            IbsCbsIndDest = IbsCbsIndDest,
            CertPasswordConfigured = certPasswordConfigured
        };
    }

    private static string? GetDb(IReadOnlyDictionary<string, string> db, string code) =>
        db.TryGetValue(code, out var value) ? value.Trim() : null;

    private static bool ParseBool(string text) =>
        text is "1" or "true" or "True" or "S" or "Y";
}
