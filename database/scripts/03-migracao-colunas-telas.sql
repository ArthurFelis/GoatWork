-- =====================================================================
-- Só para bancos criados com a versão ANTERIOR do GoatWork.sql.
-- Adiciona as colunas usadas pelas telas (perfil do profissional e "Contratar").
-- =====================================================================
USE goatwork;

ALTER TABLE trabalhador
  ADD COLUMN profissao           VARCHAR(80)   NULL AFTER data_verificacao,
  ADD COLUMN cidade              VARCHAR(100)  NULL AFTER profissao,
  ADD COLUMN valor_referencia    DECIMAL(12,2) NULL AFTER cidade,
  ADD COLUMN unidade_valor       VARCHAR(20)   NULL AFTER valor_referencia,
  ADD COLUMN servicos_concluidos INT UNSIGNED  NOT NULL DEFAULT 0 AFTER unidade_valor,
  ADD COLUMN total_avaliacoes    INT UNSIGNED  NOT NULL DEFAULT 0 AFTER servicos_concluidos;

ALTER TABLE pedido
  ADD COLUMN id_trabalhador_preferido INT UNSIGNED NULL AFTER data_criacao,
  ADD KEY ix_pedido_trab_preferido (id_trabalhador_preferido),
  ADD CONSTRAINT fk_pedido_trab_preferido FOREIGN KEY (id_trabalhador_preferido)
    REFERENCES trabalhador (id_usuario) ON UPDATE CASCADE ON DELETE SET NULL;
