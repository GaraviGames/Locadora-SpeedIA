//parte 1 do projeto
 
using System.ComponentModel.DataAnnotations;
using Npgsql;
 
var builder = WebApplication.CreateBuilder(args);
 
// pega o endereço do banco lá no appsettings.json
// Lembrando o nome "Postgres" tem que ser igual ao mencionado no codigo
var conexao = builder.Configuration.GetConnectionString("Postgres");
 
// cria a entrada do banco uma vez só e deixa liberada pro API
builder.Services.AddSingleton(NpgsqlDataSource.Create(conexao!));
 
// cria o mensageiro do RabbitMQ uma vez só e deixa liberado pros API
// lembrete: o nome "RabbitMQ" tem que ser igualz ao mencionado no codigo
builder.Services.AddSingleton<PublicadorMotos>();

// liga o consumidor, ele fica rodando junto com a API lendo a fila do RabbitMQ
// ---> se o RabbitMQ estiver desligado a API não liga, verificar docker
builder.Services.AddHostedService<ConsumidorMotos>();
 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Parte 12 do projeto

// lembrete: faz o JSON quebrado (ex: "ano": "abc") virar um erro que eu consigo pegar, senão o 400 sai sem mensagem
builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = true);
 
var app = builder.Build();

// pega o erro de JSON quebrado e devolve o 400 com a mensagem do swagger
// lembrete: tem que ficar logo depois do Build, ANTES dos guichês, senão não pega nada
app.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo();
    }
    catch (BadHttpRequestException)
    {
        if (!contexto.Response.HasStarted)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new { mensagem = "Dados inválidos" });
        }
    }
});
 
app.UseSwagger();
app.UseSwaggerUI();
 
// guichê de cadastrar moto
// o "Motos moto" é o JSON que chega já virando a ficha
app.MapPost("/motos", async (Motos moto, NpgsqlDataSource banco, PublicadorMotos publicador) =>
{
    var erros = new List<ValidationResult>();
 
    // confere as regras da ficha
    // se falhar envia o erro 400 aqui e não altera nada no banco
    var valido = Validator.TryValidateObject(moto, new ValidationContext(moto), erros, true);
 
    if (!valido)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

//Parte 7 do projeto

    // só aceita moto de 1990 até o ano atual (regra do Renavam brasileiro)
    // ---> o DateTime.Now.Year pega o ano do computador sozinho
    // fora disso devolve 400 aqui, então não chega a interagir no banco e nem no RabbitMQ
    if (moto.Ano < 1990 || moto.Ano > DateTime.Now.Year)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    // Parte 2 do Projeto
 
    // formtação para arruma a placa e já confere se é padrão
    // lembrete: lá embaixo usar a variável "placa" e NÃO o moto.Placa, senão grava a placa torta
    if (!PlacaBrasil.TentarNormalizar(moto.Placa, out var placa))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    try
    {
        // os @nomes são espaços reservados
        await using var comando = banco.CreateCommand(
            "INSERT INTO motos (identificador, ano, modelo, placa) VALUES (@identificador, @ano, @modelo, @placa)");
 
        comando.Parameters.AddWithValue("identificador", moto.Identificador);
        comando.Parameters.AddWithValue("ano", moto.Ano);
        comando.Parameters.AddWithValue("modelo", moto.Modelo);
        comando.Parameters.AddWithValue("placa", placa);
 
        await comando.ExecuteNonQueryAsync();
    }
    // placa ou identificador repetido: quem barra é o próprio banco sql
    // eu só pego esse erro e devolvo o 400 do swagger
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    //Parte 5 do projeto
 
    // a moto já foi salva, agora sim avisa o RabbitMQ que tem moto nova
    // ---> tem que ficar DEPOIS do insert, senão cria notificação de motos não salva
    // troco o moto.Placa pela placa arrumada pra o aviso ir com a placa certa
    moto.Placa = placa;
    await publicador.PublicarMotoCadastradaAsync(moto);
 
    // 201 sem corpo nenhum, padrão do teste
    return Results.StatusCode(StatusCodes.Status201Created);
});
 
