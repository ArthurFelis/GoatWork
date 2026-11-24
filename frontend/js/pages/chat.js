import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import { carregarConversas } from '../components/conversas.js';
import { enviarMensagem, obterChat } from '../api/chats.js';
import { enviarOrcamento, recusarContrato } from '../api/contratos.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, isoData, lerData, lerMoeda, moeda } from '../utils/formatadores.js';
import { aplicarMascara, mascaraMoeda } from '../utils/validacoes.js';

const usuario = exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('mensagens');
carregarFooter();

const $ = (s) => document.querySelector(s);
const corpo = $('#mensagens');
const texto = $('#texto');
const botao = $('#btn-enviar');
const formOrcamento = $('#form-orcamento');
let idChat = Number(param('chat')) || null;
let chat = null;
let assinatura = '';

aplicarMascara($('#orc-valor'), mascaraMoeda);

const ICONE_ALERTA = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" aria-hidden="true"><circle cx="12" cy="12" r="10"/><path d="M12 7v6M12 17h.01"/></svg>';
const ICONE_PIN = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v5M9 3h6l-1 6 4 4H6l4-4z"/></svg>';
const CHECK = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>';

const STATUS_CONTRATO = {
    ATIVO: 'Contrato ativo',
    CONCLUIDO: 'Concluído',
    CANCELADO: 'Cancelado',
    EM_DISPUTA: 'Em disputa'
};

function horaParaInput(h) {
    const m = /^(\d{1,2})h(\d{2})?$/.exec(h || '');
    return m ? `${m[1].padStart(2, '0')}:${m[2] ?? '00'}` : '';
}

// ------------------------------------------------------------ Mensagem fixada
function renderFixado() {
    const f = chat.fixado;
    const p = chat.proposta;
    const item = (rotulo, valor) => `<div><dt>${rotulo}</dt><dd>${valor}</dd></div>`;
    const itens = p
        ? [
            item('Valor', moeda(p.valor)),
            item('Data', data(p.data)),
            item('Horário', esc(p.hora ?? 'A combinar')),
            item('Local', esc(p.local ?? f.local ?? 'A combinar'))
        ]
        : [
            item('Local', esc(f.local ?? 'A combinar')),
            item('Data desejada', f.prazoDesejado ? data(f.prazoDesejado) : 'A combinar'),
            item('Horário', 'A combinar'),
            item('Orçamento estimado', f.orcamentoEstimado != null ? moeda(f.orcamentoEstimado) : 'A combinar')
        ];

    let etiqueta = 'Combinado no pedido';
    if (p && p.assinadoCliente) etiqueta = 'Combinado no contrato';
    else if (p) etiqueta = 'Orçamento enviado — aguardando o cliente';

    $('#fixado').innerHTML = `
        <div class="fixado-topo">
            <span class="fixado-pin">${ICONE_PIN} Mensagem fixada</span>
            <span class="fixado-etiqueta">${etiqueta}</span>
        </div>
        <p class="fixado-servico"><strong>Serviço:</strong> ${esc(f.servico)}</p>
        <dl class="fixado-dados">${itens.join('')}</dl>`;
    $('#fixado').hidden = false;
}

// ------------------------------------------------------------ Card do orçamento
function cardProposta() {
    const p = chat.proposta;
    if (!p) return '';
    if (p.assinadoCliente) {
        return `
            <div class="chat-card">
                <h3>Contrato ${esc(p.numero)}</h3>
                <p>${esc(STATUS_CONTRATO[p.status] ?? 'Assinado pelas duas partes')}</p>
                <a class="btn btn-primary btn-block" href="contrato.html?id=${p.idContrato}">Ver contrato</a>
            </div>`;
    }
    const dados = `
        <dl>
            <div><dt>Valor</dt><dd>${moeda(p.valor)}</dd></div>
            <div><dt>Data</dt><dd>${data(p.data)}${p.hora ? ` às ${esc(p.hora)}` : ''}</dd></div>
        </dl>
        ${p.local ? `<p class="card-local">Local: ${esc(p.local)}</p>` : ''}
        <p class="card-assinado">${CHECK} Assinado pelo profissional</p>`;

    if (chat.meuPapel === 'CLIENTE') {
        return `
            <div class="chat-card chat-card--orcamento">
                <h3>Orçamento recebido</h3>
                ${dados}
                <div class="chat-card-acoes">
                    <button class="btn btn-outline" type="button" id="btn-recusar">Recusar</button>
                    <a class="btn btn-primary" href="contrato.html?id=${p.idContrato}">Ver contrato e assinar</a>
                </div>
            </div>`;
    }
    return `
        <div class="chat-card chat-card--orcamento">
            <h3>Orçamento enviado</h3>
            ${dados}
            <p class="card-aguardando">Aguardando a assinatura do cliente.</p>
            <a class="btn btn-secondary btn-block" href="contrato.html?id=${p.idContrato}">Ver contrato</a>
        </div>`;
}

