import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import {
    alterarStatusUsuario, analisarDisputa, decidirDisputa, detalheDisputa, listarContratosAdmin,
    listarDisputasAdmin, listarUsuariosAdmin, resumoAdmin, verificarTrabalhador
} from '../api/admin.js';
import { listarTermos, publicarTermo } from '../api/termos.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, moeda } from '../utils/formatadores.js';

exigirUsuario(['ADMIN']);
const aba = ['contratos', 'usuarios', 'termos'].includes(param('aba')) ? param('aba') : 'disputas';
carregarNavbar(aba === 'disputas' ? 'disputas' : aba === 'usuarios' ? 'usuarios' : `admin-${aba}`);
carregarFooter();

const $ = (s) => document.querySelector(s);
const SITUACAO = { ABERTA: ['Aberta', 'badge--peach'], EM_ANALISE: ['Em análise', 'badge--blue'], RESOLVIDA: ['Resolvida', 'badge--sand'], ARQUIVADA: ['Arquivada', 'badge--sand'] };
const PAPEL = { CLIENTE: 'Cliente', TRABALHADOR: 'Trabalhador', ADMIN: 'Administrador' };
const vazio = (n, txt) => `<tr><td colspan="${n}"><div class="empty" style="border:0">${txt}</div></td></tr>`;

// ================================================================= Disputas
let filtro = '';
let selecionada = Number(param('disputa')) || null;
let detalhe = null;
let painel = 'chat';

async function carregarResumo() {
    const r = await resumoAdmin();
    $('#n-abertas').textContent = r.abertas;
    $('#n-analise').textContent = r.emAnalise;
    $('#n-resolvidas').textContent = r.resolvidas;
}

function botaoLinha(d) {
    if (d.id === selecionada && d.status !== 'RESOLVIDA') return `<button class="btn btn-primary" type="button" data-abrir="${d.id}">Analisando</button>`;
    if (d.status === 'RESOLVIDA' || d.status === 'ARQUIVADA') return `<button class="btn btn-outline" type="button" data-abrir="${d.id}">Ver</button>`;
    return `<button class="btn btn-outline" type="button" data-analisar="${d.id}">Analisar</button>`;
}

async function carregarDisputas() {
    document.querySelectorAll('#filtros .chip').forEach((c) => c.setAttribute('aria-pressed', String(c.dataset.status === filtro)));
    try {
        const lista = await listarDisputasAdmin(filtro);
        $('#disputas').innerHTML = lista.length ? lista.map((d) => {
            const [rotulo, classe] = SITUACAO[d.status];
            return `
                <tr class="${d.id === selecionada ? 'is-selecionada' : ''}">
                    <td><strong>${esc(d.numero)}</strong></td>
                    <td>${esc(d.numeroContrato)}</td>
                    <td>${esc(d.abertaPor)}<small>${PAPEL[d.papelAbertor] ?? ''}</small></td>
                    <td>${esc(d.motivo.length > 60 ? `${d.motivo.slice(0, 60)}…` : d.motivo)}</td>
                    <td><span class="badge ${classe}">${rotulo}</span></td>
                    <td>${botaoLinha(d)}</td>
                </tr>`;
        }).join('') : vazio(6, 'Nenhuma disputa por aqui.');
    } catch (e) {
        $('#disputas').innerHTML = vazio(6, esc(e.message));
    }
}

function conteudoPainel(d) {
    if (painel === 'contrato') {
        return d.clausulas.map((c) => `<section class="ct-clausula"><h3>${esc(c.titulo)}</h3><p>${esc(c.texto)}</p></section>`).join('');
    }
    if (painel === 'entregas') {
        return d.entregas.length
            ? d.entregas.map((e) => `<section class="ct-clausula"><h3>Entrega ${e.numero} · ${e.status === 'APROVADA' ? 'Aprovada' : e.status === 'REJEITADA' ? 'Rejeitada' : 'Pendente'}</h3><p>${esc(e.descricao)}</p></section>`).join('')
            : '<p class="muted" style="font-size:13px">Nenhuma entrega registrada.</p>';
    }
    if (!d.chat.length) return '<p class="muted" style="font-size:13px">Sem mensagens.</p>';
    return `<div class="adm-chat">${d.chat.map((m) => {
        if (m.bloqueada) return `<div class="msg msg--bloqueada"><span class="msg-texto"><strong>${esc(m.remetente.split(' ')[0])}:</strong> ${esc(m.conteudo)}</span><span class="msg-aviso">Mensagem bloqueada (contato externo)</span></div>`;
        return m.doCliente
            ? `<div class="msg msg--minha">${esc(m.conteudo)}</div>`
            : `<div class="msg msg--outro"><strong>${esc(m.remetente.split(' ')[0])}:</strong> ${esc(m.conteudo)}</div>`;
    }).join('')}</div>`;
}