// guichê de listar motos, com filtro 
app.MapGet("/motos", async (string? placa, NpgsqlDataSource banco) =>
{
    var motos = new List<Motos>();
 
    if (placa is not null)
    {
        // passa o filtro pelo mesmo conferidor
        // se não for uma é placa válida, devolve lista vazia mesmo (200)
        if (!PlacaBrasil.TentarNormalizar(placa, out var normalizada))
        {
            return Results.Ok(motos);
        }
        placa = normalizada;
    }
 
    // monto o sql aos poucos porque o WHERE só entra se passar pelo filtro
    var sql = "SELECT identificador, ano, modelo, placa FROM motos";
    if (placa is not null)
    {
        sql += " WHERE placa = @placa";
    }
    sql += " ORDER BY identificador";
 
    await using var comando = banco.CreateCommand(sql);
    if (placa is not null)
    {
        comando.Parameters.AddWithValue("placa", placa);
    }
 
    await using var leitor = await comando.ExecuteReaderAsync();
 
    // o leitor entrega uma linha por vez
    // lembrando os números 0,1,2,3 são a ordem das colunas lá no SELECT
    while (await leitor.ReadAsync())
    {
        motos.Add(new Motos
        {
            Identificador = leitor.GetString(0),
            Ano = leitor.GetInt32(1),
            Modelo = leitor.GetString(2),
            Placa = leitor.GetString(3)
        });
    }
 
    return Results.Ok(motos);
});
 
//Parte 3 do projeto
 
// guichê de buscar uma moto pelo id (o {id} é a parte variável do endereço)
app.MapGet("/motos/{id}", async (string id, NpgsqlDataSource banco) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { mensagem = "Request mal formada" });
    }
 
    await using var comando = banco.CreateCommand(
        "SELECT identificador, ano, modelo, placa FROM motos WHERE identificador = @id");
    comando.Parameters.AddWithValue("id", id);
 
    await using var leitor = await comando.ExecuteReaderAsync();
 
    // se não leu nenhuma linha, a moto não existe, então é 404
    if (!await leitor.ReadAsync())
    {
        return Results.NotFound(new { mensagem = "Moto não encontrada" });
    }
 
    var moto = new Motos
    {
        Identificador = leitor.GetString(0),
        Ano = leitor.GetInt32(1),
        Modelo = leitor.GetString(2),
        Placa = leitor.GetString(3)
    };
 
    return Results.Ok(moto);
});
 
// guichê de alterar a placa, só a placa mesmo
app.MapPut("/motos/{id}/placa", async (string id, AlteracaoPlaca pedido, NpgsqlDataSource banco) =>
{
    var erros = new List<ValidationResult>();
    var valido = Validator.TryValidateObject(pedido, new ValidationContext(pedido), erros, true);
 
    if (!valido || string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    if (!PlacaBrasil.TentarNormalizar(pedido.Placa, out var placa))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    try
    {
        await using var comando = banco.CreateCommand(
            "UPDATE motos SET placa = @placa WHERE identificador = @id");
        comando.Parameters.AddWithValue("placa", placa);
        comando.Parameters.AddWithValue("id", id);
 
        var linhas = await comando.ExecuteNonQueryAsync();
 
        // ---> se mudou 0 linhas, o id não existe
        // o swagger só tem true ou false (200 ou 400)
        if (linhas == 0)
        {
            return Results.BadRequest(new { mensagem = "Dados inválidos" });
        }
    }
    // placa nova já usada por outra moto: o banco recusa e devolve o erro 400
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    return Results.Ok(new { mensagem = "Placa modificada com sucesso" });
});
 
//Parte 4 do projeto
 
// guichê de remover moto
app.MapDelete("/motos/{id}", async (string id, NpgsqlDataSource banco) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    try
    {
        await using var comando = banco.CreateCommand(
            "DELETE FROM motos WHERE identificador = @id");
        comando.Parameters.AddWithValue("id", id);
 
        var linhas = await comando.ExecuteNonQueryAsync();
 
        // lembrete: 0 linhas apagadas = não existe moto com esse id
        // o swagger só tem 200 e 400 aqui, então vira 400
        if (linhas == 0)
        {
            return Results.BadRequest(new { mensagem = "Dados inválidos" });
        }
    }
    // regra teste: se a moto tem locação, quem barra é o banco (por causa do REFERENCES)
    // aqui somente identifica o erro e devolve como 400
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }
 
    return Results.Ok();
});

