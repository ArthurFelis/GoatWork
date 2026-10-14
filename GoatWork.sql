CREATE DATABASE IF NOT EXISTS goatwork;
USE goatwork;

CREATE TABLE termo_uso (
    ter_id_termo INT AUTO_INCREMENT PRIMARY KEY,
    ter_nu_versao VARCHAR(20) NOT NULL UNIQUE,
    ter_tx_texto LONGTEXT NOT NULL,
    ter_dt_publicacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
 
CREATE TABLE usuario (
    usu_id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    usu_nm_usuario VARCHAR(120) NOT NULL,
    usu_tx_email VARCHAR(150) NOT NULL UNIQUE,
    usu_tx_senha_hash VARCHAR(255) NOT NULL,
    usu_nu_cpf CHAR(11) NOT NULL UNIQUE,
    usu_nu_telefone VARCHAR(20) NULL,
    usu_ds_tipo ENUM('CLIENTE','TRABALHADOR','ADMIN') NOT NULL,
    usu_st_usuario VARCHAR(20) NOT NULL DEFAULT 'ATIVO',
    usu_dt_cadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
 
CREATE TABLE aceite_termos (
    ace_id_aceite INT AUTO_INCREMENT PRIMARY KEY,
    ace_id_usuario INT NOT NULL,
    ace_id_termo INT NOT NULL,
    ace_dt_hora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ace_nu_ip VARCHAR(45) NULL,
 
    UNIQUE (ace_id_usuario, ace_id_termo),
 
    FOREIGN KEY (ace_id_usuario) REFERENCES usuario(usu_id_usuario),
    FOREIGN KEY (ace_id_termo) REFERENCES termo_uso(ter_id_termo)
);

CREATE TABLE aceite_termos (
	ace_id_aceite INT AUTO_INCREMENT PRIMARY KEY,
    ace_id_usuario INT NOT NULL,
    ace_id_termo INT NOT NULL,
    ace_dt_hora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ace_nu_ip VARCHAR(45) NULL,
    
    UNIQUE (ace_id_usuario, ace_id_termo),
    
    FOREIGN KEY (ace_id_usuario) REFERENCES usuario(usu_id_usuario),
    FOREIGN KEY (ace_id_termo) REFERENCES termo_uso(ter_id_termo)
);

CREATE TABLE trabalhador (
    tra_id_usuario INT PRIMARY KEY,
    tra_tx_biografia TEXT NULL,
    tra_fl_verificado CHAR(1) NOT NULL DEFAULT 'N',
    tra_nu_nota_media DECIMAL(3,2) NULL,
    tra_dt_verificacao DATETIME NULL,
 
    CHECK (tra_fl_verificado IN ('S','N')),
    CHECK (tra_nu_nota_media IS NULL OR tra_nu_nota_media BETWEEN 0 AND 5),
 
    FOREIGN KEY (tra_id_usuario) REFERENCES usuario(usu_id_usuario)
);

CREATE TABLE cliente (
    cli_id_usuario INT PRIMARY KEY,
    cli_ds_tipo_pessoa ENUM('FISICA','JURIDICA') NOT NULL DEFAULT 'FISICA',
    cli_nm_exibicao VARCHAR(120) NULL,
 
    FOREIGN KEY (cli_id_usuario) REFERENCES usuario(usu_id_usuario)
);
 
CREATE TABLE categoria (
    cat_id_categoria INT AUTO_INCREMENT PRIMARY KEY,
    cat_nm_categoria VARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE atuacao ( 
	atu_id_trabalhador INT NOT NULL, 
    atu_id_categoria INT NOT NULL, 
    atu_nu_anos_experiencia INT NOT NULL DEFAULT 0,
    
    PRIMARY KEY (atu_id_trabalhador, atu_id_categoria),
    
    FOREIGN KEY (atu_id_trabalhador) REFERENCES trabalhador(tra_id_usuario),
    FOREIGN KEY (atu_id_categoria) REFERENCES categoria(cat_id_categoria)
);

CREATE TABLE mensagem_ia (
    mia_id_msg_ia INT AUTO_INCREMENT PRIMARY KEY,
    mia_id_pedido INT NOT NULL,
    mia_ds_remetente ENUM('CLIENTE','IA') NOT NULL,
    mia_tx_conteudo TEXT NOT NULL,
    mia_dt_hora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 
    FOREIGN KEY (mia_id_pedido) REFERENCES pedido(ped_id_pedido)
);

CREATE TABLE briefing (
    bri_id_briefing INT AUTO_INCREMENT PRIMARY KEY,
    bri_id_pedido INT NOT NULL UNIQUE,
    bri_tx_resumo TEXT NOT NULL,
    bri_tx_requisitos TEXT NULL,
    bri_dt_prazo_desejado DATE NULL,
    bri_vl_orcamento_estimado DECIMAL(10,2) NULL,
    bri_fl_confirmado_cliente TINYINT(1) NOT NULL DEFAULT 0,
    bri_dt_confirmacao DATETIME NULL,
 
    CHECK (bri_vl_orcamento_estimado IS NULL OR bri_vl_orcamento_estimado >= 0),
 
    FOREIGN KEY (bri_id_pedido) REFERENCES pedido(ped_id_pedido)
);
 
CREATE TABLE convite (
    con_id_convite INT AUTO_INCREMENT PRIMARY KEY,
    con_id_pedido INT NOT NULL,
    con_id_trabalhador INT NOT NULL,
    con_st_convite ENUM('ENVIADO','ACEITO','RECUSADO') NOT NULL DEFAULT 'ENVIADO',
    con_dt_envio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    con_dt_resposta DATETIME NULL,
 
    UNIQUE (con_id_pedido, con_id_trabalhador),
 
    FOREIGN KEY (con_id_pedido) REFERENCES pedido(ped_id_pedido),
    FOREIGN KEY (con_id_trabalhador) REFERENCES trabalhador(tra_id_usuario)
);

CREATE TABLE briefing (
    bri_id_briefing INT AUTO_INCREMENT PRIMARY KEY,
    bri_id_pedido INT NOT NULL UNIQUE,
    bri_tx_resumo TEXT NOT NULL,
    bri_tx_requisitos TEXT NULL,
    bri_dt_prazo_desejado DATE NULL,
    bri_vl_orcamento_estimado DECIMAL(10,2) NULL,
    bri_fl_confirmado_cliente TINYINT(1) NOT NULL DEFAULT 0,
    bri_dt_confirmacao DATETIME NULL,
 
    CHECK (bri_vl_orcamento_estimado IS NULL OR bri_vl_orcamento_estimado >= 0),
 
    FOREIGN KEY (bri_id_pedido) REFERENCES pedido(ped_id_pedido)
);
 
CREATE TABLE convite (
    con_id_convite INT AUTO_INCREMENT PRIMARY KEY,
    con_id_pedido INT NOT NULL,
    con_id_trabalhador INT NOT NULL,
    con_st_convite ENUM('ENVIADO','ACEITO','RECUSADO') NOT NULL DEFAULT 'ENVIADO',
    con_dt_envio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    con_dt_resposta DATETIME NULL,
 
    UNIQUE (con_id_pedido, con_id_trabalhador),
 
    FOREIGN KEY (con_id_pedido) REFERENCES pedido(ped_id_pedido),
    FOREIGN KEY (con_id_trabalhador) REFERENCES trabalhador(tra_id_usuario)
);

CREATE TABLE chat (
    cht_id_chat INT AUTO_INCREMENT PRIMARY KEY,
    cht_id_convite INT NOT NULL UNIQUE,
    cht_st_chat VARCHAR(20) NOT NULL DEFAULT 'ABERTO',
    cht_dt_abertura DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 
    FOREIGN KEY (cht_id_convite) REFERENCES convite(con_id_convite)
);
 
CREATE TABLE mensagem (
    men_id_mensagem INT AUTO_INCREMENT PRIMARY KEY,
    men_id_chat INT NOT NULL,
    men_id_remetente INT NOT NULL,
    men_tx_conteudo TEXT NOT NULL,
    men_fl_bloqueada CHAR(1) NOT NULL DEFAULT 'N',
    men_dt_hora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 
    CHECK (men_fl_bloqueada IN ('S','N')),
 
    FOREIGN KEY (men_id_chat) REFERENCES chat(cht_id_chat),
    FOREIGN KEY (men_id_remetente) REFERENCES usuario(usu_id_usuario)
);
 
CREATE TABLE contrato (
    ctr_id_contrato INT AUTO_INCREMENT PRIMARY KEY,
    ctr_id_chat INT NOT NULL UNIQUE,
    ctr_tx_contrato LONGTEXT NOT NULL,
    ctr_vl_acordado DECIMAL(10,2) NOT NULL,
    ctr_dt_prazo_entrega DATE NOT NULL,
    ctr_nu_taxa_plataforma DECIMAL(5,2) NOT NULL,
    ctr_st_contrato VARCHAR(30) NOT NULL DEFAULT 'AGUARDANDO_ACEITE',
    ctr_dt_geracao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ctr_dt_aceite_cliente DATETIME NULL,
    ctr_dt_aceite_trabalhador DATETIME NULL,
 
    CHECK (ctr_vl_acordado > 0),
    CHECK (ctr_nu_taxa_plataforma BETWEEN 0 AND 100),
 
    FOREIGN KEY (ctr_id_chat) REFERENCES chat(cht_id_chat)
);
 
CREATE TABLE pagamento (
    pag_id_pagamento INT AUTO_INCREMENT PRIMARY KEY,
    pag_id_contrato INT NOT NULL UNIQUE,
    pag_ds_metodo ENUM('PIX','CARTAO') NOT NULL,
    pag_vl_total DECIMAL(10,2) NOT NULL,
    pag_vl_taxa DECIMAL(10,2) NOT NULL,
    pag_vl_liquido DECIMAL(10,2) NOT NULL,
    pag_st_pagamento ENUM('PENDENTE','EM_CUSTODIA','LIBERADO','ESTORNADO') NOT NULL DEFAULT 'PENDENTE',
    pag_id_transacao_gateway VARCHAR(100) NULL UNIQUE,
    pag_dt_pagamento DATETIME NULL,
    pag_dt_liberacao DATETIME NULL,
 
    CHECK (pag_vl_total >= 0 AND pag_vl_taxa >= 0),
    CHECK (pag_vl_liquido = pag_vl_total - pag_vl_taxa),
 
    FOREIGN KEY (pag_id_contrato) REFERENCES contrato(ctr_id_contrato)
);

CREATE TABLE entrega (
    ent_id_entrega INT AUTO_INCREMENT PRIMARY KEY,
    ent_id_contrato INT NOT NULL,
    ent_tx_descricao TEXT NOT NULL,
    ent_dt_entrega DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ent_st_entrega ENUM('PENDENTE','APROVADA','REJEITADA') NOT NULL DEFAULT 'PENDENTE',
 
    FOREIGN KEY (ent_id_contrato) REFERENCES contrato(ctr_id_contrato)
);
 
CREATE TABLE disputa (
    dis_id_disputa INT AUTO_INCREMENT PRIMARY KEY,
    dis_id_contrato INT NOT NULL,
    dis_id_abertor INT NOT NULL,
    dis_id_admin INT NULL,
    dis_tx_motivo TEXT NOT NULL,
    dis_st_disputa VARCHAR(20) NOT NULL DEFAULT 'ABERTA',
    dis_tx_decisao TEXT NULL,
    dis_dt_abertura DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dis_dt_resolucao DATETIME NULL,
 
    FOREIGN KEY (dis_id_contrato) REFERENCES contrato(ctr_id_contrato),
    FOREIGN KEY (dis_id_abertor) REFERENCES usuario(usu_id_usuario),
    FOREIGN KEY (dis_id_admin) REFERENCES usuario(usu_id_usuario)
);
 
CREATE TABLE avaliacao (
    avl_id_avaliacao INT AUTO_INCREMENT PRIMARY KEY,
    avl_id_contrato INT NOT NULL,
    avl_id_avaliador INT NOT NULL,
    avl_id_avaliado INT NOT NULL,
    avl_nu_nota INT NOT NULL,
    avl_tx_comentario TEXT NULL,
    avl_dt_avaliacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 
    UNIQUE (avl_id_contrato, avl_id_avaliador),
 
    CHECK (avl_nu_nota >= 1 AND avl_nu_nota <= 5),
    CHECK (avl_id_avaliador <> avl_id_avaliado),
 
    FOREIGN KEY (avl_id_contrato) REFERENCES contrato(ctr_id_contrato),
    FOREIGN KEY (avl_id_avaliador) REFERENCES usuario(usu_id_usuario),
    FOREIGN KEY (avl_id_avaliado) REFERENCES usuario(usu_id_usuario)
);