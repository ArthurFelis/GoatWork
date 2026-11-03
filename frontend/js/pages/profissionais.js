import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { listarTrabalhadores } from '../api/trabalhadores.js';
import { listarCategorias } from '../api/categorias.js';
import { esc, nota, valorReferencia } from '../utils/formatadores.js';
import { param, usuarioAtual } from '../utils/storage.js';

carregarNavbar('profissionais');
carregarFooter();

const lista = document.querySelector('#lista');
const chips = document.querySelector('#categorias');
const busca = document.querySelector('#busca');
const destaque = param('destaque');
let categoria = param('categoria') || '';
let timer = null;

function hrefContratar(p) {
    const qs = new URLSearchParams({ trabalhador: p.id });
    const cat = categoria && p.idsCategorias.includes(Number(categoria)) ? categoria : p.idsCategorias[0];
    if (cat) qs.set('categoria', cat);
    return `novo-pedido.html?${qs}`;
}

async function carregar() {
    lista.innerHTML = '<div class="loading">Carregando profissionais…</div>';
    try {
        const profissionais = await listarTrabalhadores({ busca: busca.value.trim(), categoria });
        if (!profissionais.length) {
            lista.innerHTML = '<div class="empty"><strong>Nenhum profissional encontrado</strong>Tente outra busca ou categoria — ou conte o que precisa para a nossa IA.</div>';
            return;
        }
        const usuario = usuarioAtual();
        const podeContratar = !usuario || usuario.tipo === 'CLIENTE';
        lista.innerHTML = profissionais.map((p) => `
            <article class="prof-item ${String(p.id) === destaque ? 'is-destaque' : ''}" id="prof-${p.id}">
                <span class="avatar avatar--sm" style="width:46px;height:46px;font-size:14px">${esc(p.iniciais)}</span>
                <div>
                    <div class="prof-topo">
                        <h2 class="prof-nome">${esc(p.nome)}</h2>
                        <span class="prof-preco">${esc(valorReferencia(p.valor, p.unidade))}</span>
                    </div>
                    <p class="prof-sub">${esc([p.profissao, p.cidade].filter(Boolean).join(' · '))}</p>
                    <p class="prof-bio">${esc(p.biografia || p.categorias.join(', '))}</p>
                    <div class="prof-rodape">
                        <span class="prof-nota">★ ${nota(p.nota)} · ${p.servicos} serviços${p.verificado ? '' : ''}</span>
                        ${podeContratar ? `<a class="btn btn-primary" href="${hrefContratar(p)}">Contratar</a>` : ''}
                    </div>
                </div>
            </article>`).join('');
        if (destaque) document.querySelector(`#prof-${destaque}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' });
    } catch (e) {
        lista.innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

function marcarChip() {
    chips.querySelectorAll('.chip').forEach((c) => c.setAttribute('aria-pressed', String(c.dataset.id === String(categoria))));
}

async function iniciar() {
    try {
        const categorias = await listarCategorias();
        chips.insertAdjacentHTML('beforeend', categorias.map((c) =>
            `<button class="chip" type="button" data-id="${c.id}" aria-pressed="false">${esc(c.nome)}</button>`).join(''));
    } catch { /* lista aparece mesmo sem categorias */ }

    chips.addEventListener('click', (e) => {
        const chip = e.target.closest('.chip');
        if (!chip) return;
        categoria = chip.dataset.id;
        marcarChip();
        carregar();
    });
    busca.addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(carregar, 300);
    });
    document.querySelector('#form-busca').addEventListener('submit', (e) => {
        e.preventDefault();
        carregar();
    });
    marcarChip();
    carregar();
}

iniciar();