// ------------------------------------------------------------ Barra de ações rápidas
function renderAcoes() {
    const acoes = $('#acoes-chat');
    const aberto = chat.status === 'ABERTO';
    const pendente = chat.proposta && !chat.proposta.assinadoCliente;
    acoes.innerHTML = '';
    if (!aberto || (chat.proposta && chat.proposta.assinadoCliente) || !formOrcamento.hidden) {
        acoes.hidden = true;
        return;
    }
    if (chat.meuPapel === 'TRABALHADOR') {
        acoes.innerHTML = `
            <span>${pendente ? 'Quer ajustar valor, data ou horário?' : 'Combinou com o cliente? Envie o orçamento — o contrato sai pronto, já com a sua assinatura.'}</span>
            <button class="btn btn-primary btn-sm" type="button" id="btn-abrir-orcamento">${pendente ? 'Alterar orçamento' : 'Enviar orçamento'}</button>`;
        $('#btn-abrir-orcamento').addEventListener('click', abrirFormOrcamento);
    } else if (!pendente) {
        acoes.innerHTML = `
            <span>O contrato é gerado quando o profissional envia o orçamento.</span>
            <button class="btn btn-secondary btn-sm" type="button" id="btn-pedir">Pedir orçamento</button>`;
        $('#btn-pedir').addEventListener('click', (e) => comCarregamento(e.currentTarget, () =>
            enviar('Olá! Pode me enviar o orçamento pela GoatWork com valor, data e horário?')));
    } else {
        acoes.hidden = true;
        return;
    }
    acoes.hidden = false;
}

function abrirFormOrcamento() {
    const s = chat.sugestao ?? {};
    $('#orc-valor').value = s.valor ? moeda(s.valor) : '';
    $('#orc-data').value = s.data ?? '';
    $('#orc-data').min = isoData(new Date());
    $('#orc-hora').value = horaParaInput(s.hora);
    $('#orc-local').value = s.local ?? chat.fixado.local ?? '';
    $('#btn-enviar-orcamento').textContent = chat.proposta ? 'Enviar novo orçamento' : 'Enviar orçamento e assinar';
    formOrcamento.hidden = false;
    renderAcoes();
    $('#orc-valor').focus();
}

function fecharFormOrcamento() {
    formOrcamento.hidden = true;
    renderAcoes();
}

$('#btn-fechar-orcamento').addEventListener('click', fecharFormOrcamento);
$('#btn-cancelar-orcamento').addEventListener('click', fecharFormOrcamento);

formOrcamento.addEventListener('submit', async (e) => {
    e.preventDefault();
    const valor = lerMoeda($('#orc-valor').value);
    const prazo = $('#orc-data').value;
    const hora = $('#orc-hora').value;
    const local = $('#orc-local').value.trim();
    if (!valor || valor <= 0) return toastErro(new Error('Informe o valor total do orçamento.'));
    if (!prazo) return toastErro(new Error('Informe a data do serviço.'));
    const hoje = new Date();
    hoje.setHours(0, 0, 0, 0);
    if (lerData(prazo) < hoje) return toastErro(new Error('A data do serviço não pode estar no passado.'));

    await comCarregamento($('#btn-enviar-orcamento'), async () => {
        try {
            await enviarOrcamento(idChat, { valor, prazo, hora: hora || null, local: local || null });
            toast('Orçamento enviado! O contrato já está assinado por você.');
            formOrcamento.hidden = true;
            await abrir();
        } catch (err) {
            toastErro(err);
        }
    });
});

