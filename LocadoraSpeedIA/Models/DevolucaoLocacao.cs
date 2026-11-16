using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class DevolucaoLocacao
{
    [Required]
    [JsonPropertyName("data_devolucao")]
    public string? DataDevolucao { get; set; }
}