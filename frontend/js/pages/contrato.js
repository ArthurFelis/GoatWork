import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import { assinarContrato, obterContrato, recusarContrato } from '../api/contratos.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { esc, moeda } from '../utils/formatadores.js';

const usuario = exigirUsuario();
carregarNavbar(usuario.tipo === 'ADMIN' ? 'admin-contratos' : 'contratos');
carregarFooter();

const $ = (s) => document.querySelector(s);
const id = Number(param('id'));
const CHECK = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>';

function linhaAssinatura(rotulo, assinadoEm, souEu) {
    let direita;
    if (assinadoEm) direita = `<span class="ok">${CHECK} Assinado</span>`;
    else if (souEu) direita = '<button class="btn btn-primary" type="button" id="btn-assinar">Assinar</button>';
    else direita = '<span class="pendente">Pendente</span>';
    return `<div class="ct-assinatura"><span>${rotulo}</span>${direita}</div>`;
}

function acaoPagamento(c) {
    const pagamento = c.statusPagamento;
    if (c.status === 'CANCELADO') return 'Contrato cancelado. Valor estornado ao cliente.';
    if (pagamento === 'LIBERADO') return 'Pagamento liberado ao trabalhador.';
    if (pagamento === 'REEMBOLSADO') return 'Pagamento estornado ao cliente.';
    if (pagamento === 'EM_CUSTODIA') {
        if (c.meuPapel === 'CLIENTE') return `<a class="btn btn-orange btn-block" href="entrega.html?contrato=${c.id}">${c.etapa === 'ENTREGA_ENVIADA' ? 'Aprovar entrega' : 'Acompanhar entrega'}</a>`;
        if (c.meuPapel === 'TRABALHADOR') return `<a class="btn btn-orange btn-block" href="entrega.html?contrato=${c.id}">Registrar entrega</a>`;
        return 'Pagamento em custódia.';
    }
    if (c.status === 'ATIVO') {
        return c.meuPapel === 'CLIENTE'
            ? `<a class="btn btn-orange btn-block" href="pagamento.html?contrato=${c.id}">Pagar ${moeda(c.valor)}</a>`
            : 'Aguardando o pagamento do cliente.';
    }
    return 'Assine para pagar';
}

function render(c) {
    document.title = `Contrato ${c.numero} — GoatWork`;
    $('#contrato').innerHTML = `
        <p class="page-eyebrow">Contrato Nº ${esc(c.numero)}</p>
        <h1>Contrato de prestação de serviço</h1>
        <p class="ct-gerado">Gerado automaticamente a partir da conversa com a IA e o profissional.</p>
        <dl class="info-strip">
            <div><dt>Cliente</dt><dd>${esc(c.cliente)}</dd></div>
            <div><dt>Trabalhador</dt><dd>${esc(c.trabalhador)}${c.profissaoTrabalhador ? ` — ${esc(c.profissaoTrabalhador)}` : ''}</dd></div>
        </dl>
        ${c.clausulas.map((cl) => `
            <section class="ct-clausula">
                <h2>${esc(cl.titulo)}</h2>
                <p>${esc(cl.texto)}</p>
            </section>`).join('')}`;

    $('#assinaturas').innerHTML =
        linhaAssinatura('Trabalhador', c.aceiteTrabalhadorEm, c.meuPapel === 'TRABALHADOR') +
        linhaAssinatura('Cliente', c.aceiteClienteEm, c.meuPapel === 'CLIENTE') +
        (c.meuPapel === 'CLIENTE' && !c.aceiteClienteEm && ['GERADO', 'ACEITO_TRABALHADOR'].includes(c.status)
            ? '<button class="ct-recusar" type="button" id="btn-recusar">Recusar orçamento e voltar ao chat</button>'
            : '');

    $('#total').textContent = moeda(c.valor);
    if (c.meuPapel !== 'CLIENTE') $('#nota-total').textContent = 'O valor só é liberado ao trabalhador após a confirmação do cliente.';
    $('#acao-pagamento').innerHTML = acaoPagamento(c);
    $('#link-chat').href = `chat.html?chat=${c.idChat}`;
    $('#link-chat').hidden = c.meuPapel === 'ADMIN';

    $('#btn-recusar')?.addEventListener('click', (e) => comCarregamento(e.currentTarget, async () => {
        try {
            await recusarContrato(c.id);
            location.href = `chat.html?chat=${c.idChat}`;
        } catch (err) {
            toastErro(err);
        }
    }));

    $('#btn-assinar')?.addEventListener('click', (e) => comCarregamento(e.currentTarget, async () => {
        try {
            const atualizado = await assinarContrato(c.id);
            toast(atualizado.status === 'ATIVO' ? 'Contrato assinado pelas duas partes!' : 'Contrato assinado. Aguardando a outra parte.');
            render(atualizado);
        } catch (err) {
            toastErro(err);
        }
    }));
}

async function carregar() {
    try {
        render(await obterContrato(id));
    } catch (e) {
        $('#contrato').innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

carregar();
