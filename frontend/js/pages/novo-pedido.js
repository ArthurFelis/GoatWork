import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro } from '../components/toast.js';
import { listarCategorias } from '../api/categorias.js';
import { obterTrabalhador } from '../api/trabalhadores.js';
import { atualizarPedido, criarPedido, obterPedido } from '../api/pedidos.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { esc } from '../utils/formatadores.js';

exigirUsuario(['CLIENTE']);
carregarNavbar();
carregarFooter();

const $ = (s) => document.querySelector(s);
const chips = $('#categorias');
const descricao = $('#descricao');
const erro = $('#erro');
let categoria = Number(param('categoria')) || null;
let idTrabalhador = Number(param('trabalhador')) || null;
let idRascunho = Number(param('pedido')) || null;

function mostrarErro(msg) {
    erro.textContent = msg;
    erro.hidden = !msg;
}

function marcarCategoria() {
    chips.querySelectorAll('.chip').forEach((c) => {
        const ativo = Number(c.dataset.id) === categoria;
        c.setAttribute('aria-pressed', String(ativo));
        c.setAttribute('aria-checked', String(ativo));
    });
}

async function iniciar() {
    try {
        const categorias = await listarCategorias();
        chips.innerHTML = categorias.map((c) =>
            `<button class="chip" type="button" role="radio" data-id="${c.id}" aria-pressed="false">${esc(c.nome)}</button>`).join('');
        if (!categoria) categoria = categorias[0]?.id ?? null;

        if (idRascunho) {
            const p = await obterPedido(idRascunho);
            if (p.status !== 'RASCUNHO') {
                location.replace(p.status === 'EM_BRIEFING' ? `chat-ia.html?pedido=${p.id}` : 'contratos.html');
                return;
            }
            categoria = p.idCategoria;
            descricao.value = p.descricao;
            idTrabalhador = p.idTrabalhadorPreferido;
        }
        marcarCategoria();

        if (idTrabalhador) {
            const t = await obterTrabalhador(idTrabalhador);
            const box = $('#preferido');
            box.innerHTML = `<span>Pedido direcionado para <strong>${esc(t.nome)}</strong> · ${esc(t.profissao ?? '')}</span>
                ${idRascunho ? '' : '<button type="button" id="remover-preferido">Enviar para todos da categoria</button>'}`;
            box.hidden = false;
            $('#remover-preferido')?.addEventListener('click', () => {
                idTrabalhador = null;
                box.hidden = true;
            });
        }
    } catch (e) {
        toastErro(e);
    }
}

chips.addEventListener('click', (e) => {
    const chip = e.target.closest('.chip');
    if (!chip) return;
    categoria = Number(chip.dataset.id);
    marcarCategoria();
});

async function salvar(rascunho, botao) {
    mostrarErro('');
    const texto = descricao.value.trim();
    if (!categoria) return mostrarErro('Escolha a categoria do serviço.');
    if (texto.length < 10) {
        descricao.focus();
        return mostrarErro('Conte em poucas palavras o que você precisa.');
    }
    botao.disabled = true;
    botao.classList.add('is-loading');
    try {
        const pedido = idRascunho
            ? await atualizarPedido(idRascunho, { idCategoria: categoria, descricao: texto, rascunho })
            : await criarPedido({ idCategoria: categoria, descricao: texto, idTrabalhador, rascunho });
        idRascunho = pedido.id;
        if (rascunho) {
            history.replaceState(null, '', `?pedido=${pedido.id}`);
            toast('Rascunho salvo. Você pode continuar depois em Contratos.');
        } else {
            location.href = `chat-ia.html?pedido=${pedido.id}`;
            return;
        }
    } catch (e) {
        mostrarErro(e.message);
    }
    botao.disabled = false;
    botao.classList.remove('is-loading');
}

$('#btn-rascunho').addEventListener('click', (e) => salvar(true, e.currentTarget));
$('#form-pedido').addEventListener('submit', (e) => {
    e.preventDefault();
    salvar(false, $('#btn-continuar'));
});

iniciar();