//Parte 8 do projeto

// guichê de cadastrar entregador
app.MapPost("/entregadores", async (Entregador entregador, NpgsqlDataSource banco) =>
{
    var erros = new List<ValidationResult>();

    // confere o que é obrigatório e o tamanho dos campos
    var valido = Validator.TryValidateObject(entregador, new ValidationContext(entregador), erros, true);

    if (!valido)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    // arruma cada dado (pontos e barras, AB vira A+B, data) e já confere se tá certo
    // lembrete: lá embaixo usar as variáveis arrumadas (cnpj, cnh, tipoCnh, dataNascimento) e NÃO o que veio no entregador
    if (!FormatoEntregador.TentarCnpj(entregador.Cnpj, out var cnpj)
        || !FormatoEntregador.TentarCnh(entregador.NumeroCnh, out var cnh)
        || !FormatoEntregador.TentarTipoCnh(entregador.TipoCnh, out var tipoCnh)
        || !FormatoEntregador.TentarDataNascimento(entregador.DataNascimento, out var dataNascimento))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    try
    {
        await using var comando = banco.CreateCommand(
            "INSERT INTO entregadores (identificador, nome, cnpj, data_nascimento, numero_cnh, tipo_cnh) " +
            "VALUES (@identificador, @nome, @cnpj, @data_nascimento, @numero_cnh, @tipo_cnh)");

        comando.Parameters.AddWithValue("identificador", entregador.Identificador);
        comando.Parameters.AddWithValue("nome", entregador.Nome);
        comando.Parameters.AddWithValue("cnpj", cnpj);
        comando.Parameters.AddWithValue("data_nascimento", dataNascimento);
        comando.Parameters.AddWithValue("numero_cnh", cnh);
        comando.Parameters.AddWithValue("tipo_cnh", tipoCnh);

        await comando.ExecuteNonQueryAsync();
    }
    // identificador, cnpj ou cnh repetido: quem barra é o banco (UNIQUE), eu só devolvo o 400
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    return Results.StatusCode(StatusCodes.Status201Created);
});

//Parte 9 do projeto

