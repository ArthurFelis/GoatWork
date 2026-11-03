-- =====================================================================
-- GoatWork - dados iniciais (rodar depois de GoatWork.sql)
-- Usuários de exemplo (admin, cliente e profissionais) são criados pela API
-- na primeira execução (DbSeeder), porque as senhas precisam de hash BCrypt.
-- =====================================================================
USE goatwork;

INSERT IGNORE INTO categoria (id_categoria, nome) VALUES
  (1, 'Casa e reparos'),
  (2, 'Limpeza'),
  (3, 'Digital'),
  (4, 'Eventos'),
  (5, 'Aulas'),
  (6, 'Beleza');

-- Um item por linha; a linha iniciada com "!" é exibida em destaque (isenção de responsabilidade)
INSERT IGNORE INTO termo_uso (versao, texto) VALUES ('1.0', CONCAT_WS('\n',
  'A GoatWork é uma plataforma de intermediação entre clientes e trabalhadores autônomos, não sendo empregadora nem prestadora dos serviços contratados.',
  '!A GoatWork não se responsabiliza por danos materiais, físicos ou morais causados durante a execução do trabalho, sendo a responsabilidade exclusiva das partes envolvidas.',
  'Cliente e trabalhador comprometem-se a agir de boa-fé e a cumprir o contrato digital gerado na plataforma.',
  'Todo pagamento deve ser feito pela própria plataforma. Pagamentos por fora não terão qualquer proteção.',
  'As conversas no chat ficam registradas e podem ser usadas para mediação em caso de disputa.'));
