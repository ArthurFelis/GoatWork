import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro } from '../components/toast.js';
import { obterPagamento, pagar } from '../api/pagamentos.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, moeda } from '../utils/formatadores.js';
import { aplicarMascara, mascaraCartao, mascaraValidade, soDigitos } from '../utils/validacoes.js';

const usuario = exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('contratos');
carregarFooter();

const $ = (s) => document.querySelector(s);
const idContrato = Number(param('contrato'));
let metodo = 'PIX';
let resumo = null;

aplicarMascara($('#cartao-numero'), mascaraCartao);
aplicarMascara($('#cartao-validade'), mascaraValidade);
aplicarMascara($('#cartao-cvv'), (v) => soDigitos(v).slice(0, 4));

document.querySelectorAll('#metodo button').forEach((b) => b.addEventListener('click', () => {
    metodo = b.dataset.metodo;
    document.querySelectorAll('#metodo button').forEach((x) => x.setAttribute('aria-pressed', String(x === b)));
    $('#painel-pix').hidden = metodo !== 'PIX';
    $('#painel-cartao').hidden = metodo !== 'CARTAO';
}));

$('#btn-copiar').addEventListener('click', async () => {
    const campo = $('#pix-codigo');
    try {
        await navigator.clipboard.writeText(campo.value);
    } catch {
        campo.select();
        document.execCommand?.('copy');
    }
    toast('Código Pix copiado.');
});

function marcarSituacao(status) {
    const ordem = ['PENDENTE', 'EM_CUSTODIA', 'LIBERADO'];
    const atual = status === 'FALHOU' ? 0 : Math.max(0, ordem.indexOf(status));
    document.querySelectorAll('#situacao li').forEach((li, i) => {
        li.classList.toggle('is-active', i === atual);
        li.classList.toggle('is-done', i < atual);
    });
}

function render(r) {
    resumo = r;
    document.title = `Pagamento ${r.numeroContrato} — GoatWork`;
    $('#numero').textContent = `Contrato Nº ${r.numeroContrato}`;
    $('#total').textContent = moeda(r.valorTotal);
    $('#valor-pix').textContent = moeda(r.valorTotal);
    $('#v-servico').textContent = moeda(r.valorTotal);
    $('#t-taxa').textContent = `Taxa da plataforma (${Number(r.taxaPerc).toLocaleString('pt-BR')}%)`;
    $('#v-taxa').textContent = moeda(r.valorTaxa);
    $('#v-liquido').textContent = moeda(r.valorLiquido);
    $('#pix-codigo').value = r.pixCopiaECola;
    $('#servico').textContent = `${r.servico.replace(/\.$/, '')} · ${r.trabalhador} · ${data(r.prazo)}`;
    marcarSituacao(r.status);

    const pago = ['EM_CUSTODIA', 'LIBERADO', 'REEMBOLSADO'].includes(r.status);
    const acao = $('#acao');
    if (pago) {
        const textos = { EM_CUSTODIA: 'Pagamento em custódia ✓', LIBERADO: 'Pagamento liberado ✓', REEMBOLSADO: 'Pagamento estornado' };
        acao.innerHTML = `<div class="pg-status-ok">${textos[r.status]}</div><a class="link" href="contrato.html?id=${r.idContrato}">Ver contrato →</a>`;
        document.querySelectorAll('#metodo button').forEach((b) => {
            b.disabled = true;
            b.setAttribute('aria-pressed', String(b.dataset.metodo === (r.metodo ?? 'PIX')));
        });
        $('#painel-pix').hidden = r.metodo === 'CARTAO';
        $('#painel-cartao').hidden = true;
    } else if (usuario.tipo !== 'CLIENTE') {
        acao.innerHTML = '<div class="pg-status-ok">Aguardando o pagamento do cliente</div>';
    } else if (r.statusContrato !== 'ATIVO') {
        acao.innerHTML = `<div class="pg-status-ok">Assine o contrato para pagar</div><a class="link" href="contrato.html?id=${r.idContrato}">Ver contrato →</a>`;
    } else {
        acao.innerHTML = `<button class="btn btn-orange" type="button" id="btn-pagar">Pagar ${moeda(r.valorTotal)}</button>`;
        $('#btn-pagar').addEventListener('click', confirmarPagamento);
    }
}

async function confirmarPagamento(e) {
    const botao = e.currentTarget;
    const dados = { metodo };
    if (metodo === 'CARTAO') {
        dados.numeroCartao = soDigitos($('#cartao-numero').value);
        dados.nomeCartao = $('#cartao-nome').value.trim();
        dados.validade = $('#cartao-validade').value.trim();
        dados.cvv = $('#cartao-cvv').value.trim();
        if (dados.numeroCartao.length < 13) return toastErro(new Error('Número do cartão inválido.'));
        if (!dados.nomeCartao) return toastErro(new Error('Informe o nome impresso no cartão.'));
        if (!/^(0[1-9]|1[0-2])\/\d{2}$/.test(dados.validade)) return toastErro(new Error('Validade inválida (MM/AA).'));
        if (dados.cvv.length < 3) return toastErro(new Error('CVV inválido.'));
    }
    botao.disabled = true;
    botao.classList.add('is-loading');
    try {
        render(await pagar(idContrato, dados));
        toast('Pagamento confirmado! O valor está em custódia.');
    } catch (err) {
        toastErro(err);
        botao.disabled = false;
        botao.classList.remove('is-loading');
    }
}

async function carregar() {
    try {
        render(await obterPagamento(idContrato));
    } catch (e) {
        document.querySelector('.pg-card').innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

carregar();
