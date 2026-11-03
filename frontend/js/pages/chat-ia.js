import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { erro as toastErro } from '../components/toast.js';
import { carregarConversas } from '../components/conversas.js';
import { conversaIa, enviarMensagemIa } from '../api/briefings.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { esc } from '../utils/formatadores.js';

const usuario = exigirUsuario(['CLIENTE']);
carregarNavbar('mensagens');
carregarFooter();

const $ = (s) => document.querySelector(s);
const corpo = $('#mensagens');
const form = $('#form-msg');
const texto = $('#texto');
const botao = $('#btn-enviar');
const idPedido = Number(param('pedido'));
let conversa = null;

function render(digitando = false, pendente = null) {
    const msgs = conversa.mensagens.map((m) => m.remetente === 'IA'
        ? `<div class="msg msg--outro"><span class="msg-autor">IA</span>${esc(m.conteudo)}</div>`
        : `<div class="msg msg--minha">${esc(m.conteudo)}</div>`);

    if (pendente) msgs.push(`<div class="msg msg--minha">${esc(pendente)}</div>`);
    if (digitando) msgs.push('<div class="digitando" aria-label="IA digitando"><span></span><span></span><span></span></div>');

    const status = conversa.pedido.status;
    if (conversa.briefing && status === 'EM_BRIEFING' && !digitando) {
        msgs.push(`
            <div class="chat-card">
                <h3>Briefing pronto</h3>
                <p>${esc(conversa.briefing.resumo)}</p>
                <a class="btn btn-primary btn-block" href="revisao-briefing.html?pedido=${idPedido}">Revisar briefing</a>
            </div>`);
    } else if (status !== 'EM_BRIEFING') {
        msgs.push(`
            <div class="chat-card">
                <h3>Pedido enviado aos profissionais</h3>
                <p>${esc(conversa.briefing?.resumo ?? conversa.pedido.titulo)}</p>
                <a class="btn btn-primary btn-block" href="contratos.html">Acompanhar pedido</a>
            </div>`);
    }

    corpo.innerHTML = msgs.join('');
    corpo.scrollTop = corpo.scrollHeight;

    const fechado = status !== 'EM_BRIEFING';
    texto.disabled = fechado;
    botao.disabled = fechado;
    texto.placeholder = fechado ? 'Este pedido já foi enviado.' : 'Escreva sua mensagem...';
}

async function carregar() {
    if (!idPedido) {
        location.replace('novo-pedido.html');
        return;
    }
    try {
        conversa = await conversaIa(idPedido);
        if (conversa.pedido.status === 'RASCUNHO') {
            location.replace(`novo-pedido.html?pedido=${idPedido}`);
            return;
        }
        render();
        texto.focus();
    } catch (e) {
        corpo.innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
    carregarConversas($('#conversas'), { usuario, ativo: 'ia', idPedidoIa: idPedido }).catch(() => {});
}

form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const conteudo = texto.value.trim();
    if (!conteudo || !conversa) return;
    texto.value = '';
    botao.disabled = true;
    render(true, conteudo);
    try {
        conversa = await enviarMensagemIa(idPedido, conteudo);
    } catch (err) {
        texto.value = conteudo;
        toastErro(err);
    }
    render();
    botao.disabled = false;
    texto.focus();
});

carregar();
