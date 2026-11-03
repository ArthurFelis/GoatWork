import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast } from '../components/toast.js';
import { confirmarBriefing, obterBriefing } from '../api/briefings.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, dataBrParaIso, esc, lerMoeda, moeda } from '../utils/formatadores.js';
import { aplicarMascara, mascaraData, mascaraMoeda } from '../utils/validacoes.js';

exigirUsuario(['CLIENTE']);
carregarNavbar('mensagens');
carregarFooter();

const $ = (s) => document.querySelector(s);
const idPedido = Number(param('pedido'));
const resumo = $('#resumo');
const erro = $('#erro');
let requisitos = [];

aplicarMascara($('#prazo'), mascaraData);
aplicarMascara($('#orcamento'), mascaraMoeda);

function ajustarAltura() {
    resumo.style.height = 'auto';
    resumo.style.height = `${resumo.scrollHeight + 2}px`;
}
resumo.addEventListener('input', ajustarAltura);

function renderRequisitos() {
    $('#requisitos').innerHTML = requisitos.length
        ? requisitos.map((r) => `<li>${esc(r)}</li>`).join('')
        : '<li class="muted">Nenhum requisito informado.</li>';
}

$('#btn-editar-req').addEventListener('click', () => {
    const edicao = $('#requisitos-edicao');
    if (edicao.hidden) {
        edicao.value = requisitos.join('\n');
        edicao.hidden = false;
        $('#requisitos').hidden = true;
        $('#btn-editar-req').textContent = 'Concluir';
        edicao.focus();
    } else {
        requisitos = edicao.value.split('\n').map((l) => l.trim()).filter(Boolean);
        edicao.hidden = true;
        $('#requisitos').hidden = false;
        $('#btn-editar-req').textContent = 'Editar';
        renderRequisitos();
    }
});

function mostrarErro(msg) {
    erro.textContent = msg;
    erro.hidden = !msg;
}

async function carregar() {
    $('#btn-voltar').href = `chat-ia.html?pedido=${idPedido}`;
    try {
        const b = await obterBriefing(idPedido);
        requisitos = b.requisitos;
        $('#categoria').textContent = b.categoria;
        $('#categoria-side').textContent = b.categoria;
        resumo.value = b.resumo;
        $('#prazo').value = b.prazoDesejado ? data(b.prazoDesejado) : '';
        $('#orcamento').value = b.orcamentoEstimado != null ? moeda(b.orcamentoEstimado) : '';
        renderRequisitos();

        if (b.statusPedido !== 'EM_BRIEFING') modoLeitura();

        $('#carregando').hidden = true;
        $('#conteudo').hidden = false;
        ajustarAltura();
    } catch (e) {
        $('#carregando').outerHTML = `<div class="empty"><strong>Briefing indisponível</strong>${esc(e.message)}<br><br><a class="link" href="chat-ia.html?pedido=${idPedido}">Voltar à conversa com a IA</a></div>`;
    }
}

function modoLeitura() {
    $('#titulo').textContent = 'Briefing enviado';
    $('#subtitulo').textContent = 'Este pedido já foi enviado como convite aos profissionais.';
    [resumo, $('#prazo'), $('#orcamento')].forEach((el) => { el.readOnly = true; });
    $('#btn-editar-req').hidden = true;
    $('#caixa-confirmo').hidden = true;
    $('#acoes').innerHTML = '<a class="btn btn-primary" href="contratos.html">Ver pedidos e contratos</a>';
}

$('#form-briefing').addEventListener('submit', async (e) => {
    e.preventDefault();
    mostrarErro('');
    if (!$('#requisitos-edicao').hidden) $('#btn-editar-req').click();

    const prazoTxt = $('#prazo').value.trim();
    const prazo = prazoTxt ? dataBrParaIso(prazoTxt) : null;
    if (prazoTxt && !prazo) return mostrarErro('Prazo inválido. Use o formato dd/mm/aaaa.');
    if (!resumo.value.trim()) return mostrarErro('O resumo não pode ficar vazio.');
    if (!$('#confirmo').checked) return mostrarErro('Confirme que o briefing representa o que você precisa.');

    const botao = $('#btn-confirmar');
    botao.disabled = true;
    botao.classList.add('is-loading');
    try {
        await confirmarBriefing(idPedido, {
            resumo: resumo.value.trim(),
            requisitos,
            prazoDesejado: prazo,
            orcamentoEstimado: lerMoeda($('#orcamento').value),
            confirmo: true
        });
        toast('Pedido enviado! Avisaremos quando um profissional aceitar.');
        location.href = 'contratos.html';
    } catch (err) {
        mostrarErro(err.message);
        botao.disabled = false;
        botao.classList.remove('is-loading');
    }
});

carregar();
