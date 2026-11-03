import { termoAtual } from '../api/termos.js';
import { esc } from '../utils/formatadores.js';

/** Painel azul "Termos de uso — Válidos para clientes e trabalhadores." (telas Entrar e Cadastro). */
export async function carregarPainelTermos(alvo) {
    if (!alvo) return;
    alvo.innerHTML = '<h2>Termos de uso</h2><p class="termos-sub">Válidos para clientes e trabalhadores.</p><div class="loading" style="color:#c9d1e6">Carregando…</div>';
    try {
        const termo = await termoAtual();
        alvo.innerHTML = `
            <h2>Termos de uso</h2>
            <p class="termos-sub">Válidos para clientes e trabalhadores.</p>
            <ol class="termos-lista">
                ${termo.itens.map((item, i) => `
                    <li class="${item.destaque ? 'is-destaque' : ''}">
                        <span class="num-dot">${i + 1}</span>
                        <span>${esc(item.texto)}</span>
                    </li>`).join('')}
            </ol>`;
    } catch (e) {
        alvo.querySelector('.loading').textContent = e.message;
    }
}
