using System.ComponentModel.DataAnnotations;

public class AlteracaoPlaca
{
    [Required]
    public string Placa { get; set; } = string.Empty;
}