using System.Globalization;
using System.Text.RegularExpressions;

public static class FormatoEntregador
{
    private static readonly Regex SoNumerosEPontuacao = new(@"^[0-9.\-/\s]+$");

    private static readonly string[] FormatosData =
    {
        "yyyy-MM-dd", "d/M/yyyy", "d-M-yyyy", "d.M.yyyy", "ddMMyyyy"
    };

    private static bool TentarSoNumeros(string? texto, int quantidade, out string numeros)
    {
        numeros = string.Empty;

        if (string.IsNullOrWhiteSpace(texto) || !SoNumerosEPontuacao.IsMatch(texto))
        {
            return false;
        }

        var limpo = Regex.Replace(texto, @"[^0-9]", "");

        if (limpo.Length != quantidade)
        {
            return false;
        }

        numeros = limpo;
        return true;
    }

    public static bool TentarCnpj(string? texto, out string cnpj)
    {
        cnpj = string.Empty;

        if (!TentarSoNumeros(texto, 14, out var n))
        {
            return false;
        }

        cnpj = $"{n[..2]}.{n[2..5]}.{n[5..8]}/{n[8..12]}-{n[12..]}";
        return true;
    }

    public static bool TentarCnh(string? texto, out string cnh)
    {
        return TentarSoNumeros(texto, 11, out cnh);
    }

    public static bool TentarTipoCnh(string? texto, out string tipo)
    {
        tipo = string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpo = Regex.Replace(texto.ToUpperInvariant(), @"[\s+]", "");

        if (limpo == "A" || limpo == "B")
        {
            tipo = limpo;
            return true;
        }

        if (limpo == "AB" || limpo == "BA")
        {
            tipo = "A+B";
            return true;
        }

        return false;
    }

    public static bool TentarDataNascimento(string? texto, out DateOnly data)
    {
        data = default;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        texto = texto.Trim();

        var achou = DateOnly.TryParseExact(
            texto, FormatosData, CultureInfo.InvariantCulture, DateTimeStyles.None, out data);

        if (!achou && texto.Contains('T')
            && DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var completa))
        {
            data = DateOnly.FromDateTime(completa.Date);
            achou = true;
        }

        return achou && data <= DateOnly.FromDateTime(DateTime.Today);
    }
}