function renderDetalhe() {
    const d = detalhe;
    const resolvida = d.status === 'RESOLVIDA' || d.status === 'ARQUIVADA';
    $('#detalhe').innerHTML = `
        <div class="adm-detalhe-topo">
            <h2>Disputa ${esc(d.numero)}</h2>
            <span>Contrato ${esc(d.numeroContrato)}</span>
        </div>
        <div class="segmented adm-abas" role="tablist">
            <button type="button" data-painel="chat" aria-pressed="${painel === 'chat'}">Chat</button>
            <button type="button" data-painel="contrato" aria-pressed="${painel === 'contrato'}">Contrato</button>
            <button type="button" data-painel="entregas" aria-pressed="${painel === 'entregas'}">Entregas</button>
        </div>
        <div class="adm-painel" id="painel">${conteudoPainel(d)}</div>
        <div class="adm-bloco">
            <span class="label">Motivo informado</span>
            <p>${esc(d.motivo)}</p>
        </div>
        ${resolvida ? `
            <div class="adm-bloco">
                <span class="label">Decisão registrada</span>
                <p>${esc(d.decisao ?? '')}</p>
            </div>` : `
            <form id="form-decisao" novalidate>
                <div class="adm-bloco">
                    <span class="label">Decisão sobre o pagamento (${moeda(d.valor)})</span>
                    <div class="adm-radios">
                        <label><input type="radio" name="decisao" value="LIBERAR"> Liberar ao trabalhador</label>
                        <label><input type="radio" name="decisao" value="ESTORNAR"> Estornar ao cliente</label>
                    </div>
                </div>
                <div class="adm-bloco">
                    <label class="label" for="justificativa">Justificativa da decisão</label>
                    <textarea class="textarea" id="justificativa" maxlength="2000" placeholder="Registre o motivo da decisão."></textarea>
                </div>
                <button class="btn btn-primary btn-block" type="submit" id="btn-decidir">Registrar decisão</button>
            </form>`}`;

    $('#detalhe').querySelectorAll('[data-painel]').forEach((b) => b.addEventListener('click', () => {
        painel = b.dataset.painel;
        renderDetalhe();
    }));

    $('#form-decisao')?.addEventListener('submit', (e) => {
        e.preventDefault();
        const escolha = $('#form-decisao').querySelector('input[name="decisao"]:checked')?.value;
        const justificativa = $('#justificativa').value.trim();
        if (!escolha) return toastErro(new Error('Escolha liberar ao trabalhador ou estornar ao cliente.'));
        if (justificativa.length < 10) return toastErro(new Error('Registre a justificativa da decisão.'));
        return comCarregamento($('#btn-decidir'), async () => {
            try {
                detalhe = await decidirDisputa(d.id, escolha, justificativa);
                toast(escolha === 'LIBERAR' ? 'Pagamento liberado ao trabalhador.' : 'Pagamento estornado ao cliente.');
                renderDetalhe();
                await Promise.all([carregarDisputas(), carregarResumo()]);
            } catch (err) {
                toastErro(err);
            }
        });
    });
}

