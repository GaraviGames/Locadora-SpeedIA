public static class AvisoTerminal
{
    public static void Destaque(string texto)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(texto);
        Console.ResetColor();
    }
}