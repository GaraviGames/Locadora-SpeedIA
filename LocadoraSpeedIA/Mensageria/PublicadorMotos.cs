using System.Text.Json;
using RabbitMQ.Client;

public class PublicadorMotos
{
    // lembrete: o consumidor do 9B vai usar esse mesmo nome de fila
    public const string NomeFila = "moto-cadastrada";

    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);
    private readonly ConnectionFactory _fabrica;

    public PublicadorMotos(IConfiguration configuracao)
    {
        _fabrica = new ConnectionFactory
        {
            Uri = new Uri(configuracao.GetConnectionString("RabbitMQ")!)
        };
    }

    public async Task PublicarMotoCadastradaAsync(Motos moto)
    {
        await using var conexao = await _fabrica.CreateConnectionAsync();
        await using var canal = await conexao.CreateChannelAsync();

        // lembrete: se a fila já existe com essas mesmas configurações não acontece nada
        // se alguém declarar diferente depois (ex: durable false), o RabbitMQ dá erro
        await canal.QueueDeclareAsync(
            queue: NomeFila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var corpo = JsonSerializer.SerializeToUtf8Bytes(moto, OpcoesJson);

        await canal.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: NomeFila,
            mandatory: false,
            basicProperties: new BasicProperties { Persistent = true },
            body: corpo);
    }
}