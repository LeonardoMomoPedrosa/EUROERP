using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EUROERP.Infrastructure.Nfes;

internal static class NfesRpsSignature
{
    public static byte[] BuildCancelServicosSignature(X509Certificate2 certificate, string inscricaoMunicipal, string nfesNo)
    {
        var key = NfesTextHelper.LeftZero(NfesTextHelper.CleanDigits(inscricaoMunicipal), 8)
            + NfesTextHelper.LeftZero(NfesTextHelper.CleanDigits(nfesNo), 12);
        return SignSha1Rsa(certificate, key);
    }

    public static byte[] BuildRpsAssinatura(
        X509Certificate2 certificate,
        string inscricaoMunicipal,
        string serieRps,
        string numeroRps,
        DateTime emitDate,
        decimal netAmount,
        int cpfCnpjInd,
        string cpfCnpj,
        string codigoServico)
    {
        var amountCents = decimal.Round(decimal.Round(netAmount, 2) * 100, 0);
        var key = new StringBuilder();
        key.Append(NfesTextHelper.LeftZero(NfesTextHelper.CleanDigits(inscricaoMunicipal), 8));
        key.Append(NfesTextHelper.RightSpaces(serieRps, 5));
        key.Append(NfesTextHelper.LeftZero(numeroRps, 12));
        key.Append(emitDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        key.Append('T'); // tributacao
        key.Append('N'); // status RPS normal
        key.Append('N'); // ISS retido
        key.Append(NfesTextHelper.LeftZero(amountCents.ToString(CultureInfo.InvariantCulture), 15));
        key.Append("000000000000000");
        key.Append(NfesTextHelper.LeftZero(codigoServico, 5));
        key.Append(cpfCnpjInd.ToString(CultureInfo.InvariantCulture));
        key.Append(NfesTextHelper.LeftZero(NfesTextHelper.CleanDigits(cpfCnpj), 14));

        return SignSha1Rsa(certificate, key.ToString());
    }

    private static byte[] SignSha1Rsa(X509Certificate2 certificate, string input)
    {
#pragma warning disable SYSLIB0028
        var rsa = certificate.GetRSAPrivateKey()
            ?? (RSA?)certificate.PrivateKey
            ?? throw new InvalidOperationException("Certificado sem chave privada RSA.");
#pragma warning restore SYSLIB0028
        var data = Encoding.UTF8.GetBytes(input);
        return rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }
}