// guichê de enviar a foto da cnh (a foto vai pro disco, no banco só fica o caminho)
app.MapPost("/entregadores/{id}/cnh", async (string id, AtualizacaoCnh pedido, NpgsqlDataSource banco, IWebHostEnvironment ambiente) =>
{
    var erros = new List<ValidationResult>();
    var valido = Validator.TryValidateObject(pedido, new ValidationContext(pedido), erros, true);

    if (!valido || string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    // transforma o base64 em bytes e confere se é png ou bmp (olhando os primeiros bytes do arquivo)
    if (!ImagemCnh.TentarLer(pedido.ImagemCnh, out var bytes, out var extensao))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    // vê se o entregador existe e já pega o caminho da foto antiga (se tiver) pra apagar depois
    string? caminhoAntigo;
    await using (var busca = banco.CreateCommand(
        "SELECT caminho_imagem_cnh FROM entregadores WHERE identificador = @id"))
    {
        busca.Parameters.AddWithValue("id", id);
        await using var leitor = await busca.ExecuteReaderAsync();

        if (!await leitor.ReadAsync())
        {
            return Results.BadRequest(new { mensagem = "Dados inválidos" });
        }

        caminhoAntigo = leitor.IsDBNull(0) ? null : leitor.GetString(0);
    }

    // lembrete: o nome do arquivo é um código aleatório (Guid), NUNCA usar o id que veio na URL
    // senão alguém manda "../" no id e salva a foto em qualquer pasta do computador
    var nomeArquivo = Guid.NewGuid().ToString("N") + extensao;
    var caminhoRelativo = Path.Combine("ArquivosCnh", nomeArquivo);
    var caminhoCompleto = Path.Combine(ambiente.ContentRootPath, caminhoRelativo);

    Directory.CreateDirectory(Path.Combine(ambiente.ContentRootPath, "ArquivosCnh"));
    await File.WriteAllBytesAsync(caminhoCompleto, bytes);

    await using var atualiza = banco.CreateCommand(
        "UPDATE entregadores SET caminho_imagem_cnh = @caminho WHERE identificador = @id");
    atualiza.Parameters.AddWithValue("caminho", caminhoRelativo);
    atualiza.Parameters.AddWithValue("id", id);
    await atualiza.ExecuteNonQueryAsync();

    // troca de foto: apaga a antiga pra não ficar arquivo sobrando
    if (caminhoAntigo is not null)
    {
        var antigo = Path.Combine(ambiente.ContentRootPath, caminhoAntigo);
        if (File.Exists(antigo))
        {
            File.Delete(antigo);
        }
    }

    return Results.StatusCode(StatusCodes.Status201Created);
});

//Parte 10 do projeto

// guichê de alugar moto
app.MapPost("/locacao", async (NovaLocacao pedido, NpgsqlDataSource banco) =>
{
    var erros = new List<ValidationResult>();
    var valido = Validator.TryValidateObject(pedido, new ValidationContext(pedido), erros, true);

    if (!valido)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    // só tem plano de 7, 15, 30, 45 e 50 dias, o valor da diária vem do plano
    if (!PlanosLocacao.TentarValorDiaria(pedido.Plano, out var valorDiaria))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    // lembrete: só entregador com CNH categoria A (A ou A+B) pode alugar
    // entregador que não existe também cai aqui, porque o tipo vem nulo
    await using (var buscaTipo = banco.CreateCommand(
        "SELECT tipo_cnh FROM entregadores WHERE identificador = @id"))
    {
        buscaTipo.Parameters.AddWithValue("id", pedido.EntregadorId);
        var tipoCnh = await buscaTipo.ExecuteScalarAsync() as string;

        if (tipoCnh != "A" && tipoCnh != "A+B")
        {
            return Results.BadRequest(new { mensagem = "Dados inválidos" });
        }
    }

    // se veio identificador no JSON usa ele, senão gera um
    var id = string.IsNullOrWhiteSpace(pedido.Identificador)
        ? "locacao-" + Guid.NewGuid().ToString("N")
        : pedido.Identificador.Trim();

    // lembrete: o início é SEMPRE o dia seguinte à criação (não uso o data_inicio que veio no JSON)
    // término e previsão = início + dias do plano, terminando às 23:59:59 do último dia
    var inicio = DateTime.UtcNow.Date.AddDays(1);
    var termino = inicio.AddDays(pedido.Plano).AddSeconds(-1);

    // lembrete: uma moto não pode estar alugada pra dois entregadores ao mesmo tempo
    // procura locação da MESMA moto que se choca com o período novo
    // o período antigo vai até a devolução (se já devolveu) ou até o término previsto
    await using (var buscaConflito = banco.CreateCommand(
        "SELECT 1 FROM locacoes WHERE moto_id = @moto_id " +
        "AND data_inicio < @fim AND COALESCE(data_devolucao, data_termino) > @inicio LIMIT 1"))
    {
        buscaConflito.Parameters.AddWithValue("moto_id", pedido.MotoId);
        buscaConflito.Parameters.AddWithValue("inicio", inicio);
        buscaConflito.Parameters.AddWithValue("fim", termino);

        if (await buscaConflito.ExecuteScalarAsync() is not null)
        {
            return Results.BadRequest(new { mensagem = "Dados inválidos" });
        }
    }

    try
    {
        await using var comando = banco.CreateCommand(
            "INSERT INTO locacoes (identificador, entregador_id, moto_id, plano, valor_diaria, data_inicio, data_termino, data_previsao_termino) " +
            "VALUES (@identificador, @entregador_id, @moto_id, @plano, @valor_diaria, @data_inicio, @data_termino, @data_previsao_termino)");

        comando.Parameters.AddWithValue("identificador", id);
        comando.Parameters.AddWithValue("entregador_id", pedido.EntregadorId);
        comando.Parameters.AddWithValue("moto_id", pedido.MotoId);
        comando.Parameters.AddWithValue("plano", pedido.Plano);
        comando.Parameters.AddWithValue("valor_diaria", valorDiaria);
        comando.Parameters.AddWithValue("data_inicio", inicio);
        comando.Parameters.AddWithValue("data_termino", termino);
        comando.Parameters.AddWithValue("data_previsao_termino", termino);

        await comando.ExecuteNonQueryAsync();
    }
    // identificador repetido (UniqueViolation) ou moto que não existe (ForeignKeyViolation): o banco recusa e eu devolvo 400
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                                   || ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    AvisoTerminal.Destaque($"Locação {id} criada. Diária: {Dinheiro.Reais(valorDiaria)}");

    // 201 sem corpo, mas mando o endereço da locação no cabeçalho (Location), já que o id pode ter sido gerado aqui
    return Results.Created($"/locacao/{id}", null);
});

// guichê de consultar uma locação pelo id
app.MapGet("/locacao/{id}", async (string id, NpgsqlDataSource banco) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    await using var comando = banco.CreateCommand(
        "SELECT identificador, valor_diaria, entregador_id, moto_id, data_inicio, data_termino, data_previsao_termino, data_devolucao, valor_total " +
        "FROM locacoes WHERE identificador = @id");
    comando.Parameters.AddWithValue("id", id);

    await using var leitor = await comando.ExecuteReaderAsync();

    if (!await leitor.ReadAsync())
    {
        return Results.NotFound(new { mensagem = "Locação não encontrada" });
    }

    var locacao = new LocacaoResposta
    {
        Identificador = leitor.GetString(0),
        ValorDiaria = leitor.GetDecimal(1),
        EntregadorId = leitor.GetString(2),
        MotoId = leitor.GetString(3),
        DataInicio = leitor.GetDateTime(4),
        DataTermino = leitor.GetDateTime(5),
        DataPrevisaoTermino = leitor.GetDateTime(6),
        // lembrete: a devolução começa vazia (null) até o entregador devolver a moto
        DataDevolucao = leitor.IsDBNull(7) ? null : leitor.GetDateTime(7),
        ValorTotal = leitor.IsDBNull(8) ? null : leitor.GetDecimal(8)
    };

    return Results.Ok(locacao);
});

