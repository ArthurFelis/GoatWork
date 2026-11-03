import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast } from '../components/toast.js';
import { listarCategorias } from '../api/categorias.js';
import { meuPerfil, salvarMeuPerfil } from '../api/trabalhadores.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, lerMoeda, moeda, nota } from '../utils/formatadores.js';
import { aplicarMascara, mascaraMoeda } from '../utils/validacoes.js';

exigirUsuario(['TRABALHADOR']);
carregarNavbar('perfil');
carregarFooter();

const $ = (s) => document.querySelector(s);
const erro = $('#erro');
const caixaAtuacoes = $('#atuacoes');
let categorias = [];
let perfil = null;

aplicarMascara($('#valor'), mascaraMoeda);

const CHECK = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>';

function mostrarErro(msg) {
    erro.textContent = msg;
    erro.hidden = !msg;
}

function linhaAtuacao(a = {}) {
    const linha = document.createElement('div');
    linha.className = 'pf-atuacao';
    const id = Math.random().toString(36).slice(2, 8);
    linha.innerHTML = `
        <div>
            <label for="cat-${id}">Categoria</label>
            <select class="select" id="cat-${id}" data-campo="categoria">
                ${categorias.map((c) => `<option value="${c.id}" ${c.id === a.idCategoria ? 'selected' : ''}>${esc(c.nome)}</option>`).join('')}
            </select>
        </div>
        <div>
            <label for="anos-${id}">Anos de experiência</label>
            <input class="input" id="anos-${id}" data-campo="anos" type="number" min="0" max="80" value="${a.anosExperiencia ?? 0}">
        </div>
        <button class="pf-remover" type="button" aria-label="Remover categoria">✕</button>`;
    linha.querySelector('.pf-remover').addEventListener('click', () => linha.remove());
    return linha;
}

function renderResumo(p) {
    $('#resumo').innerHTML = `
        <span class="avatar avatar--xl">${esc(p.iniciais)}</span>
        <h2>${esc(p.nome)}</h2>
        <p class="pf-sub">${esc([p.profissao, p.cidade].filter(Boolean).join(' · ') || 'Complete seu perfil')}</p>
        ${p.verificado
            ? `<span class="badge badge--peach pf-selo">${CHECK} Perfil verificado</span>`
            : '<span class="badge badge--sand pf-selo">Verificação pendente</span>'}
        <div class="pf-stats">
            <div><strong class="nota">★ ${nota(p.nota)}</strong><span>Nota média</span></div>
            <div><strong class="servicos">${p.servicos}</strong><span>Serviços</span></div>
        </div>`;
    $('#verificacao').textContent = p.verificado
        ? `Verificado em ${data(p.dataVerificacao)}. Mantenha seus dados atualizados para continuar com o selo.`
        : 'Seus dados estão em análise pela equipe GoatWork. O selo aparece assim que a verificação for concluída.';
}

function preencher(p) {
    perfil = p;
    renderResumo(p);
    $('#biografia').value = p.biografia ?? '';
    $('#profissao').value = p.profissao ?? '';
    $('#cidade').value = p.cidade ?? '';
    $('#valor').value = p.valorReferencia != null ? moeda(p.valorReferencia) : '';
    $('#unidade').value = p.unidadeValor ?? 'h';
    caixaAtuacoes.innerHTML = '';
    (p.atuacoes.length ? p.atuacoes : [{}]).forEach((a) => caixaAtuacoes.appendChild(linhaAtuacao(a)));
}

$('#btn-add').addEventListener('click', () => {
    const usadas = [...caixaAtuacoes.querySelectorAll('[data-campo="categoria"]')].map((s) => Number(s.value));
    const livre = categorias.find((c) => !usadas.includes(c.id));
    caixaAtuacoes.appendChild(linhaAtuacao({ idCategoria: livre?.id }));
});

$('#btn-cancelar').addEventListener('click', () => {
    if (perfil) preencher(perfil);
    mostrarErro('');
});

$('#form-perfil').addEventListener('submit', async (e) => {
    e.preventDefault();
    mostrarErro('');
    const atuacoes = [...caixaAtuacoes.querySelectorAll('.pf-atuacao')].map((l) => ({
        idCategoria: Number(l.querySelector('[data-campo="categoria"]').value),
        anosExperiencia: Number(l.querySelector('[data-campo="anos"]').value || 0)
    }));
    if (!atuacoes.length) return mostrarErro('Adicione pelo menos uma categoria de atuação.');
    if (new Set(atuacoes.map((a) => a.idCategoria)).size !== atuacoes.length) return mostrarErro('Cada categoria só pode aparecer uma vez.');

    const botao = $('#btn-salvar');
    botao.disabled = true;
    botao.classList.add('is-loading');
    try {
        const atualizado = await salvarMeuPerfil({
            biografia: $('#biografia').value.trim() || null,
            profissao: $('#profissao').value.trim() || null,
            cidade: $('#cidade').value.trim() || null,
            valorReferencia: lerMoeda($('#valor').value),
            unidadeValor: $('#unidade').value,
            atuacoes
        });
        preencher(atualizado);
        toast('Perfil salvo.');
    } catch (err) {
        mostrarErro(err.message);
    }
    botao.disabled = false;
    botao.classList.remove('is-loading');
});

async function carregar() {
    try {
        [categorias, perfil] = await Promise.all([listarCategorias(), meuPerfil()]);
        preencher(perfil);
        if (param('novo')) toast('Conta criada! Complete seu perfil para receber convites.');
    } catch (e) {
        mostrarErro(e.message);
    }
}

carregar();
