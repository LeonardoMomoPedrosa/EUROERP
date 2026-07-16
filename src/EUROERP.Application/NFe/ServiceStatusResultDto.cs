namespace EUROERP.Application.NFe;

/// <summary>
/// Result of SEFAZ NFe status service (retConsStatServ).
/// </summary>
public class ServiceStatusResultDto
{
    /// <summary>Código do status (ex.: 107 = Serviço em operação).</summary>
    public string CStat { get; init; } = "";
    public string XMotivo { get; init; } = "";
    public string Versao { get; init; } = "";
    /// <summary>1 = Produção, 2 = Homologação.</summary>
    public string TpAmb { get; init; } = "";
    public string VerAplic { get; init; } = "";
    public string CUf { get; init; } = "";
    /// <summary>Data/hora recebimento da consulta (formato SEFAZ).</summary>
    public string DhRecbto { get; init; } = "";
    /// <summary>Tempo médio de resposta em segundos (últimos 5 min).</summary>
    public string TMed { get; init; } = "";
    /// <summary>Data/hora prevista para retorno dos serviços (quando informada).</summary>
    public string DhRetorno { get; init; } = "";
    /// <summary>Observações da SEFAZ ao contribuinte.</summary>
    public string XObs { get; init; } = "";
}
