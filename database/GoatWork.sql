-- =====================================================================
-- GoatWork - Modelo físico (MySQL 8.0+)
-- =====================================================================

DROP DATABASE IF EXISTS goatwork;
CREATE DATABASE goatwork
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE goatwork;

-- =====================================================================
-- FASE 1 · Cadastro e termos de uso
-- =====================================================================

CREATE TABLE termo_uso (
  id_termo         INT UNSIGNED NOT NULL AUTO_INCREMENT,
  versao           VARCHAR(20)  NOT NULL,
  texto            LONGTEXT     NOT NULL,
  data_publicacao  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_termo),
  UNIQUE KEY uq_termo_versao (versao)
) ENGINE=InnoDB;

CREATE TABLE usuario (
  id_usuario    INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome          VARCHAR(150) NOT NULL,
  email         VARCHAR(190) NOT NULL,
  senha_hash    VARCHAR(255) NOT NULL,
  cpf           CHAR(11)     NOT NULL,
  telefone      VARCHAR(20)  NULL,
  tipo          ENUM('CLIENTE','TRABALHADOR','ADMIN') NOT NULL,
  status        ENUM('ATIVO','INATIVO','SUSPENSO','BLOQUEADO') NOT NULL DEFAULT 'ATIVO',
  data_cadastro DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_usuario),
  UNIQUE KEY uq_usuario_email (email),
  UNIQUE KEY uq_usuario_cpf (cpf)
) ENGINE=InnoDB;

