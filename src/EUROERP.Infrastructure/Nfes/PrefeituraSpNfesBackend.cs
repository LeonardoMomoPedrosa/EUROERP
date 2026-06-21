using System.Globalization;
using System.Text;
using EUROERP.Infrastructure.Nfes.PrefeituraSp;

namespace EUROERP.Infrastructure.Nfes;

/// <summary>Prefeitura de São Paulo — PedidoEnvioLoteRPS (legacy layout).</summary>
public sealed class PrefeituraSpNfesBackend : INfesEmissionBackend
{
    private const string SerieRps = "0001";

    private readonly INfesConfigProvider _configProvider;
    private readonly INfesCertificateProvider _certificateProvider;
    private readonly INfesPrefeituraClient _prefeituraClient;

    public PrefeituraSpNfesBackend(
        INfesConfigProvider configProvider,
        INfesCertificateProvider certificateProvider,
        INfesPrefeituraClient prefeituraClient)
    {
        _configProvider = configProvider;
        _certificateProvider = certificateProvider;
        _prefeituraClient = prefeituraClient;
    }

    public string ProviderKey => "PrefeituraSp";

    public async Task<NfesEmissionOutcome> EmitAsync(NfesEmissionWorkItem work, CancellationToken cancellationToken)
    {
        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var emitCnpj = config.EmitCnpj;
        var emitIm = config.EmitIMun;
        var serviceCode = config.ServiceCode;
        var serviceTax = config.ServiceTax;

        if (string.IsNullOrWhiteSpace(emitCnpj) || string.IsNullOrWhiteSpace(emitIm))
            return NfesEmissionOutcome.Fail("Configure CNPJ e Inscrição Municipal em Diretoria > Configuração NFES.");

        var cert = _certificateProvider.GetCertificate();
        var personType = work.Client.PersonType?.Trim().ToUpperInvariant() ?? "J";
        var cpfCnpj = NfesTextHelper.CleanDigits(work.Client.CnpjPf ?? "");
        var cpfCnpjInd = personType == "F" ? 1 : 2;
        var discriminacao = BuildDiscriminacao(work);

        var envio = new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                CPFCNPJRemetente = new tpCPFCNPJ
                {
                    ItemElementName = ItemChoiceType.CNPJ,
                    Item = NfesTextHelper.CleanDigits(emitCnpj)
                },
                dtInicio = DateTime.Today,
                dtFim = DateTime.Today,
                QtdRPS = 1,
                ValorTotalServicos = work.NetAmount
            },
            RPS = new[]
            {
                BuildRps(work.RpsNumber, work.NetAmount, serviceCode, serviceTax, work.Client, personType, cpfCnpj, discriminacao, cert, emitIm, cpfCnpjInd)
            }
        };

        var xmlDoc = NfesXmlSupport.SerializePedido(envio);
        xmlDoc = NfesXmlSupport.SignRpsDocument(xmlDoc, cert);

        var baseDir = config.XmlPath;
        var orderFolder = Path.Combine(baseDir, "S" + work.OrderId);
        Directory.CreateDirectory(orderFolder);
        var chave = work.OrderId.ToString(CultureInfo.InvariantCulture);
        var xmlPath = Path.Combine(orderFolder, chave + "-nfes.xml");
        var xmlUtf8 = xmlDoc.OuterXml.Replace("utf-16", "utf-8", StringComparison.Ordinal);
        await File.WriteAllTextAsync(xmlPath, xmlUtf8, cancellationToken).ConfigureAwait(false);

        var xsdPath = Path.Combine(config.SchemaPath, "nfes", "PedidoEnvioLoteRPS_v01.xsd");
        try { NfesXmlSupport.ValidatePedidoXml(xsdPath, xmlPath); }
        catch (Exception ex) { return NfesEmissionOutcome.Fail(ex.Message); }

        var responseXml = await _prefeituraClient.SendLoteRpsAsync(xmlUtf8, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(orderFolder, chave + "-rec.xml"), responseXml, cancellationToken).ConfigureAwait(false);

        RetornoEnvioLoteRPS response;
        try { response = NfesXmlSupport.Deserialize<RetornoEnvioLoteRPS>(responseXml); }
        catch (Exception ex) { return NfesEmissionOutcome.Fail("Resposta inválida da Prefeitura: " + ex.Message); }

        if (response.Cabecalho == null || !response.Cabecalho.Sucesso)
        {
            var sb = new StringBuilder();
            if (response.Erro != null)
                foreach (var e in response.Erro)
                    sb.AppendLine($"Erro {e.Codigo}: {e.Descricao}");
            if (response.Alerta != null)
                foreach (var a in response.Alerta)
                    sb.AppendLine($"Alerta {a.Codigo}: {a.Descricao}");
            return NfesEmissionOutcome.Fail(sb.Length > 0 ? sb.ToString() : "Prefeitura rejeitou o lote RPS.");
        }

        if (response.ChaveNFeRPS == null || response.ChaveNFeRPS.Length == 0)
            return NfesEmissionOutcome.Fail("Resposta sem chave NFES.");

        var chaveNfe = response.ChaveNFeRPS[0];
        return new NfesEmissionOutcome
        {
            Success = true,
            NfesNo = chaveNfe.ChaveNFe.NumeroNFe.ToString(CultureInfo.InvariantCulture),
            RpsNo = chaveNfe.ChaveRPS.NumeroRPS.ToString(CultureInfo.InvariantCulture),
            CheckCode = chaveNfe.ChaveNFe.CodigoVerificacao ?? "",
            XmlPath = xmlPath,
            Message = "NFe de Serviços enviada com sucesso."
        };
    }

    private static tpRPS BuildRps(
        int rpsNumber,
        decimal netAmount,
        string serviceCode,
        decimal serviceTax,
        NfesClientRow client,
        string personType,
        string cpfCnpj,
        string discriminacao,
        System.Security.Cryptography.X509Certificates.X509Certificate2 cert,
        string emitIm,
        int cpfCnpjInd)
    {
        var rps = new tpRPS
        {
            AliquotaServicos = serviceTax / 100m,
            CodigoServico = int.Parse(serviceCode, CultureInfo.InvariantCulture),
            CPFCNPJTomador = new tpCPFCNPJ
            {
                ItemElementName = personType == "F" ? ItemChoiceType.CPF : ItemChoiceType.CNPJ,
                Item = cpfCnpj
            },
            DataEmissao = DateTime.Today,
            Discriminacao = discriminacao,
            EmailTomador = NfesTextHelper.Substring((client.Email ?? "").Split('/')[0].Trim(), 60),
            EnderecoTomador = BuildEndereco(client),
            ISSRetido = false,
            RazaoSocialTomador = NfesTextHelper.CleanStringToXml(client.SocialName ?? "", 60),
            StatusRPS = tpStatusNFe.N,
            TipoRPS = tpTipoRPS.RPS,
            TributacaoRPS = tpTributacaoNFe.T,
            ValorServicos = netAmount,
            ChaveRPS = new tpChaveRPS
            {
                InscricaoPrestador = long.Parse(NfesTextHelper.CleanDigits(emitIm), CultureInfo.InvariantCulture),
                NumeroRPS = rpsNumber,
                SerieRPS = SerieRps
            },
            Assinatura = NfesRpsSignature.BuildRpsAssinatura(cert, emitIm, SerieRps, rpsNumber.ToString(CultureInfo.InvariantCulture),
                DateTime.Today, netAmount, cpfCnpjInd, cpfCnpj, serviceCode)
        };

        ApplyInscricoes(rps, client);
        return rps;
    }

    private static void ApplyInscricoes(tpRPS rps, NfesClientRow client)
    {
        var ie = NfesTextHelper.CleanDigits(client.StateInscr ?? "");
        if (ie.Length > 2 && !ie.Equals("ISENTO", StringComparison.OrdinalIgnoreCase))
        {
            rps.InscricaoEstadualTomador = long.Parse(ie, CultureInfo.InvariantCulture);
            rps.InscricaoEstadualTomadorSpecified = true;
        }

        var im = NfesTextHelper.CleanDigits(client.MunInscr ?? "");
        if (im.Length > 2 && !im.Equals("ISENTO", StringComparison.OrdinalIgnoreCase))
        {
            rps.InscricaoMunicipalTomador = long.Parse(im, CultureInfo.InvariantCulture);
            rps.InscricaoMunicipalTomadorSpecified = true;
        }
    }

    private static tpEndereco BuildEndereco(NfesClientRow client)
    {
        var ender = new tpEndereco
        {
            Bairro = NfesTextHelper.CleanStringToXml(client.AddressBlock ?? "", 60),
            CEP = int.Parse(NfesTextHelper.CleanDigits(client.AddressZipCode ?? "0"), CultureInfo.InvariantCulture),
            CEPSpecified = true,
            Cidade = int.Parse(client.CMun ?? "0", CultureInfo.InvariantCulture),
            CidadeSpecified = true,
            Logradouro = NfesTextHelper.CleanStringToXml(client.AddressStreet ?? "", 60),
            NumeroEndereco = NfesTextHelper.Substring(client.AddressNumber ?? "", 60),
            TipoLogradouro = "",
            UF = client.AddressStateCode ?? "SP"
        };
        if (!string.IsNullOrWhiteSpace(client.AddressComplement))
            ender.ComplementoEndereco = NfesTextHelper.CleanStringToXml(client.AddressComplement, 60);
        return ender;
    }

    private static string BuildDiscriminacao(NfesEmissionWorkItem work)
    {
        var sb = new StringBuilder();
        foreach (var line in work.ServiceLines)
        {
            sb.Append(line.Name).Append(" - Qtd: ").Append(line.Quantity.ToString(CultureInfo.InvariantCulture))
                .Append(" - Unit.R$ ").Append(line.UnitPrice.ToString("N2", CultureInfo.InvariantCulture))
                .Append(" - Total R$ ").Append(line.TotalPrice.ToString("N2", CultureInfo.InvariantCulture))
                .Append('\n');
        }
        sb.Append("OS ").Append(work.OrderId).Append('\n');
        if (!string.IsNullOrWhiteSpace(work.Order.CarPlate))
            sb.Append(work.Order.CarDescription).Append(" Placa: ").Append(work.Order.CarPlate).Append('\n');
        if (!string.IsNullOrWhiteSpace(work.Order.CarProblem))
            sb.Append("Dados adicionais:  ").Append(work.Order.CarProblem).Append('\n');
        if (work.BtrDueDates.Count > 0)
        {
            var venc = "Vencs: " + string.Join("  ", work.BtrDueDates.Select(d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
            foreach (var line in NfesTextHelper.CutLines(venc, 120))
                sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }
}