async function abrirDisputa(id, analisar) {
    selecionada = id;
    painel = 'chat';
    history.replaceState(null, '', `?disputa=${id}`);
    $('#detalhe').innerHTML = '<div class="loading"></div>';
    try {
        detalhe = analisar ? await analisarDisputa(id) : await detalheDisputa(id);
        renderDetalhe();
    } catch (e) {
        $('#detalhe').innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
    await Promise.all([carregarDisputas(), carregarResumo()]);
}

function iniciarDisputas() {
    $('#aba-disputas').hidden = false;
    $('#filtros').addEventListener('click', (e) => {
        const chip = e.target.closest('.chip');
        if (!chip) return;
        filtro = chip.dataset.status;
        carregarDisputas();
    });
    $('#disputas').addEventListener('click', (e) => {
        const analisar = e.target.closest('[data-analisar]');
        const abrir = e.target.closest('[data-abrir]');
        if (analisar) abrirDisputa(Number(analisar.dataset.analisar), true);
        else if (abrir) abrirDisputa(Number(abrir.dataset.abrir), false);
    });
    carregarResumo().catch(toastErro);
    if (selecionada) abrirDisputa(selecionada, false);
    else carregarDisputas();
}

// ================================================================= Contratos
async function iniciarContratos() {
    $('#titulo').textContent = 'Contratos';
    $('#subtitulo').textContent = 'Todos os contratos da plataforma e a situação do pagamento em custódia.';
    $('#aba-contratos').hidden = false;
    const ST = { GERADO: 'Gerado', ACEITO_CLIENTE: 'Assinado pelo cliente', ACEITO_TRABALHADOR: 'Assinado pelo trabalhador', ATIVO: 'Ativo', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado', EM_DISPUTA: 'Em disputa' };
    const PG = { PENDENTE: 'Pendente', EM_CUSTODIA: 'Em custódia', LIBERADO: 'Liberado', REEMBOLSADO: 'Estornado', FALHOU: 'Falhou' };
    try {
        const lista = await listarContratosAdmin();
        $('#contratos').innerHTML = lista.length ? lista.map((c) => `
            <tr>
                <td>${esc(c.numero)}</td><td>${esc(c.cliente)}</td><td>${esc(c.trabalhador)}</td><td>${moeda(c.valor)}</td>
                <td><span class="badge badge--sand">${ST[c.status] ?? c.status}</span></td>
                <td>${c.statusPagamento ? `<span class="badge ${c.statusPagamento === 'EM_CUSTODIA' ? 'badge--peach' : 'badge--blue'}">${PG[c.statusPagamento]}</span>` : '—'}</td>
                <td><a class="link" href="contrato.html?id=${c.id}">Ver →</a></td>
            </tr>`).join('') : vazio(7, 'Nenhum contrato ainda.');
    } catch (e) {
        $('#contratos').innerHTML = vazio(7, esc(e.message));
    }
}

// ================================================================= Usuários
let tipoUsuario = '';
async function carregarUsuarios() {
    document.querySelectorAll('#filtro-usuarios .chip').forEach((c) => c.setAttribute('aria-pressed', String(c.dataset.tipo === tipoUsuario)));
    try {
        const lista = await listarUsuariosAdmin(tipoUsuario);
        $('#usuarios').innerHTML = lista.map((u) => {
            const verif = u.tipo !== 'TRABALHADOR' ? '—'
                : u.verificado ? '<span class="badge badge--green">Verificado</span>'
                    : `<button class="btn btn-outline btn-sm" type="button" data-verificar="${u.id}">Verificar</button>`;
            const acao = u.tipo === 'ADMIN' ? ''
                : u.status === 'ATIVO'
                    ? `<button class="btn btn-outline btn-sm" type="button" data-status="SUSPENSO" data-id="${u.id}">Suspender</button>`
                    : `<button class="btn btn-primary btn-sm" type="button" data-status="ATIVO" data-id="${u.id}">Reativar</button>`;
            return `
                <tr>
                    <td><strong>${esc(u.nome)}</strong></td><td>${esc(u.email)}</td><td>${PAPEL[u.tipo]}</td>
                    <td><span class="badge ${u.status === 'ATIVO' ? 'badge--green' : 'badge--red'}">${esc(u.status.charAt(0) + u.status.slice(1).toLowerCase())}</span></td>
                    <td>${verif}</td><td>${data(u.dataCadastro)}</td><td>${acao}</td>
                </tr>`;
        }).join('') || vazio(7, 'Nenhum usuário.');
    } catch (e) {
        $('#usuarios').innerHTML = vazio(7, esc(e.message));
    }
}

function iniciarUsuarios() {
    $('#titulo').textContent = 'Usuários';
    $('#subtitulo').textContent = 'Verifique a identidade dos trabalhadores e gerencie o acesso à plataforma.';
    $('#aba-usuarios').hidden = false;
    $('#filtro-usuarios').addEventListener('click', (e) => {
        const chip = e.target.closest('.chip');
        if (!chip) return;
        tipoUsuario = chip.dataset.tipo;
        carregarUsuarios();
    });
    $('#usuarios').addEventListener('click', (e) => {
        const verificar = e.target.closest('[data-verificar]');
        const status = e.target.closest('[data-status]');
        if (verificar) {
            comCarregamento(verificar, async () => {
                try { await verificarTrabalhador(verificar.dataset.verificar); toast('Trabalhador verificado.'); await carregarUsuarios(); } catch (err) { toastErro(err); }
            });
        } else if (status) {
            comCarregamento(status, async () => {
                try { await alterarStatusUsuario(status.dataset.id, status.dataset.status); toast('Situação atualizada.'); await carregarUsuarios(); } catch (err) { toastErro(err); }
            });
        }
    });
    carregarUsuarios();
}

// ================================================================= Termos
async function carregarVersoes() {
    const versoes = await listarTermos();
    $('#versoes').innerHTML = `<h3>Versões publicadas</h3><ul>${versoes.map((t, i) => `
        <li><strong>Versão ${esc(t.versao)}</strong>${i === 0 ? ' <span class="badge badge--green">Vigente</span>' : ''}<br><span class="muted">${data(t.dataPublicacao)} · ${t.itens.length} itens</span></li>`).join('')}</ul>`;
    if (!$('#texto-termo').value && versoes[0]) {
        $('#texto-termo').value = versoes[0].itens.map((i) => (i.destaque ? `!${i.texto}` : i.texto)).join('\n');
    }
}

function iniciarTermos() {
    $('#titulo').textContent = 'Termos de uso';
    $('#subtitulo').textContent = 'Controle de versões dos Termos de Uso aceitos por clientes e trabalhadores.';
    $('#aba-termos').hidden = false;
    $('#form-termo').addEventListener('submit', (e) => {
        e.preventDefault();
        comCarregamento($('#btn-publicar'), async () => {
            try {
                await publicarTermo({ versao: $('#versao').value.trim(), texto: $('#texto-termo').value.trim() });
                toast('Nova versão publicada.');
                $('#versao').value = '';
                await carregarVersoes();
            } catch (err) {
                toastErro(err);
            }
        });
    });
    carregarVersoes().catch(toastErro);
}

({ disputas: iniciarDisputas, contratos: iniciarContratos, usuarios: iniciarUsuarios, termos: iniciarTermos })[aba]();
