import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import { aprovarEntrega, enviarEntrega, obterEntregas, rejeitarEntrega } from '../api/entregas.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, dataHora, esc, iniciais, moeda } from '../utils/formatadores.js';

exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('contratos');
carregarFooter();

const app = document.querySelector('#app');
const idContrato = Number(param('contrato'));
const STATUS = { PENDENTE: 'Aguardando aprovação', APROVADA: 'Aprovada', REJEITADA: 'Rejeitada' };
const ICONE = { PENDENTE: '•', APROVADA: '✓', REJEITADA: '×' };

const cabecalho = (d, titulo, subtitulo) => `
    <header class="np-head">
        <p class="page-eyebrow">Contrato Nº ${esc(d.numeroContrato)}</p>
        <h1 class="page-title">${titulo}</h1>
        <p class="page-subtitle">${subtitulo}</p>
    </header>`;

function historico(d, rascunho = false) {
    const itens = d.entregas.map((e) => `
        <li>
            <span class="en-ico en-ico--${e.status.toLowerCase()}">${ICONE[e.status]}</span>
            <div>
                <strong>Entrega ${e.numero} · ${STATUS[e.status]}</strong>
                <span>${esc(dataHora(e.dataEntrega))}. ${esc(e.descricao.length > 70 ? `${e.descricao.slice(0, 70)}…` : e.descricao)}</span>
            </div>
        </li>`);
    if (rascunho) {
        itens.push(`
            <li class="is-rascunho">
                <span class="en-ico en-ico--rascunho">•</span>
                <div><strong>Entrega ${d.entregas.length + 1} · Rascunho</strong><span>Pendente de envio</span></div>
            </li>`);
    }
    return `
        <section class="card-outline en-historico">
            <h3>Histórico de entregas</h3>
            ${itens.length ? `<ol>${itens.join('')}</ol>` : '<p>Nenhuma entrega registrada ainda.</p>'}
        </section>`;
}

// ------------------------------------------------------------ Trabalhador
function renderTrabalhador(d) {
    const pendente = d.entregas.find((e) => e.status === 'PENDENTE');
    const podeEnviar = d.statusContrato === 'ATIVO' && d.statusPagamento === 'EM_CUSTODIA' && !pendente;
    const rotuloPagamento = { PENDENTE: 'Aguardando pagamento', EM_CUSTODIA: 'Em custódia', LIBERADO: 'Liberado', REEMBOLSADO: 'Estornado', FALHOU: 'Aguardando pagamento' }[d.statusPagamento];

    let principal;
    if (podeEnviar) {
        principal = `
            <form class="card en-card" id="form-entrega" novalidate>
                <h2>Nova entrega</h2>
                <label class="label" for="descricao">Descreva o que foi entregue</label>
                <textarea class="textarea" id="descricao" maxlength="2000" placeholder="Ex.: Quadro de energia substituído e 4 tomadas instaladas. Circuitos testados e identificados no quadro."></textarea>
                <p class="hint">Seja claro: o cliente vai usar este texto para aprovar ou rejeitar.</p>
                <div class="actions"><button class="btn btn-primary" type="submit" id="btn-enviar">Enviar entrega</button></div>
            </form>`;
    } else {
        let estado;
        if (pendente) estado = `<strong>Entrega ${pendente.numero} enviada</strong>Aguardando a aprovação do cliente. O pagamento continua em custódia.`;
        else if (d.statusContrato === 'CONCLUIDO') estado = `<strong>Contrato concluído</strong>Pagamento de ${moeda(d.valorLiquido)} liberado na sua carteira.<br><a class="btn btn-primary" href="avaliacao.html?contrato=${d.idContrato}">Avaliar o cliente</a> <a class="btn btn-secondary" href="carteira.html">Ver carteira</a>`;
        else if (d.statusContrato === 'EM_DISPUTA') estado = `<strong>Contrato em disputa</strong>Um administrador está analisando o chat e o contrato.<br><a class="btn btn-secondary" href="disputa.html?contrato=${d.idContrato}">Ver disputa</a>`;
        else if (d.statusContrato === 'CANCELADO') estado = '<strong>Contrato cancelado</strong>O valor foi estornado ao cliente.';
        else estado = '<strong>Aguardando pagamento</strong>Você poderá registrar a entrega quando o pagamento do cliente estiver em custódia.';
        principal = `<section class="card en-card"><h2>Nova entrega</h2><div class="en-estado">${estado}</div></section>`;
    }

    app.innerHTML = `
        ${cabecalho(d, 'Entrega do serviço', 'Registre a entrega para o cliente aprovar. O pagamento está em custódia e será liberado após a aprovação.')}
        <dl class="info-strip en-strip">
            <div><dt>Cliente</dt><dd>${esc(d.cliente)}</dd></div>
            <div><dt>Serviço</dt><dd>${esc(d.servico.replace(/\.$/, ''))}</dd></div>
            <div><dt>Prazo</dt><dd>${data(d.prazo)}</dd></div>
            <div><dt>Pagamento</dt><dd>${rotuloPagamento} · ${moeda(d.valor)}</dd></div>
        </dl>
        <div class="layout-side layout-side--wide">
            ${principal}
            <aside class="side-stack">
                <div id="historico">${historico(d, podeEnviar)}</div>
                <div class="card-soft">Se o cliente rejeitar, você pode enviar uma nova entrega no mesmo contrato. Se houver desacordo, qualquer parte pode abrir uma <a class="link" href="disputa.html?contrato=${d.idContrato}">disputa</a>.</div>
            </aside>
        </div>`;

    document.querySelector('#form-entrega')?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const texto = document.querySelector('#descricao').value.trim();
        if (texto.length < 10) return toastErro(new Error('Descreva o que foi entregue.'));
        await comCarregamento(document.querySelector('#btn-enviar'), async () => {
            try {
                const atualizado = await enviarEntrega(idContrato, texto);
                toast('Entrega enviada! O cliente vai revisar.');
                render(atualizado);
            } catch (err) {
                toastErro(err);
            }
        });
    });
}

