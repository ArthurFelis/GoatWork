using GoatWork.Application.Common;
using GoatWork.Application.Interfaces;
using GoatWork.Application.Services;
using GoatWork.Domain.Enums;
using Xunit;

namespace GoatWork.Tests;

public class FiltroContatoExternoTests
{
    private readonly FiltroContatoExterno _filtro = new();

    [Theory]
    [InlineData("Pode chamar no meu whatsapp (11) 98765-4321")]
    [InlineData("me manda um e-mail: carlos@gmail.com")]
    [InlineData("11987654321")]
    [InlineData("acessa www.meusite.com.br")]
    [InlineData("me chama no zap")]
    public void Bloqueia_contatos_externos(string mensagem) => Assert.True(_filtro.ContemContatoExterno(mensagem));

    [Theory]
    [InlineData("Combinado então: R$ 650,00, sábado 10/10 às 9h.")]
    [InlineData("R$ 1.500,00 com materiais")]
    [InlineData("Preciso instalar 4 tomadas e trocar o quadro de energia")]
    [InlineData("Entrega em 10/10/2026")]
    public void Permite_mensagens_normais(string mensagem) => Assert.False(_filtro.ContemContatoExterno(mensagem));
}

public class DetectorAcordoTests
{
    [Fact]
    public void Identifica_valor_data_e_hora_do_acordo()
    {
        var msgs = new[]
        {
            "Olá, Gabriel! Li o seu pedido.",
            "R$ 650,00 com materiais e segurança adequados. Chego às 9h.",
            "Combinado então: R$ 650,00, sábado 10/10 às 9h."
        };
        var acordo = DetectorAcordo.Detectar(msgs, new DateOnly(2026, 9, 1));
        Assert.NotNull(acordo);
        Assert.Equal(650.00m, acordo!.Valor);
        Assert.Equal(new DateOnly(2026, 10, 10), acordo.Data);
        Assert.Equal("9h", acordo.Hora);
    }

    [Fact]
    public void Sem_confirmacao_nao_ha_acordo() =>
        Assert.Null(DetectorAcordo.Detectar(new[] { "Consigo por R$ 500,00 no dia 10/10" }, new DateOnly(2026, 9, 1)));
}

public class AtendimentoIaRegrasTests
{
    [Fact]
    public void Pergunta_local_e_prazo_e_depois_gera_briefing()
    {
        var hoje = new DateOnly(2026, 9, 1);
        const string descricao = "Preciso instalar 4 tomadas e trocar o quadro de energia da minha casa.";
        var historico = new List<MensagemContextoIa>
        {
            new(RemetenteIa.IA, AtendimentoIaRegras.Saudacao),
            new(RemetenteIa.CLIENTE, descricao)
        };

        var r1 = AtendimentoIaRegras.Responder(new ContextoIa("Casa e reparos", descricao, historico, hoje));
        Assert.Null(r1.Briefing);
        Assert.Equal(AtendimentoIaRegras.PerguntaLocalPrazo, r1.Mensagem);

        historico.Add(new(RemetenteIa.IA, r1.Mensagem));
        historico.Add(new(RemetenteIa.CLIENTE, "Rua das Flores, 120, Vila Mariana. Sábado, 10/10, se possível."));
        var r2 = AtendimentoIaRegras.Responder(new ContextoIa("Casa e reparos", descricao, historico, hoje));

        Assert.NotNull(r2.Briefing);
        Assert.Equal(new DateOnly(2026, 10, 10), r2.Briefing!.PrazoDesejado);
        Assert.Contains("Local: Rua das Flores, 120, Vila Mariana", r2.Briefing.Requisitos);
    }
}

public class ValidacaoTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("111.111.111-11", false)]
    [InlineData("123", false)]
    public void Cpf(string cpf, bool valido) => Assert.Equal(valido, Validacao.CpfValido(cpf));

    [Fact]
    public void Taxa_de_10_por_cento()
    {
        var (taxa, liquido) = Etapas.Valores(650m, 10m);
        Assert.Equal(65m, taxa);
        Assert.Equal(585m, liquido);
    }
}