//Parte 11 do projeto

// guichê de informar a devolução e calcular o valor total
app.MapPut("/locacao/{id}/devolucao", async (string id, DevolucaoLocacao pedido, NpgsqlDataSource banco) =>
{
    var erros = new List<ValidationResult>();
    var valido = Validator.TryValidateObject(pedido, new ValidationContext(pedido), erros, true);

    if (!valido || string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    // lê a data que veio (aceita o formato do swagger e dia/mês/ano), se não der já é 400
    if (!CalculoLocacao.TentarLerData(pedido.DataDevolucao, out var devolucao))
    {
        return Results.BadRequest(new { mensagem = "Dados inválidos" });
    }

    int plano;
    decimal valorDiaria;
    DateTime inicio, previsao;

    // busca da locação só o que o cálculo precisa
    await using (var busca = banco.CreateCommand(
        "SELECT plano, valor_diaria, data_inicio, data_previsao_termino FROM locacoes WHERE identificador = @id"))
    {
        busca.Parameters.AddWithValue("id", id);
        await using var leitor = await busca.ExecuteReaderAsync();

        // lembrete: o swagger só tem 200 e 400 nesse guichê, então locação que não existe vira 400
        if (!await leitor.ReadAsync())
        {
            return Results.BadRequest(new { mensagem = "Dados inválidos" });
        }

        plano = leitor.GetInt32(0);
        valorDiaria = leitor.GetDecimal(1);
        inicio = leitor.GetDateTime(2);
        previsao = leitor.GetDateTime(3);
    }

    var total = CalculoLocacao.CalcularTotal(plano, valorDiaria, inicio, previsao, devolucao);

    // lembrete: guardar a data_devolucao é o que libera a moto pra um novo aluguel (a regra do passo 12 olha ela)
    await using var atualiza = banco.CreateCommand(
        "UPDATE locacoes SET data_devolucao = @devolucao, valor_total = @total WHERE identificador = @id");
    atualiza.Parameters.AddWithValue("devolucao", devolucao);
    atualiza.Parameters.AddWithValue("total", total);
    atualiza.Parameters.AddWithValue("id", id);
    await atualiza.ExecuteNonQueryAsync();

    // lembrete: aviso em azul no terminal (o JSON continua número porque o swagger do teste exige número)
    AvisoTerminal.Destaque($"Locação {id} devolvida. Valor total: {Dinheiro.Reais(total)}");

    return Results.Ok(new { mensagem = "Data de devolução informada com sucesso" });
});
 
// Inicializador
app.Run();
 