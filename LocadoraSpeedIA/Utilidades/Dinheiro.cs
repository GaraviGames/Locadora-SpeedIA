using System.Globalization;

public static class Dinheiro
{
    private static readonly CultureInfo Brasil = new("pt-BR");

    public static string Reais(decimal valor)
    {
        return "R$ " + valor.ToString("N2", Brasil);
    }
}