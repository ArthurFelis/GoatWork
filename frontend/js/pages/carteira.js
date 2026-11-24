import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, comCarregamento } from '../components/toast.js';
import { obterCarteira, sacar } from '../api/carteira.js';
import { exigirUsuario } from '../utils/storage.js';
import { dataHora, esc, lerMoeda, moeda } from '../utils/formatadores.js';
import {
    aplicarMascara, cnpjValido, cpfValido, emailValido, mascaraCnpj, mascaraCpf, mascaraMoeda, mascaraTelefone, soDigitos
} from '../utils/validacoes.js';

exigirUsuario(['TRABALHADOR']);
carregarNavbar('carteira');
carregarFooter();

const $ = (s) => document.querySelector(s);
const erro = $('#erro');
const campoValor = $('#valor');
const campoChave = $('#chave');
const tipoChave = $('#tipo-chave');
const botao = $('#btn-retirar');
let carteira = null;
let filtro = '';
let confirmando = false;

const SITUACAO = {
    LIBERADO: ['Liberado', 'badge--green'],
    EM_CUSTODIA: ['Em custódia', 'badge--peach'],
    PAGO: ['Transferido', 'badge--blue'],
    SOLICITADO: ['Processando', 'badge--sand'],
    FALHOU: ['Falhou · estornado', 'badge--red']
};

const CHAVES = {
    CPF: { placeholder: '000.000.000-00', mascara: mascaraCpf, modo: 'numeric', valida: cpfValido, erro: 'Informe um CPF válido.' },
    CNPJ: { placeholder: '00.000.000/0000-00', mascara: mascaraCnpj, modo: 'numeric', valida: cnpjValido, erro: 'Informe um CNPJ válido.' },
    EMAIL: { placeholder: 'voce@email.com', mascara: null, modo: 'email', valida: emailValido, erro: 'Informe um e-mail válido.' },
    TELEFONE: { placeholder: '(11) 90000-0000', mascara: mascaraTelefone, modo: 'tel', valida: (v) => [10, 11].includes(soDigitos(v).length), erro: 'Informe o celular com DDD.' },
    ALEATORIA: {
        placeholder: '123e4567-e89b-12d3-a456-426614174000', mascara: null, modo: 'text',
        valida: (v) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v.trim()),
        erro: 'A chave aleatória tem 36 caracteres (letras, números e hífens).'
    }
};

aplicarMascara(campoValor, mascaraMoeda);
campoChave.addEventListener('input', () => {
    const m = CHAVES[tipoChave.value].mascara;
    if (m) campoChave.value = m(campoChave.value);
});

function mostrarErro(msg) {
    erro.textContent = msg;
    erro.hidden = !msg;
}

function cancelarConfirmacao() {
    confirmando = false;
    $('#confirmar').hidden = true;
    botao.hidden = false;
}

function atualizarChave(limpar = true) {
    const cfg = CHAVES[tipoChave.value];
    campoChave.placeholder = cfg.placeholder;
    campoChave.inputMode = cfg.modo;
    if (limpar) campoChave.value = '';
    $('#btn-meu-doc').hidden = !carteira || tipoChave.value !== carteira.documentoTipo;
    cancelarConfirmacao();
}

function atualizarBotao() {
    const valor = lerMoeda(campoValor.value) ?? 0;
    const pode = carteira && carteira.disponivel >= carteira.saqueMinimo;
    botao.disabled = !pode;
    botao.textContent = !pode
        ? 'Sem saldo para retirar'
        : valor > 0 ? `Retirar ${moeda(valor)}` : 'Retirar';
}

function renderMovimentos() {
    const lista = carteira.movimentacoes.filter((m) => !filtro
        || (filtro === 'ENTRADA' ? m.tipo !== 'SAQUE' : m.tipo === 'SAQUE'));
    document.querySelectorAll('#filtros .chip').forEach((c) => c.setAttribute('aria-pressed', String(c.dataset.filtro === filtro)));

    $('#movimentos').innerHTML = lista.length ? lista.map((m) => {
        const [rotulo, classe] = SITUACAO[m.situacao] ?? [m.situacao, 'badge--sand'];
        const classeValor = m.situacao === 'FALHOU' ? 'is-falhou' : m.tipo === 'SAQUE' ? 'is-saque' : m.tipo === 'A_RECEBER' ? 'is-pendente' : 'is-entrada';
        const sinal = m.tipo === 'SAQUE' ? '− ' : '+ ';
        return `
            <tr>
                <td class="cart-data">${esc(dataHora(m.data))}</td>
                <td>${esc(m.descricao)}</td>
                <td><span class="badge ${classe}">${rotulo}</span></td>
                <td class="cart-valor ${classeValor}">${sinal}${moeda(Math.abs(m.valor))}</td>
            </tr>`;
    }).join('') : `<tr><td colspan="4"><div class="empty" style="border:0"><strong>Nada por aqui ainda</strong>${filtro === 'SAQUE'
        ? 'Seus saques aparecem aqui.'
        : 'Quando um cliente aprovar uma entrega, o valor líquido entra na sua carteira.'}</div></td></tr>`;
}

