public static class ImagemCnh
{
    private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static bool TentarLer(string? base64, out byte[] bytes, out string extensao)
    {
        bytes = Array.Empty<byte>();
        extensao = string.Empty;

        if (string.IsNullOrWhiteSpace(base64))
        {
            return false;
        }

        var texto = base64.Trim();

        if (texto.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var virgula = texto.IndexOf(',');
            if (virgula < 0)
            {
                return false;
            }
            texto = texto[(virgula + 1)..];
        }

        try
        {
            bytes = Convert.FromBase64String(texto);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(AssinaturaPng))
        {
            extensao = ".png";
            return true;
        }

        if (bytes.Length > 14 && bytes[0] == 0x42 && bytes[1] == 0x4D)
        {
            extensao = ".bmp";
            return true;
        }

        return false;
    }
}