import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { erro as toastErro } from '../components/toast.js';
import { carregarConversas } from '../components/conversas.js';
import { enviarMensagem, obterChat } from '../api/chats.js';
import { gerarContrato } from '../api/contratos.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, moeda } from '../utils/formatadores.js';

const usuario = exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('mensagens');
carregarFooter();

const $ = (s) => document.querySelector(s);
const corpo = $('#mensagens');
const texto = $('#texto');
const botao = $('#btn-enviar');
let idChat = Number(param('chat')) || null;
let chat = null;
let assinatura = '';

const ICONE_ALERTA = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" aria-hidden="true"><circle cx="12" cy="12" r="10"/><path d="M12 7v6M12 17h.01"/></svg>';

const STATUS_CONTRATO = {
    GERADO: 'Aguardando assinaturas',
    ACEITO_CLIENTE: 'Aguardando assinatura do trabalhador',
    ACEITO_TRABALHADOR: 'Aguardando assinatura do cliente',
    ATIVO: 'Contrato ativo',
    CONCLUIDO: 'Concluído',
    CANCELADO: 'Cancelado',
    EM_DISPUTA: 'Em disputa'
};

function render() {
    $('#chat-titulo').textContent = chat.titulo;
    $('#chat-subtitulo').textContent = chat.subtitulo;
    $('#chat-lock').hidden = false;
    document.title = `${chat.titulo} — Mensagens — GoatWork`;

    const html = chat.mensagens.map((m) => {
        if (m.bloqueada) {
            return `<div class="msg msg--bloqueada">
                        <span class="msg-texto">${esc(m.conteudo)}</span>
                        <span class="msg-aviso">${ICONE_ALERTA} Mensagem bloqueada: não é permitido compartilhar contatos externos.</span>
                    </div>`;
        }
        return m.minha
            ? `<div class="msg msg--minha">${esc(m.conteudo)}</div>`
            : `<div class="msg msg--outro"><span class="msg-autor">${esc(m.remetente)}</span>${esc(m.conteudo)}</div>`;
    });

    if (!chat.mensagens.length) {
        html.push(`<p class="msg-sistema">Chat liberado. ${usuario.tipo === 'TRABALHADOR' ? 'Apresente-se e combine valor e data com o cliente.' : 'Combine valor e data com o profissional.'} Contatos externos são bloqueados.</p>`);
    }

    if (chat.idContrato) {
        html.push(`
            <div class="chat-card">
                <h3>Contrato ${esc(chat.numeroContrato)}</h3>
                <p>${esc(STATUS_CONTRATO[chat.statusContrato] ?? chat.statusContrato)}</p>
                <a class="btn btn-primary btn-block" href="contrato.html?id=${chat.idContrato}">Ver contrato</a>
            </div>`);
    } else if (chat.acordo) {
        html.push(`
            <div class="chat-card">
                <h3>Acordo identificado</h3>
                <dl>
                    <div><dt>Valor</dt><dd>${moeda(chat.acordo.valor)}</dd></div>
                    <div><dt>Data de entrega</dt><dd>${data(chat.acordo.data)}</dd></div>
                </dl>
                <button class="btn btn-primary btn-block" type="button" id="btn-gerar">Gerar contrato</button>
            </div>`);
    }

    const estavaNoFim = corpo.scrollHeight - corpo.scrollTop - corpo.clientHeight < 60;
    corpo.innerHTML = html.join('');
    if (estavaNoFim || !assinatura) corpo.scrollTop = corpo.scrollHeight;

    const aberto = chat.status === 'ABERTO';
    texto.disabled = !aberto;
    botao.disabled = !aberto;
    texto.placeholder = aberto ? 'Escreva sua mensagem...' : 'Esta conversa foi encerrada.';

    $('#btn-gerar')?.addEventListener('click', async (e) => {
        const b = e.currentTarget;
        b.disabled = true;
        b.classList.add('is-loading');
        try {
            const contrato = await gerarContrato(chat.idChat, chat.acordo.valor, chat.acordo.data);
            location.href = `contrato.html?id=${contrato.id}`;
        } catch (err) {
            toastErro(err);
            b.disabled = false;
            b.classList.remove('is-loading');
        }
    });

    assinatura = `${chat.mensagens.length}|${chat.idContrato}|${chat.statusContrato}|${chat.status}`;
}

async function abrir() {
    try {
        chat = await obterChat(idChat);
        render();
    } catch (e) {
        corpo.innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

// Atualiza a conversa a cada 5 s para mostrar as mensagens da outra parte
async function atualizar() {
    if (!idChat || document.hidden || document.activeElement === texto && texto.value) return;
    try {
        const novo = await obterChat(idChat);
        const sig = `${novo.mensagens.length}|${novo.idContrato}|${novo.statusContrato}|${novo.status}`;
        if (sig !== assinatura) {
            chat = novo;
            render();
        }
    } catch { /* tenta de novo no próximo ciclo */ }
}

$('#form-msg').addEventListener('submit', async (e) => {
    e.preventDefault();
    const conteudo = texto.value.trim();
    if (!conteudo || !chat) return;
    botao.disabled = true;
    try {
        chat = await enviarMensagem(idChat, conteudo);
        texto.value = '';
        assinatura = '';
        render();
    } catch (err) {
        toastErro(err);
    }
    botao.disabled = chat?.status !== 'ABERTO';
    texto.focus();
});

async function iniciar() {
    try {
        const { chats } = await carregarConversas($('#conversas'), { usuario, ativo: idChat });
        if (!idChat) {
            if (chats.length) {
                idChat = chats[0].idChat;
                history.replaceState(null, '', `?chat=${idChat}`);
                carregarConversas($('#conversas'), { usuario, ativo: idChat }).catch(() => {});
            } else {
                $('#chat-titulo').textContent = 'Mensagens';
                $('#chat-subtitulo').textContent = usuario.tipo === 'CLIENTE' ? 'Nenhuma conversa com profissionais ainda' : 'Nenhuma conversa ainda';
                corpo.innerHTML = usuario.tipo === 'CLIENTE'
                    ? '<div class="empty"><strong>O chat abre quando um profissional aceita seu pedido</strong>Comece um pedido com a IA e acompanhe em Contratos.<br><br><a class="btn btn-primary" href="novo-pedido.html">Começar com a IA</a></div>'
                    : '<div class="empty"><strong>Nenhuma conversa ainda</strong>Aceite um convite para abrir o chat com o cliente.<br><br><a class="btn btn-primary" href="convites.html">Ver convites</a></div>';
                return;
            }
        }
    } catch (e) {
        toastErro(e);
    }
    if (idChat) {
        await abrir();
        setInterval(atualizar, 5000);
    }
}

iniciar();