// ------------------------------------------------------------ Cliente
function renderCliente(d) {
    const pendente = [...d.entregas].reverse().find((e) => e.status === 'PENDENTE');
    const subtitulo = pendente
        ? `${esc(d.trabalhador)} registrou a entrega do serviço. Confira e decida.`
        : 'Acompanhe as entregas registradas pelo profissional.';

    let principal;
    if (pendente) {
        principal = `
            <section class="card en-card">
                <div class="ap-topo">
                    <div class="ap-quem">
                        <span class="avatar">${esc(d.iniciaisTrabalhador)}</span>
                        <div><strong>${esc(d.trabalhador)}</strong><span>Entrega ${pendente.numero} · ${esc(dataHora(pendente.dataEntrega))}</span></div>
                    </div>
                    <span class="badge badge--peach">Aguardando sua aprovação</span>
                </div>
                <div class="ap-bloco">
                    <span class="label">O que foi entregue</span>
                    <div class="box-cream">${esc(pendente.descricao)}</div>
                </div>
                <div class="ap-bloco">
                    <span class="label">Combinado no contrato</span>
                    <ul class="bullets">${d.requisitos.map((r) => `<li>${esc(r)}</li>`).join('')}</ul>
                </div>
                <div class="actions">
                    <button class="btn btn-outline" type="button" id="btn-rejeitar">Rejeitar entrega</button>
                    <button class="btn btn-primary" type="button" id="btn-aprovar">Aprovar e liberar pagamento</button>
                </div>
            </section>`;
    } else {
        let estado;
        if (d.statusContrato === 'CONCLUIDO') estado = `<strong>Serviço aprovado</strong>O pagamento foi liberado ao profissional.<br><a class="btn btn-primary" href="avaliacao.html?contrato=${d.idContrato}">Avaliar o serviço</a>`;
        else if (d.statusContrato === 'EM_DISPUTA') estado = `<strong>Contrato em disputa</strong>Um administrador está analisando o caso.<br><a class="btn btn-secondary" href="disputa.html?contrato=${d.idContrato}">Ver disputa</a>`;
        else if (d.statusPagamento !== 'EM_CUSTODIA') estado = `<strong>Pagamento pendente</strong>Pague o contrato para o profissional iniciar.<br><a class="btn btn-primary" href="pagamento.html?contrato=${d.idContrato}">Ir para pagamento</a>`;
        else estado = '<strong>Aguardando entrega</strong>Você será avisado aqui quando o profissional registrar a entrega.';
        principal = `<section class="card en-card"><div class="en-estado">${estado}</div><div style="margin-top:22px">${historico(d)}</div></section>`;
    }

    const liberado = d.statusPagamento === 'LIBERADO';
    app.innerHTML = `
        ${cabecalho(d, pendente ? 'Aprovar entrega' : 'Entrega do serviço', subtitulo)}
        <div class="layout-side pg-layout">
            ${principal}
            <aside class="side-stack">
                <section class="card-navy ap-navy">
                    <span class="small">${liberado ? 'Pagamento liberado' : 'Pagamento em custódia'}</span>
                    <strong class="total">${moeda(d.valor)}</strong>
                    <p>Ao aprovar, ${moeda(d.valorLiquido)} são liberados ao trabalhador (taxa da plataforma: ${moeda(d.valorTaxa)}).</p>
                </section>
                ${d.statusContrato === 'ATIVO' && d.statusPagamento === 'EM_CUSTODIA' ? `
                <section class="card-soft">
                    <h3>Não ficou satisfeito?</h3>
                    <p>Ao rejeitar, o trabalhador pode enviar uma nova entrega. Se não houver acordo, você pode abrir uma disputa e um administrador decide.</p>
                    <a class="link" style="display:inline-block;margin-top:10px;font-size:13px" href="disputa.html?contrato=${d.idContrato}">Abrir disputa →</a>
                </section>` : ''}
            </aside>
        </div>`;

    document.querySelector('#btn-aprovar')?.addEventListener('click', (e) => comCarregamento(e.currentTarget, async () => {
        try {
            await aprovarEntrega(pendente.id);
            location.href = `avaliacao.html?contrato=${d.idContrato}`;
        } catch (err) {
            toastErro(err);
        }
    }));
    document.querySelector('#btn-rejeitar')?.addEventListener('click', (e) => comCarregamento(e.currentTarget, async () => {
        try {
            render(await rejeitarEntrega(pendente.id));
            toast('Entrega rejeitada. O profissional pode enviar uma nova entrega.');
        } catch (err) {
            toastErro(err);
        }
    }));
}

function render(d) {
    document.title = `Entrega ${d.numeroContrato} — GoatWork`;
    if (d.meuPapel === 'TRABALHADOR') renderTrabalhador(d);
    else renderCliente(d);
}

async function carregar() {
    try {
        render(await obterEntregas(idContrato));
    } catch (e) {
        app.innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

carregar();
