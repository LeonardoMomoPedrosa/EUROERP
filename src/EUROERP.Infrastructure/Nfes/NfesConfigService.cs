using EUROERP.Application.Config;
using EUROERP.Application.Nfes;
using Microsoft.Extensions.Configuration;

namespace EUROERP.Infrastructure.Nfes;

public interface INfesConfigProvider
{
    Task<NfesConfigSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public class NfesConfigService : INfesConfigService, INfesConfigProvider
{
    private readonly ISysControlService _sysControl;
    private readonly IConfiguration _configuration;
    private NfesConfigSnapshot? _cached;

    public NfesConfigService(ISysControlService sysControl, IConfiguration configuration)
    {
        _sysControl = sysControl;
        _configuration = configuration;
    }

    public async Task<NfesConfigSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_cached != null)
            return _cached;
        var db = await _sysControl.GetValuesAsync(NfesConfigCodes.All, cancellationToken).ConfigureAwait(false);
        _cached = NfesConfigSnapshot.From(db, _configuration);
        return _cached;
    }

    public async Task<NfesConfigDto> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var pwd = await _sysControl.GetValueAsync(NfesConfigCodes.CertPassword, cancellationToken).ConfigureAwait(false);
        var hasPwd = !string.IsNullOrWhiteSpace(pwd)
            || !string.IsNullOrWhiteSpace(_configuration["Nfes:CertPassword"])
            || !string.IsNullOrWhiteSpace(_configuration["NFe:CertPassword"]);
        return snapshot.ToDto(hasPwd);
    }

    public async Task SaveConfigAsync(NfesConfigDto config, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>
        {
            [NfesConfigCodes.Provider] = config.Provider.Trim(),
            [NfesConfigCodes.Environment] = config.Environment.Trim(),
            [NfesConfigCodes.CodigoMunicipio] = config.CodigoMunicipio.Trim(),
            [NfesConfigCodes.EmitCnpj] = config.EmitCnpj.Trim(),
            [NfesConfigCodes.EmitIMun] = config.EmitIMun.Trim(),
            [NfesConfigCodes.ApiBaseUrl] = config.ApiBaseUrl.Trim(),
            [NfesConfigCodes.ApiBaseUrlHomolog] = config.ApiBaseUrlHomolog.Trim(),
            [NfesConfigCodes.ApiEmitPath] = config.ApiEmitPath.Trim(),
            [NfesConfigCodes.LoteNfeUrl] = config.LoteNfeUrl.Trim(),
            [NfesConfigCodes.CertPath] = config.CertPath.Trim(),
            [NfesConfigCodes.XmlPath] = config.XmlPath.Trim(),
            [NfesConfigCodes.SchemaPath] = config.SchemaPath.Trim(),
            [NfesConfigCodes.SerieDps] = config.SerieDps.Trim(),
            [NfesConfigCodes.VersaoDps] = config.VersaoDps.Trim(),
            [NfesConfigCodes.VersaoAplicativo] = config.VersaoAplicativo.Trim(),
            [NfesConfigCodes.CodigoTributacaoNacional] = config.CodigoTributacaoNacional.Trim(),
            [NfesConfigCodes.CodigoTributacaoMunicipal] = config.CodigoTributacaoMunicipal.Trim(),
            [NfesConfigCodes.CodigoNbs] = config.CodigoNbs.Trim(),
            [NfesConfigCodes.ServiceCode] = config.ServiceCode.Trim(),
            [NfesConfigCodes.ServiceTax] = config.ServiceTax.Trim(),
            [NfesConfigCodes.OpSimpNac] = config.OpSimpNac.Trim(),
            [NfesConfigCodes.RegEspTrib] = config.RegEspTrib.Trim(),
            [NfesConfigCodes.RegApTribSn] = config.RegApTribSn.Trim(),
            [NfesConfigCodes.PercentualTotTribSn] = config.PercentualTotTribSn.Trim(),
            [NfesConfigCodes.IncludeIbsCbs] = config.IncludeIbsCbs ? "1" : "0",
            [NfesConfigCodes.IbsCbsIndOp] = config.IbsCbsIndOp.Trim(),
            [NfesConfigCodes.IbsCbsCst] = config.IbsCbsCst.Trim(),
            [NfesConfigCodes.IbsCbsClassTrib] = config.IbsCbsClassTrib.Trim(),
            [NfesConfigCodes.IbsCbsFinNfse] = config.IbsCbsFinNfse.Trim(),
            [NfesConfigCodes.IbsCbsIndFinal] = config.IbsCbsIndFinal.Trim(),
            [NfesConfigCodes.IbsCbsIndDest] = config.IbsCbsIndDest.Trim()
        };

        if (!string.IsNullOrWhiteSpace(config.CertPassword))
            values[NfesConfigCodes.CertPassword] = config.CertPassword;

        await _sysControl.SaveValuesAsync(values, cancellationToken).ConfigureAwait(false);
        _cached = null;
    }
}
