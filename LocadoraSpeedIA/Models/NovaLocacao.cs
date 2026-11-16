using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class NovaLocacao
{
    [StringLength(50)]
    [JsonPropertyName("identificador")]
    public string? Identificador { get; set; }

    [Required]
    [StringLength(50)]
    [JsonPropertyName("entregador_id")]
    public string EntregadorId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [JsonPropertyName("moto_id")]
    public string MotoId { get; set; } = string.Empty;

    [JsonPropertyName("data_inicio")]
    public DateTime? DataInicio { get; set; }

    [JsonPropertyName("data_termino")]
    public DateTime? DataTermino { get; set; }

    [JsonPropertyName("data_previsao_termino")]
    public DateTime? DataPrevisaoTermino { get; set; }

    [JsonPropertyName("plano")]
    public int Plano { get; set; }
}