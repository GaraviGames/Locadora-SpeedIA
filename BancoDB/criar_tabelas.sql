CREATE TABLE motos (
    identificador VARCHAR(50) PRIMARY KEY,
    ano INTEGER NOT NULL,
    modelo VARCHAR(15) NOT NULL,
    placa VARCHAR(8) NOT NULL UNIQUE
);

CREATE TABLE entregadores (
    identificador VARCHAR(50) PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    cnpj VARCHAR(18) NOT NULL UNIQUE,
    data_nascimento DATE NOT NULL,
    numero_cnh VARCHAR(11) NOT NULL UNIQUE,
    tipo_cnh VARCHAR(3) NOT NULL CHECK (tipo_cnh IN ('A', 'B', 'A+B')),
    caminho_imagem_cnh TEXT
);

CREATE TABLE locacoes (
    identificador VARCHAR(50) PRIMARY KEY,
    entregador_id VARCHAR(50) NOT NULL REFERENCES entregadores(identificador),
    moto_id VARCHAR(50) NOT NULL REFERENCES motos(identificador),
    plano INTEGER NOT NULL,
    valor_diaria NUMERIC(10,2) NOT NULL,
    data_inicio TIMESTAMPTZ NOT NULL,
    data_termino TIMESTAMPTZ NOT NULL,
    data_previsao_termino TIMESTAMPTZ NOT NULL,
    data_devolucao TIMESTAMPTZ,
    valor_total NUMERIC(10,2)
);

CREATE TABLE notificacoes_motos (
    id SERIAL PRIMARY KEY,
    identificador VARCHAR(50) NOT NULL,
    ano INTEGER NOT NULL,
    modelo VARCHAR(15) NOT NULL,
    placa VARCHAR(8) NOT NULL,
    recebida_em TIMESTAMPTZ NOT NULL DEFAULT now()
);