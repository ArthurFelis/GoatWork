-- =====================================================================
-- Só para bancos criados ANTES do cadastro com CNPJ (pessoa jurídica).
-- A coluna cpf passa a guardar CPF (11 dígitos) ou CNPJ (14 dígitos).
-- =====================================================================
USE goatwork;

ALTER TABLE usuario MODIFY cpf VARCHAR(14) NOT NULL;

-- Trabalhador também pode ser pessoa jurídica (MEI/empresa)
ALTER TABLE trabalhador
  ADD COLUMN tipo_pessoa ENUM('FISICA','JURIDICA') NOT NULL DEFAULT 'FISICA' AFTER biografia;
