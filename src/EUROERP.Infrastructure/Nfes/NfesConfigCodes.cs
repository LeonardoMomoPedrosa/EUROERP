namespace EUROERP.Infrastructure.Nfes;

internal static class NfesConfigCodes
{
    public const string Provider = "NFES_PROVIDER";
    public const string Environment = "NFES_ENV";
    public const string CodigoMunicipio = "NFES_CODMUN";
    public const string EmitCnpj = "NFES_CNPJ";
    public const string EmitIMun = "NFES_IMUN";
    public const string ApiBaseUrl = "NFES_API_URL";
    public const string ApiBaseUrlHomolog = "NFES_API_URL_H";
    public const string ApiEmitPath = "NFES_API_PATH";
    public const string LoteNfeUrl = "NFES_LOTE_URL";
    public const string CertPath = "NFES_CERT_PATH";
    public const string CertPassword = "NFES_CERT_PWD";
    public const string XmlPath = "NFES_XML_PATH";
    public const string SchemaPath = "NFES_SCHEMA_PATH";
    public const string SerieDps = "NFES_SERIE";
    public const string VersaoDps = "NFES_VER_DPS";
    public const string VersaoAplicativo = "NFES_VER_APP";
    public const string CodigoTributacaoNacional = "NFES_CTTRIB_NAC";
    public const string CodigoTributacaoMunicipal = "NFES_CTTRIB_MUN";
    public const string CodigoNbs = "NFES_CNBS";
    public const string ServiceCode = "NFES_SVC_CODE";
    public const string ServiceTax = "NFES_ALIQ";
    public const string OpSimpNac = "NFES_OPSIMPNAC";
    public const string RegEspTrib = "NFES_REGESPTRIB";
    public const string RegApTribSn = "NFES_REGAPTRIBSN";
    public const string PercentualTotTribSn = "NFES_PTOTRIBSN";
    public const string IncludeIbsCbs = "NFES_IBSCBS";
    public const string IbsCbsIndOp = "NFES_CINDOP";
    public const string IbsCbsCst = "NFES_CST";
    public const string IbsCbsClassTrib = "NFES_CCLASSTRIB";
    public const string IbsCbsFinNfse = "NFES_FINNFSE";
    public const string IbsCbsIndFinal = "NFES_INDFINAL";
    public const string IbsCbsIndDest = "NFES_INDDEST";

    public static IReadOnlyList<string> All { get; } =
    [
        Provider, Environment, CodigoMunicipio, EmitCnpj, EmitIMun,
        ApiBaseUrl, ApiBaseUrlHomolog, ApiEmitPath, LoteNfeUrl,
        CertPath, CertPassword, XmlPath, SchemaPath,
        SerieDps, VersaoDps, VersaoAplicativo,
        CodigoTributacaoNacional, CodigoTributacaoMunicipal, CodigoNbs,
        ServiceCode, ServiceTax, OpSimpNac, RegEspTrib, RegApTribSn, PercentualTotTribSn,
        IncludeIbsCbs, IbsCbsIndOp, IbsCbsCst, IbsCbsClassTrib,
        IbsCbsFinNfse, IbsCbsIndFinal, IbsCbsIndDest
    ];
}