function render(c) {
    carteira = c;
    $('#saldo').textContent = moeda(c.disponivel);
    $('#custodia').textContent = moeda(c.emCustodia);
    $('#recebido').textContent = moeda(c.totalRecebido);
    $('#sacado').textContent = moeda(c.totalSacado);
    $('#minimo').textContent = `Disponível: ${moeda(c.disponivel)} · mínimo por saque: ${moeda(c.saqueMinimo)}`;
    if (!campoChave.value && tipoChave.value === 'CPF' && c.documentoTipo === 'CNPJ') {
        tipoChave.value = 'CNPJ';
    }
    atualizarChave(false);
    atualizarBotao();
    renderMovimentos();
}

$('#filtros').addEventListener('click', (e) => {
    const chip = e.target.closest('.chip');
    if (!chip || !carteira) return;
    filtro = chip.dataset.filtro;
    renderMovimentos();
});

tipoChave.addEventListener('change', () => atualizarChave());
campoValor.addEventListener('input', () => { cancelarConfirmacao(); atualizarBotao(); });
campoChave.addEventListener('input', cancelarConfirmacao);

$('#btn-tudo').addEventListener('click', () => {
    if (!carteira) return;
    campoValor.value = moeda(carteira.disponivel);
    cancelarConfirmacao();
    atualizarBotao();
});

$('#btn-meu-doc').addEventListener('click', () => {
    if (!carteira) return;
    campoChave.value = CHAVES[carteira.documentoTipo].mascara(carteira.documento);
    cancelarConfirmacao();
});

// 1º clique valida e pede confirmação; a retirada só acontece em "Confirmar retirada"
$('#form-saque').addEventListener('submit', (e) => {
    e.preventDefault();
    mostrarErro('');
    if (!carteira || confirmando) return;
    const valor = lerMoeda(campoValor.value);
    const cfg = CHAVES[tipoChave.value];
    const chave = campoChave.value.trim();

    if (!valor || valor <= 0) return mostrarErro('Informe o valor que deseja retirar.');
    if (valor < carteira.saqueMinimo) return mostrarErro(`O valor mínimo para saque é ${moeda(carteira.saqueMinimo)}.`);
    if (valor > carteira.disponivel) return mostrarErro(`Saldo insuficiente. Disponível: ${moeda(carteira.disponivel)}.`);
    if (!chave || !cfg.valida(chave)) return mostrarErro(`Chave Pix inválida. ${cfg.erro}`);

    confirmando = true;
    botao.hidden = true;
    const caixa = $('#confirmar');
    caixa.innerHTML = `
        Confirmar a retirada de <strong>${moeda(valor)}</strong> para a chave
        <strong>${esc(tipoChave.options[tipoChave.selectedIndex].text)} ${esc(chave)}</strong>?
        <div class="cart-confirmar-acoes">
            <button class="btn btn-secondary" type="button" id="btn-voltar">Voltar</button>
            <button class="btn btn-orange" type="button" id="btn-confirmar">Confirmar retirada</button>
        </div>`;
    caixa.hidden = false;
    $('#btn-voltar').addEventListener('click', cancelarConfirmacao);
    $('#btn-confirmar').addEventListener('click', (ev) => comCarregamento(ev.currentTarget, async () => {
        try {
            const atualizada = await sacar(valor, tipoChave.value, chave);
            campoValor.value = '';
            cancelarConfirmacao();
            render(atualizada);
            toast(`Saque de ${moeda(valor)} enviado para a sua chave Pix.`);
        } catch (err) {
            cancelarConfirmacao();
            mostrarErro(err.message);
            try { render(await obterCarteira()); } catch { /* mantém a tela */ }
        }
    }));
});

async function carregar() {
    try {
        render(await obterCarteira());
    } catch (e) {
        $('#movimentos').innerHTML = `<tr><td colspan="4"><div class="empty">${esc(e.message)}</div></td></tr>`;
        mostrarErro(e.message);
    }
}

carregar();
