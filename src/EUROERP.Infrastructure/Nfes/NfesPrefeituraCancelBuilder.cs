using System.Globalization;
using EUROERP.Infrastructure.Nfes.PrefeituraSp;

namespace EUROERP.Infrastructure.Nfes;

internal static class NfesPrefeituraCancelBuilder
{
    public static PedidoCancelamentoNFe Build(NfesConfigSnapshot config, string nfesNo, string nfesCheckCode, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        var emitCnpj = NfesTextHelper.CleanDigits(config.EmitCnpj);
        var emitIm = NfesTextHelper.CleanDigits(config.EmitIMun);
        if (string.IsNullOrWhiteSpace(emitCnpj) || string.IsNullOrWhiteSpace(emitIm))
            throw new InvalidOperationException("Configure CNPJ e Inscrição Municipal em Diretoria > Configuração NFES.");

        if (!long.TryParse(emitIm, NumberStyles.Integer, CultureInfo.InvariantCulture, out var inscricaoPrestador))
            throw new InvalidOperationException("Inscrição Municipal inválida.");

        var nfesNoText = NfesTextHelper.CleanDigits(nfesNo);
        if (!long.TryParse(nfesNoText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeroNFe))
            throw new InvalidOperationException("Número da NFS-e inválido.");

        var checkCode = nfesCheckCode.Trim();
        if (string.IsNullOrWhiteSpace(checkCode))
            throw new InvalidOperationException("Pedido sem código de verificação NFS-e.");

        return new PedidoCancelamentoNFe
        {
            Cabecalho = new PedidoCancelamentoNFeCabecalho
            {
                CPFCNPJRemetente = new tpCPFCNPJ
                {
                    ItemElementName = ItemChoiceType.CNPJ,
                    Item = emitCnpj
                },
                transacao = false
            },
            Detalhe =
            [
                new PedidoCancelamentoNFeDetalhe
                {
                    ChaveNFe = new tpChaveNFe
                    {
                        InscricaoPrestador = inscricaoPrestador,
                        NumeroNFe = numeroNFe,
                        CodigoVerificacao = checkCode
                    },
                    AssinaturaCancelamento = NfesRpsSignature.BuildCancelServicosSignature(certificate, emitIm, nfesNoText)
                }
            ]
        };
    }
}
