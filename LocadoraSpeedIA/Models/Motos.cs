

using System.ComponentModel.DataAnnotations;

public class Motos
{
    [Required]
    [StringLength(50)]
    public string Identificador { get; set; } = string.Empty;

    [Range(1000, 9999)]
    public int Ano { get; set; }

    [Required]
    [StringLength(30)]
    public string Modelo { get; set; } = string.Empty;

    [Required]
    public string Placa { get; set; } = string.Empty;
}