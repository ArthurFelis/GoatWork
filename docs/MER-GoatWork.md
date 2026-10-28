# MER – Plataforma de contratação de autônomos (GoatWork)

Notação: **PK** = chave primária, *FK* = chave estrangeira. Cardinalidades no formato (mín,máx).

## 1. Entidades e atributos

**USUARIO** (**id_usuario**, nome, email, senha_hash, cpf, telefone, tipo, status, data_cadastro)
Generalização total/disjunta: todo usuário é CLIENTE, TRABALHADOR ou ADMIN (campo `tipo`).

**CLIENTE** (**id_usuario**, tipo_pessoa, nome_exibicao) – *id_usuario* → USUARIO

**TRABALHADOR** (**id_usuario**, biografia, verificado, nota_media, data_verificacao) – *id_usuario* → USUARIO

**TERMO_USO** (**id_termo**, versao, texto, data_publicacao)

**ACEITE_TERMOS** (**id_aceite**, *id_usuario*, *id_termo*, data_hora, ip)
Prova de que cada usuário aceitou os termos (inclusive a isenção de responsabilidade) no cadastro/login.

**CATEGORIA** (**id_categoria**, nome)

**ATUACAO** (***id_trabalhador***, ***id_categoria***, anos_experiencia) – entidade associativa TRABALHADOR × CATEGORIA

**PEDIDO** (**id_pedido**, *id_cliente*, *id_categoria*, descricao_inicial, status, data_criacao)

**MENSAGEM_IA** (**id_msg_ia**, *id_pedido*, remetente, conteudo, data_hora)
Conversa entre o cliente e a IA de atendimento.

**BRIEFING** (**id_briefing**, *id_pedido*, resumo, requisitos, prazo_desejado, orcamento_estimado, confirmado_cliente, data_confirmacao)

**CONVITE** (**id_convite**, *id_pedido*, *id_trabalhador*, status, data_envio, data_resposta) – associativa PEDIDO × TRABALHADOR

**CHAT** (**id_chat**, *id_convite*, status, data_abertura)

**MENSAGEM** (**id_mensagem**, *id_chat*, *id_remetente*, conteudo, bloqueada, data_hora)

**CONTRATO** (**id_contrato**, *id_chat*, texto_contrato, valor_acordado, prazo_entrega, taxa_plataforma_perc, status, data_geracao, aceite_cliente_em, aceite_trabalhador_em)

**PAGAMENTO** (**id_pagamento**, *id_contrato*, metodo, valor_total, valor_taxa, valor_liquido, status, id_transacao_gateway, data_pagamento, data_liberacao)
Status: PENDENTE → EM_CUSTODIA → LIBERADO (ou ESTORNADO).

**ENTREGA** (**id_entrega**, *id_contrato*, descricao, data_entrega, status)

**DISPUTA** (**id_disputa**, *id_contrato*, *id_abertor*, *id_admin*, motivo, status, decisao, data_abertura, data_resolucao)

**AVALIACAO** (**id_avaliacao**, *id_contrato*, *id_avaliador*, *id_avaliado*, nota, comentario, data)

## 2. Relacionamentos

| # | Relacionamento | Entidades | Cardinalidade |
|---|---|---|---|
| 1 | aceita | USUARIO – ACEITE_TERMOS | 1:N — usuário (1,1) … aceite (0,N) |
| 2 | é aceito em | TERMO_USO – ACEITE_TERMOS | 1:N |
| 3 | é | USUARIO – CLIENTE / TRABALHADOR | 1:1 (especialização) |
| 4 | cria | CLIENTE – PEDIDO | 1:N — cliente (0,N) … pedido (1,1) |
| 5 | classifica | CATEGORIA – PEDIDO | 1:N |
| 6 | atua em | TRABALHADOR – CATEGORIA | N:N, resolvido por ATUACAO |
| 7 | tem | PEDIDO – MENSAGEM_IA | 1:N |
| 8 | gera | PEDIDO – BRIEFING | 1:1 (briefing só existe a partir de um pedido) |
| 9 | é enviado a / recebe | PEDIDO – TRABALHADOR | N:N, resolvido por CONVITE |
| 10 | libera | CONVITE – CHAT | 1:1 — chat só abre se o trabalhador aceitar conversar |
| 11 | contém | CHAT – MENSAGEM | 1:N |
| 12 | envia | USUARIO – MENSAGEM | 1:N |
| 13 | origina | CHAT – CONTRATO | 1:1 (contrato gerado a partir do briefing + acordos do chat) |
| 14 | é pago por | CONTRATO – PAGAMENTO | 1:1 |
| 15 | tem | CONTRATO – ENTREGA | 1:N (permite nova entrega após rejeição) |
| 16 | pode gerar | CONTRATO – DISPUTA | 1:N |
| 17 | abre / resolve | USUARIO – DISPUTA | 1:N em cada papel (abertor e admin) |
| 18 | recebe | CONTRATO – AVALIACAO | 1:N (máx. 2: cliente→trabalhador e trabalhador→cliente) |
| 19 | avalia / é avaliado em | USUARIO – AVALIACAO | 1:N em cada papel |

## 3. Regras de negócio refletidas no modelo

1. Todo usuário só é ativado após ter um registro em ACEITE_TERMOS (termos de não responsabilização da plataforma).
2. O cliente conversa primeiro com a IA (MENSAGEM_IA), que produz o BRIEFING; o pedido só é enviado depois de `confirmado_cliente = verdadeiro`.
3. O CHAT só existe se o CONVITE tiver status ACEITO; mensagens com contato externo são marcadas em `MENSAGEM.bloqueada`.
4. O CONTRATO só tem validade quando `aceite_cliente_em` e `aceite_trabalhador_em` estão preenchidos.
5. O pagamento fica EM_CUSTODIA e só é LIBERADO após a ENTREGA ser APROVADA ou a DISPUTA ser decidida por um administrador; `valor_liquido = valor_total − valor_taxa`.
6. Cada usuário só pode avaliar a outra parte de um contrato concluído (uma avaliação por avaliador por contrato).