// ------------------------------------------------------------ Conversa
function render() {
    $('#chat-titulo').textContent = chat.titulo;
    $('#chat-subtitulo').textContent = chat.subtitulo;
    $('#chat-lock').hidden = false;
    document.title = `${chat.titulo} — Mensagens — GoatWork`;
    renderFixado();

    const html = chat.mensagens.map((m) => {
        if (m.bloqueada) {
            return `<div class="msg msg--bloqueada"><span class="msg-texto">${esc(m.conteudo)}</span><span class="msg-aviso">${ICONE_ALERTA} Mensagem bloqueada: não é permitido compartilhar contatos externos.</span></div>`;
        }
        return m.minha
            ? `<div class="msg msg--minha">${esc(m.conteudo)}</div>`
            : `<div class="msg msg--outro"><span class="msg-autor">${esc(m.remetente)}</span>${esc(m.conteudo)}</div>`;
    });

    if (!chat.mensagens.length) {
        html.push(`<p class="msg-sistema">Chat liberado. ${chat.meuPapel === 'TRABALHADOR'
            ? 'Apresente-se, tire as dúvidas e use "Enviar orçamento" quando combinarem.'
            : 'Converse com o profissional; ele envia o orçamento por aqui.'} Contatos externos são bloqueados.</p>`);
    }
    if (chat.pedidoFechadoComOutro) {
        html.push('<p class="msg-sistema">Este pedido foi fechado com outro profissional. A conversa foi encerrada.</p>');
    }
    html.push(cardProposta());

    const estavaNoFim = corpo.scrollHeight - corpo.scrollTop - corpo.clientHeight < 60;
    corpo.innerHTML = html.join('');
    if (estavaNoFim || !assinatura) {
        corpo.scrollTop = corpo.scrollHeight;
        requestAnimationFrame(() => { corpo.scrollTop = corpo.scrollHeight; });
    }

    const aberto = chat.status === 'ABERTO';
    texto.disabled = !aberto;
    botao.disabled = !aberto;
    texto.placeholder = aberto ? 'Escreva sua mensagem...' : 'Esta conversa foi encerrada.';
    if (!aberto) formOrcamento.hidden = true;
    renderAcoes();

    $('#btn-recusar')?.addEventListener('click', (e) => comCarregamento(e.currentTarget, async () => {
        try {
            await recusarContrato(chat.proposta.idContrato);
            toast('Orçamento recusado. O profissional pode enviar um novo.');
            await abrir();
        } catch (err) {
            toastErro(err);
        }
    }));

    assinatura = assinar(chat);
}

const assinar = (c) => `${c.mensagens.length}|${c.proposta?.idContrato}|${c.proposta?.valor}|${c.proposta?.assinadoCliente}|${c.proposta?.status}|${c.status}`;

async function abrir() {
    try {
        chat = await obterChat(idChat);
        assinatura = '';
        render();
    } catch (e) {
        corpo.innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

// Atualiza a conversa a cada 5 s para mostrar as mensagens da outra parte
async function atualizar() {
    if (!idChat || document.hidden || (document.activeElement === texto && texto.value) || !formOrcamento.hidden) return;
    try {
        const novo = await obterChat(idChat);
        if (assinar(novo) !== assinatura) {
            chat = novo;
            render();
        }
    } catch { /* tenta de novo no próximo ciclo */ }
}

async function enviar(conteudo) {
    try {
        chat = await enviarMensagem(idChat, conteudo);
        assinatura = '';
        render();
        return true;
    } catch (err) {
        toastErro(err);
        return false;
    }
}

$('#form-msg').addEventListener('submit', async (e) => {
    e.preventDefault();
    const conteudo = texto.value.trim();
    if (!conteudo || !chat) return;
    botao.disabled = true;
    if (await enviar(conteudo)) texto.value = '';
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
