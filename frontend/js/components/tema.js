// Botão de modo escuro/claro (fica na barra de navegação de todas as telas)
const CHAVE = 'goatwork.tema';

const LUA = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/></svg>';
const SOL = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="4.5"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>';

export const temaAtual = () => (document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light');

export function aplicarTema(tema, salvar = true) {
    const raiz = document.documentElement;
    raiz.classList.add('tema-trocando');
    raiz.setAttribute('data-theme', tema);
    if (salvar) {
        try { localStorage.setItem(CHAVE, tema); } catch { /* navegação privada */ }
    }
    setTimeout(() => raiz.classList.remove('tema-trocando'), 300);
    document.querySelectorAll('[data-alternar-tema]').forEach(atualizarBotao);
}

function atualizarBotao(botao) {
    const escuro = temaAtual() === 'dark';
    botao.innerHTML = escuro ? SOL : LUA;
    botao.setAttribute('aria-label', escuro ? 'Ativar modo claro' : 'Ativar modo escuro');
    botao.setAttribute('title', escuro ? 'Modo claro' : 'Modo escuro');
    botao.setAttribute('aria-pressed', String(escuro));
}

/** HTML do botão; ligue o clique com ligarBotaoTema() depois de inserir. */
export const botaoTemaHtml = () => '<button class="gw-tema" type="button" data-alternar-tema></button>';

export function ligarBotaoTema(raiz = document) {
    raiz.querySelectorAll('[data-alternar-tema]').forEach((botao) => {
        atualizarBotao(botao);
        botao.addEventListener('click', () => aplicarTema(temaAtual() === 'dark' ? 'light' : 'dark'));
    });
}
