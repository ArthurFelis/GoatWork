namespace GoatWork.Application.DTOs;

// ---------------------------------------------------------------------
// Fase 1 · Cadastro, login, termos, perfis
// ---------------------------------------------------------------------

public record CadastroRequest(
    string Tipo,
    string Nome,
    string Email,
    string Senha,
    string Cpf,
    string? Telefone,
    string? TipoPessoa,
    string? NomeExibicao,
    string? Profissao,
    string? Cidade,
    bool AceitouTermos);

public record LoginRequest(string Tipo, string Email, string Senha, bool AceitouTermos);

public record UsuarioResumoDto(int Id, string Nome, string Email, string Tipo, string Iniciais);

public record AuthResponse(string Token, UsuarioResumoDto Usuario);

public record AtualizarUsuarioRequest(string Nome, string? Telefone);

public record ItemTermoDto(string Texto, bool Destaque);

public record TermoDto(int Id, string Versao, DateTime DataPublicacao, List<ItemTermoDto> Itens);

public record PublicarTermoRequest(string Versao, string Texto);

public record CategoriaDto(int Id, string Nome);

public record TrabalhadorCardDto(
    int Id,
    string Nome,
    string Iniciais,
    string? Profissao,
    string? Cidade,
    string? Biografia,
    decimal Nota,
    int Servicos,
    decimal? Valor,
    string? Unidade,
    bool Verificado,
    List<int> IdsCategorias,
    List<string> Categorias);

public record AtuacaoDto(int IdCategoria, string Categoria, int AnosExperiencia);

public record PerfilTrabalhadorDto(
    int Id,
    string Nome,
    string Iniciais,
    string? Profissao,
    string? Cidade,
    string? Biografia,
    decimal Nota,
    int Servicos,
    decimal? ValorReferencia,
    string? UnidadeValor,
    bool Verificado,
    DateTime? DataVerificacao,
    List<AtuacaoDto> Atuacoes);

public record AtuacaoRequest(int IdCategoria, int AnosExperiencia);

public record AtualizarPerfilRequest(
    string? Biografia,
    string? Profissao,
    string? Cidade,
    decimal? ValorReferencia,
    string? UnidadeValor,
    List<AtuacaoRequest> Atuacoes);

// ---------------------------------------------------------------------
// Fase 2 · Pedido e briefing com a IA
// ---------------------------------------------------------------------

public record CriarPedidoRequest(int IdCategoria, string Descricao, int? IdTrabalhador, bool Rascunho);

public record AtualizarPedidoRequest(int IdCategoria, string Descricao, bool Rascunho);

public record PedidoDto(
    int Id,
    string Numero,
    string Titulo,
    string Descricao,
    int IdCategoria,
    string Categoria,
    string Status,
    DateTime DataCriacao,
    int? IdTrabalhadorPreferido,
    string? TrabalhadorPreferido,
    int? IdChat,
    string? TrabalhadorAceito,
    int? IdContrato,
    bool TemBriefing);

public record MensagemIaDto(int Id, string Remetente, string Conteudo, DateTime DataHora);

public record EnviarMensagemRequest(string Conteudo);

public record BriefingDto(
    int Id,
    int IdPedido,
    string Categoria,
    string Resumo,
    List<string> Requisitos,
    DateOnly? PrazoDesejado,
    decimal? OrcamentoEstimado,
    bool Confirmado,
    string StatusPedido);

public record ConversaIaDto(PedidoDto Pedido, List<MensagemIaDto> Mensagens, BriefingDto? Briefing);

public record ConfirmarBriefingRequest(
    string Resumo,
    List<string> Requisitos,
    DateOnly? PrazoDesejado,
    decimal? OrcamentoEstimado,
    bool Confirmo);

// ---------------------------------------------------------------------
// Fase 3 · Convites e chat interno
// ---------------------------------------------------------------------

public record ConviteDto(
    int Id,
    string Status,
    int IdPedido,
    string Titulo,
    string Resumo,
    string Categoria,
    string Cliente,
    string IniciaisCliente,
    DateTime DataEnvio,
    string? Local,
    DateOnly? Prazo,
    decimal? Orcamento,
    int? IdChat);

public record ConversaResumoDto(int IdChat, string Titulo, string Subtitulo, DateTime UltimaAtividade);

public record MensagemDto(int Id, string Remetente, bool Minha, string Conteudo, bool Bloqueada, DateTime DataHora);

public record AcordoDto(decimal Valor, DateOnly Data, string? Hora);

public record ChatDto(
    int IdChat,
    int IdPedido,
    string NumeroPedido,
    string Titulo,
    string Subtitulo,
    string Status,
    List<MensagemDto> Mensagens,
    AcordoDto? Acordo,
    int? IdContrato,
    string? NumeroContrato,
    string? StatusContrato);

