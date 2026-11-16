using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Entregador
{
    [Required]
    [StringLength(50)]
    [JsonPropertyName("identificador")]
    public string Identificador { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [JsonPropertyName("cnpj")]
    public string Cnpj { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("data_nascimento")]
    public string? DataNascimento { get; set; }

    [Required]
    [StringLength(30)]
    [JsonPropertyName("numero_cnh")]
    public string NumeroCnh { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [JsonPropertyName("tipo_cnh")]
    public string TipoCnh { get; set; } = string.Empty;

    [JsonPropertyName("imagem_cnh")]
    public string? ImagemCnh { get; set; }
}