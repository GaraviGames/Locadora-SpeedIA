using System.Globalization;

public static class CalculoLocacao
{
    private static readonly string[] FormatosData = { "d/M/yyyy", "d-M-yyyy", "d.M.yyyy", "ddMMyyyy" };

    public static bool TentarLerData(string? texto, out DateTime data)
    {
        data = default;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        texto = texto.Trim();

        if (DateTime.TryParseExact(texto, FormatosData, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var simples))
        {
            data = simples;
            return true;
        }

        if (DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var completa))
        {
            data = completa.UtcDateTime;
            return true;
        }

        return false;
    }

    private static decimal PercentualMulta(int plano)
    {
        if (plano == 7)
        {
            return 0.20m;
        }

        if (plano == 15)
        {
            return 0.40m;
        }

        return 0m;
    }

    public static decimal CalcularTotal(int plano, decimal valorDiaria, DateTime inicio, DateTime previsao, DateTime devolucao)
    {
        var diaInicio = DateOnly.FromDateTime(inicio);
        var diaPrevisto = DateOnly.FromDateTime(previsao);
        var diaDevolucao = DateOnly.FromDateTime(devolucao);

        decimal total;

        if (diaDevolucao < diaPrevisto)
        {
            var diasUsados = Math.Max(1, diaDevolucao.DayNumber - diaInicio.DayNumber + 1);
            var diasNaoUsados = plano - diasUsados;
            var valorNaoUsado = diasNaoUsados * valorDiaria;

            total = diasUsados * valorDiaria + valorNaoUsado * PercentualMulta(plano);
        }
        else
        {
            var diasExtras = diaDevolucao.DayNumber - diaPrevisto.DayNumber;
            total = plano * valorDiaria + diasExtras * 50m;
        }

        return Math.Round(total, 2);
    }
}