// ---------------------------------------------------------------------
// Fase 4 · Contrato, pagamento e entrega
// ---------------------------------------------------------------------

public record GerarContratoRequest(int IdChat, decimal Valor, DateOnly Prazo);

public record ClausulaDto(string Titulo, string Texto);

public record ContratoDto(
    int Id,
    string Numero,
    string Status,
    int IdChat,
    int IdPedido,
    string Resumo,
    string Cliente,
    string Trabalhador,
    string? ProfissaoTrabalhador,
    List<ClausulaDto> Clausulas,
    decimal Valor,
    DateOnly Prazo,
    decimal TaxaPerc,
    DateTime? AceiteClienteEm,
    DateTime? AceiteTrabalhadorEm,
    string MeuPapel,
    string? StatusPagamento,
    string Etapa);

public record ContratoListaDto(
    int Id,
    string Numero,
    string Servico,
    string OutraParte,
    decimal Valor,
    string Status,
    string Etapa,
    bool AssinadoPorMim,
    bool AvaliadoPorMim,
    int? IdDisputa);

public record PagamentoResumoDto(
    int IdContrato,
    string NumeroContrato,
    string StatusContrato,
    decimal ValorTotal,
    decimal TaxaPerc,
    decimal ValorTaxa,
    decimal ValorLiquido,
    string Status,
    string? Metodo,
    string PixCopiaECola,
    string Servico,
    string Trabalhador,
    DateOnly Prazo,
    DateTime? DataPagamento);

public record PagarRequest(string Metodo, string? NumeroCartao, string? NomeCartao, string? Validade, string? Cvv);

public record EntregaDto(int Id, int Numero, string Descricao, DateTime DataEntrega, string Status);

public record EntregasContratoDto(
    int IdContrato,
    string NumeroContrato,
    string StatusContrato,
    string MeuPapel,
    string Cliente,
    string Trabalhador,
    string IniciaisTrabalhador,
    string Servico,
    List<string> Requisitos,
    DateOnly Prazo,
    decimal Valor,
    decimal ValorTaxa,
    decimal ValorLiquido,
    string StatusPagamento,
    List<EntregaDto> Entregas);

public record EnviarEntregaRequest(string Descricao);

// ---------------------------------------------------------------------
// Fase 5 · Disputa, avaliação e administração
// ---------------------------------------------------------------------

public record DisputaDto(
    int Id,
    string Numero,
    string Motivo,
    string Status,
    string? Decisao,
    DateTime DataAbertura,
    DateTime? DataResolucao,
    string AbertaPor,
    string PapelAbertor);

public record DisputaContextoDto(
    int IdContrato,
    string NumeroContrato,
    string Trabalhador,
    string Cliente,
    string MeuPapel,
    decimal ValorCustodia,
    string SituacaoContrato,
    string StatusPagamento,
    DisputaDto? Disputa);

public record AbrirDisputaRequest(string Motivo, bool Entendo);

public record AvaliacaoContextoDto(
    int IdContrato,
    string NumeroContrato,
    string StatusContrato,
    string MeuPapel,
    string Avaliado,
    string IniciaisAvaliado,
    string SubtituloAvaliado,
    string Resumo,
    decimal Valor,
    string StatusPagamento,
    DateTime? ConcluidoEm,
    bool JaAvaliado,
    int? MinhaNota);

public record AvaliarRequest(int Nota, string? Comentario);

public record AdminResumoDto(int Abertas, int EmAnalise, int Resolvidas);

public record DisputaAdminItemDto(
    int Id,
    string Numero,
    int IdContrato,
    string NumeroContrato,
    string AbertaPor,
    string PapelAbertor,
    string Motivo,
    string Status);

public record MensagemAdminDto(string Remetente, bool DoCliente, string Conteudo, bool Bloqueada, DateTime DataHora);

public record DisputaAdminDetalheDto(
    int Id,
    string Numero,
    int IdContrato,
    string NumeroContrato,
    string Motivo,
    string Status,
    string? Decisao,
    string Cliente,
    string Trabalhador,
    decimal Valor,
    string StatusPagamento,
    List<MensagemAdminDto> Chat,
    List<ClausulaDto> Clausulas,
    List<EntregaDto> Entregas);

public record DecidirDisputaRequest(string Decisao, string Justificativa);

public record UsuarioAdminDto(
    int Id,
    string Nome,
    string Email,
    string Tipo,
    string Status,
    bool? Verificado,
    DateTime DataCadastro);

public record AlterarStatusUsuarioRequest(string Status);

public record ContratoAdminDto(
    int Id,
    string Numero,
    string Cliente,
    string Trabalhador,
    decimal Valor,
    string Status,
    string? StatusPagamento);