CREATE TABLE aceite_termos (
  id_aceite  INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_usuario INT UNSIGNED NOT NULL,
  id_termo   INT UNSIGNED NOT NULL,
  data_hora  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ip         VARCHAR(45)  NOT NULL,  
  PRIMARY KEY (id_aceite),
  UNIQUE KEY uq_aceite_usuario_termo (id_usuario, id_termo),
  CONSTRAINT fk_aceite_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_aceite_termo FOREIGN KEY (id_termo) REFERENCES termo_uso (id_termo)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE trabalhador (
  id_usuario        INT UNSIGNED NOT NULL,
  biografia         TEXT         NULL,
  verificado        BOOLEAN      NOT NULL DEFAULT FALSE,
  nota_media        DECIMAL(3,2) NOT NULL DEFAULT 0.00,
  data_verificacao  DATETIME     NULL,
  PRIMARY KEY (id_usuario),
  CONSTRAINT ck_trabalhador_nota CHECK (nota_media BETWEEN 0 AND 5),
  CONSTRAINT fk_trabalhador_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE cliente (
  id_usuario     INT UNSIGNED NOT NULL,
  tipo_pessoa    ENUM('FISICA','JURIDICA') NOT NULL DEFAULT 'FISICA',
  nome_exibicao  VARCHAR(100) NULL,
  PRIMARY KEY (id_usuario),
  CONSTRAINT fk_cliente_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE categoria (
  id_categoria INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nome         VARCHAR(100) NOT NULL,
  PRIMARY KEY (id_categoria),
  UNIQUE KEY uq_categoria_nome (nome)
) ENGINE=InnoDB;

CREATE TABLE atuacao (
  id_trabalhador   INT UNSIGNED NOT NULL,
  id_categoria     INT UNSIGNED NOT NULL,
  anos_experiencia TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (id_trabalhador, id_categoria),
  CONSTRAINT fk_atuacao_trabalhador FOREIGN KEY (id_trabalhador) REFERENCES trabalhador (id_usuario)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_atuacao_categoria FOREIGN KEY (id_categoria) REFERENCES categoria (id_categoria)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- =====================================================================
-- FASE 2 · Pedido e briefing com IA
-- =====================================================================

CREATE TABLE pedido (
  id_pedido         INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_cliente        INT UNSIGNED NOT NULL,
  id_categoria      INT UNSIGNED NOT NULL,
  descricao_inicial TEXT         NOT NULL,
  status            ENUM('RASCUNHO','EM_BRIEFING','PUBLICADO','EM_ANDAMENTO','CONCLUIDO','CANCELADO')
                    NOT NULL DEFAULT 'RASCUNHO',
  data_criacao      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_pedido),
  KEY ix_pedido_cliente (id_cliente),
  KEY ix_pedido_categoria (id_categoria),
  CONSTRAINT fk_pedido_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_pedido_categoria FOREIGN KEY (id_categoria) REFERENCES categoria (id_categoria)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE mensagem_ia (
  id_msg_ia  INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_pedido  INT UNSIGNED NOT NULL,
  remetente  ENUM('CLIENTE','IA') NOT NULL,
  conteudo   TEXT     NOT NULL,
  data_hora  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_msg_ia),
  KEY ix_msgia_pedido (id_pedido, data_hora),
  CONSTRAINT fk_msgia_pedido FOREIGN KEY (id_pedido) REFERENCES pedido (id_pedido)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE briefing (
  id_briefing        INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_pedido          INT UNSIGNED NOT NULL,
  resumo             TEXT          NOT NULL,
  requisitos         TEXT          NULL,
  prazo_desejado     DATE          NULL,
  orcamento_estimado DECIMAL(12,2) NULL,
  confirmado_cliente BOOLEAN       NOT NULL DEFAULT FALSE,
  data_confirmacao   DATETIME      NULL,
  PRIMARY KEY (id_briefing),
  UNIQUE KEY uq_briefing_pedido (id_pedido),
  CONSTRAINT ck_briefing_orcamento CHECK (orcamento_estimado IS NULL OR orcamento_estimado >= 0),
  CONSTRAINT fk_briefing_pedido FOREIGN KEY (id_pedido) REFERENCES pedido (id_pedido)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- =====================================================================
-- FASE 3 · Convite e chat interno
-- =====================================================================

CREATE TABLE convite (
  id_convite     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_pedido      INT UNSIGNED NOT NULL,
  id_trabalhador INT UNSIGNED NOT NULL,
  status         ENUM('ENVIADO','ACEITO','RECUSADO') NOT NULL DEFAULT 'ENVIADO',
  data_envio     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_resposta  DATETIME NULL,
  PRIMARY KEY (id_convite),
  UNIQUE KEY uq_convite_pedido_trab (id_pedido, id_trabalhador),
  KEY ix_convite_trabalhador (id_trabalhador),
  CONSTRAINT fk_convite_pedido FOREIGN KEY (id_pedido) REFERENCES pedido (id_pedido)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_convite_trabalhador FOREIGN KEY (id_trabalhador) REFERENCES trabalhador (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE chat (
  id_chat       INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_convite    INT UNSIGNED NOT NULL,
  status        ENUM('ABERTO','FECHADO','BLOQUEADO') NOT NULL DEFAULT 'ABERTO',
  data_abertura DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_chat),
  UNIQUE KEY uq_chat_convite (id_convite),
  CONSTRAINT fk_chat_convite FOREIGN KEY (id_convite) REFERENCES convite (id_convite)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE mensagem (
  id_mensagem  INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_chat      INT UNSIGNED NOT NULL,
  id_remetente INT UNSIGNED NOT NULL,
  conteudo     TEXT     NOT NULL,
  bloqueada    BOOLEAN  NOT NULL DEFAULT FALSE,
  data_hora    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_mensagem),
  KEY ix_mensagem_chat (id_chat, data_hora),
  KEY ix_mensagem_remetente (id_remetente),
  CONSTRAINT fk_mensagem_chat FOREIGN KEY (id_chat) REFERENCES chat (id_chat)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_mensagem_remetente FOREIGN KEY (id_remetente) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- =====================================================================
-- FASE 4 · Contrato, pagamento e entrega
-- =====================================================================

CREATE TABLE contrato (
  id_contrato          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_chat              INT UNSIGNED NOT NULL,
  texto_contrato       LONGTEXT      NOT NULL,
  valor_acordado       DECIMAL(12,2) NOT NULL,
  prazo_entrega        DATE          NOT NULL,
  taxa_plataforma_perc DECIMAL(5,2)  NOT NULL,
  status               ENUM('GERADO','ACEITO_CLIENTE','ACEITO_TRABALHADOR','ATIVO',
                            'CONCLUIDO','CANCELADO','EM_DISPUTA')
                       NOT NULL DEFAULT 'GERADO',
  data_geracao         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  aceite_cliente_em    DATETIME NULL,
  aceite_trabalhador_em DATETIME NULL,
  PRIMARY KEY (id_contrato),
  UNIQUE KEY uq_contrato_chat (id_chat),
  CONSTRAINT ck_contrato_valor CHECK (valor_acordado > 0),
  CONSTRAINT ck_contrato_taxa  CHECK (taxa_plataforma_perc BETWEEN 0 AND 100),
  CONSTRAINT fk_contrato_chat FOREIGN KEY (id_chat) REFERENCES chat (id_chat)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE pagamento (
  id_pagamento         INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_contrato          INT UNSIGNED NOT NULL,
  metodo               ENUM('PIX','CARTAO') NOT NULL,
  valor_total          DECIMAL(12,2) NOT NULL,
  valor_taxa           DECIMAL(12,2) NOT NULL,
  valor_liquido        DECIMAL(12,2) NOT NULL,
  status               ENUM('PENDENTE','EM_CUSTODIA','LIBERADO','REEMBOLSADO','FALHOU')
                       NOT NULL DEFAULT 'PENDENTE',
  id_transacao_gateway VARCHAR(100) NULL,
  data_pagamento       DATETIME NULL,
  data_liberacao       DATETIME NULL,
  PRIMARY KEY (id_pagamento),
  UNIQUE KEY uq_pagamento_contrato (id_contrato),
  UNIQUE KEY uq_pagamento_gateway (id_transacao_gateway),
  CONSTRAINT ck_pagamento_valores CHECK (
    valor_total > 0 AND valor_taxa >= 0 AND valor_liquido >= 0
    AND valor_liquido = valor_total - valor_taxa),
  CONSTRAINT fk_pagamento_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE entrega (
  id_entrega   INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_contrato  INT UNSIGNED NOT NULL,
  descricao    TEXT     NOT NULL,
  data_entrega DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  status       ENUM('PENDENTE','APROVADA','REJEITADA') NOT NULL DEFAULT 'PENDENTE',
  PRIMARY KEY (id_entrega),
  KEY ix_entrega_contrato (id_contrato),
  CONSTRAINT fk_entrega_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- =====================================================================
-- FASE 5 · Disputa e avaliação
-- =====================================================================

CREATE TABLE disputa (
  id_disputa      INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_contrato     INT UNSIGNED NOT NULL,
  id_abertor      INT UNSIGNED NOT NULL,
  id_admin        INT UNSIGNED NULL,       
  motivo          TEXT     NOT NULL,
  status          ENUM('ABERTA','EM_ANALISE','RESOLVIDA','ARQUIVADA') NOT NULL DEFAULT 'ABERTA',
  decisao         TEXT     NULL,
  data_abertura   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_resolucao  DATETIME NULL,
  PRIMARY KEY (id_disputa),
  KEY ix_disputa_contrato (id_contrato),
  KEY ix_disputa_abertor (id_abertor),
  KEY ix_disputa_admin (id_admin),
  CONSTRAINT fk_disputa_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_disputa_abertor FOREIGN KEY (id_abertor) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_disputa_admin FOREIGN KEY (id_admin) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE avaliacao (
  id_avaliacao  INT UNSIGNED NOT NULL AUTO_INCREMENT,
  id_contrato   INT UNSIGNED NOT NULL,
  id_avaliador  INT UNSIGNED NOT NULL,
  id_avaliado   INT UNSIGNED NOT NULL,
  nota          TINYINT UNSIGNED NOT NULL,
  comentario    TEXT     NULL,
  data          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_avaliacao),
  UNIQUE KEY uq_avaliacao_contrato_avaliador (id_contrato, id_avaliador),
  KEY ix_avaliacao_avaliado (id_avaliado),
  CONSTRAINT ck_avaliacao_nota CHECK (nota BETWEEN 1 AND 5),
  CONSTRAINT fk_avaliacao_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_avaliacao_avaliador FOREIGN KEY (id_avaliador) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_avaliacao_avaliado FOREIGN KEY (id_avaliado) REFERENCES usuario (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

DELIMITER $$
CREATE TRIGGER trg_avaliacao_bi BEFORE INSERT ON avaliacao
FOR EACH ROW
BEGIN
  IF NEW.id_avaliador = NEW.id_avaliado THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Usuario nao pode avaliar a si mesmo';
  END IF;
END$$

CREATE TRIGGER trg_avaliacao_bu BEFORE UPDATE ON avaliacao
FOR EACH ROW
BEGIN
  IF NEW.id_avaliador = NEW.id_avaliado THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Usuario nao pode avaliar a si mesmo';
  END IF;
END$$
DELIMITER ;