using System.Text.RegularExpressions;

public static class PlacaBrasil
{
    private static readonly Regex Padrao =
        new(@"^([A-Z]{3})-?([0-9]{4}|[0-9][A-Z][0-9]{2})$");

    public static bool TentarNormalizar(string? texto, out string placa)
    {
        placa = string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var resultado = Padrao.Match(texto.Trim().ToUpperInvariant());

        if (!resultado.Success)
        {
            return false;
        }

        placa = resultado.Groups[1].Value + "-" + resultado.Groups[2].Value;
        return true;
    }
}