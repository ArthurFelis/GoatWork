-- =====================================================================
-- Só para bancos criados ANTES da versão com "Enviar orçamento" no chat.
-- Guarda o horário e o local combinados no contrato.
-- =====================================================================
USE goatwork;

ALTER TABLE contrato
  ADD COLUMN hora_servico  VARCHAR(10)  NULL AFTER prazo_entrega,
  ADD COLUMN local_servico VARCHAR(255) NULL AFTER hora_servico;
