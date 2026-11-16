using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class AtualizacaoCnh
{
    [Required]
    [JsonPropertyName("imagem_cnh")]
    public string ImagemCnh { get; set; } = string.Empty;
}