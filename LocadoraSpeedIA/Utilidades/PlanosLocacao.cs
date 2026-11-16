public static class PlanosLocacao
{
    private static readonly Dictionary<int, decimal> ValorPorDia = new()
    {
        { 7, 30m },
        { 15, 28m },
        { 30, 22m },
        { 45, 20m },
        { 50, 18m }
    };

    public static bool TentarValorDiaria(int plano, out decimal valor)
    {
        return ValorPorDia.TryGetValue(plano, out valor);
    }
}