-- =====================================================================
-- Só para bancos criados ANTES da tela "Minha carteira".
-- Cria a tabela de saques do trabalhador.
-- =====================================================================
USE goatwork;

-- ---------------------------------------------------------------------
-- Carteira do trabalhador: saques via Pix do saldo liberado
-- ---------------------------------------------------------------------
CREATE TABLE saque (
  id_saque              INT UNSIGNED  NOT NULL AUTO_INCREMENT,
  id_trabalhador        INT UNSIGNED  NOT NULL,
  valor                 DECIMAL(12,2) NOT NULL,
  tipo_chave            ENUM('CPF','CNPJ','EMAIL','TELEFONE','ALEATORIA') NOT NULL,
  chave_pix             VARCHAR(140)  NOT NULL,
  status                ENUM('SOLICITADO','PAGO','FALHOU') NOT NULL DEFAULT 'SOLICITADO',
  id_transacao_gateway  VARCHAR(100)  NULL,
  data_solicitacao      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_conclusao        DATETIME      NULL,
  PRIMARY KEY (id_saque),
  KEY ix_saque_trabalhador (id_trabalhador),
  CONSTRAINT fk_saque_trabalhador FOREIGN KEY (id_trabalhador) REFERENCES trabalhador (id_usuario)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT ck_saque_valor CHECK (valor > 0)
) ENGINE=InnoDB;
