using System.Text.Json;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ConsumidorMotos : BackgroundService
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);
    private readonly ConnectionFactory _fabrica;
    private readonly NpgsqlDataSource _banco;
    private readonly ILogger<ConsumidorMotos> _log;

    public ConsumidorMotos(IConfiguration configuracao, NpgsqlDataSource banco, ILogger<ConsumidorMotos> log)
    {
        _fabrica = new ConnectionFactory
        {
            Uri = new Uri(configuracao.GetConnectionString("RabbitMQ")!)
        };
        _banco = banco;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken parar)
    {
        await using var conexao = await _fabrica.CreateConnectionAsync(parar);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: parar);

        // lembrete: declarar igualzinho ao publicador (mesmo nome, durable true), senão o RabbitMQ dá erro
        await canal.QueueDeclareAsync(
            queue: PublicadorMotos.NomeFila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: parar);

        var consumidor = new AsyncEventingBasicConsumer(canal);

        consumidor.ReceivedAsync += async (_, mensagem) =>
        {
            try
            {
                var moto = JsonSerializer.Deserialize<Motos>(mensagem.Body.ToArray(), OpcoesJson);

                if (moto is not null && moto.Ano == 2024)
                {
                    await GuardarAsync(moto);
                    _log.LogInformation("Moto de 2024 cadastrada: {Id} - {Modelo} - {Placa}",
                    moto.Identificador, moto.Modelo, moto.Placa);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"*** NOTIFICAÇÃO: moto de 2024 cadastrada -> {moto.Identificador} | {moto.Modelo} | {moto.Placa} ***");
                    Console.ResetColor();
                }

                // lembrete: só confirmo (ack) DEPOIS de guardar, assim se o banco falhar a mensagem não some
                await canal.BasicAckAsync(mensagem.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Erro ao processar a mensagem da fila");
                await canal.BasicNackAsync(mensagem.DeliveryTag, false, false);
            }
        };

        // autoAck false = eu mesmo confirmo lá em cima, não o RabbitMQ
        await canal.BasicConsumeAsync(PublicadorMotos.NomeFila, autoAck: false, consumer: consumidor);

        // lembrete: sem isso o método termina e o consumidor para de ouvir
        await Task.Delay(Timeout.Infinite, parar);
    }

    private async Task GuardarAsync(Motos moto)
    {
        await using var comando = _banco.CreateCommand(
            "INSERT INTO notificacoes_motos (identificador, ano, modelo, placa) VALUES (@identificador, @ano, @modelo, @placa)");

        comando.Parameters.AddWithValue("identificador", moto.Identificador);
        comando.Parameters.AddWithValue("ano", moto.Ano);
        comando.Parameters.AddWithValue("modelo", moto.Modelo);
        comando.Parameters.AddWithValue("placa", moto.Placa);

        await comando.ExecuteNonQueryAsync();
    }
}