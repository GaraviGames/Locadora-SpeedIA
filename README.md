# Locadora SpeedIA

API em C# (.NET) para gerenciar aluguel de motos e entregadores.

## Tecnologias

- .NET / C# (Minimal API)
- PostgreSQL (acesso com Npgsql, sem PL/pgSQL)
- RabbitMQ (aviso de moto cadastrada)
- Swagger (página de testes)

## Como rodar

1. Instale o SDK do .NET compatível com o `TargetFramework` do arquivo `LocadoraSpeedIA/LocadoraSpeedIA.csproj`.
2. Crie um banco chamado `LocadoraSpeedIA` no PostgreSQL e rode o script `BancoDB/criar_tabelas.sql`.
3. Suba o RabbitMQ:
   ```
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```
4. Em `LocadoraSpeedIA/appsettings.json`, troque `SUA_SENHA` pela senha do seu PostgreSQL.
5. Rode:
   ```
   cd LocadoraSpeedIA
   dotnet run
   ```
6. Abra o endereço mostrado no terminal, seguido de `/swagger`.

## Endpoints

- Motos: `POST /motos`, `GET /motos`, `GET /motos/{id}`, `PUT /motos/{id}/placa`, `DELETE /motos/{id}`
- Entregadores: `POST /entregadores`, `POST /entregadores/{id}/cnh`
- Locações: `POST /locacao`, `GET /locacao/{id}`, `PUT /locacao/{id}/devolucao`

## Regras principais

- A placa é única e aceita os formatos `ABC-1234` e `ABC-1D23`.
- Só motos de 1990 até o ano atual podem ser cadastradas.
- Ao cadastrar uma moto, um evento é publicado no RabbitMQ. Um consumidor guarda no banco a notificação das motos de 2024.
- Só entregador com CNH A (ou A+B) pode alugar. Uma moto não pode ter duas locações ao mesmo tempo.
- Planos: 7, 15, 30, 45 e 50 dias. Na devolução antecipada, os planos de 7 e 15 dias têm multa de 20% e 40% sobre as diárias não usadas. Cada dia depois da previsão custa R$ 50,00 a mais.
- As fotos de CNH (png ou bmp) ficam em disco, na pasta `LocadoraSpeedIA/ArquivosCnh`, e só o caminho vai para o banco.