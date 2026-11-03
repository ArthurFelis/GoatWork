// Avisos rápidos no canto da tela
export function toast(mensagem, tipo = 'ok', ms = 4200) {
    let pilha = document.querySelector('.gw-toasts');
    if (!pilha) {
        pilha = document.createElement('div');
        pilha.className = 'gw-toasts';
        pilha.setAttribute('role', 'status');
        pilha.setAttribute('aria-live', 'polite');
        document.body.appendChild(pilha);
    }
    const item = document.createElement('div');
    item.className = `gw-toast gw-toast--${tipo}`;
    item.textContent = mensagem;
    pilha.appendChild(item);
    setTimeout(() => item.remove(), ms);
}

export const erro = (e) => {
    if (e?.message === 'redirecionando') return;
    toast(e?.message || String(e), 'erro', 6000);
};

/** Mostra spinner no botão enquanto a promessa roda. */
export async function comCarregamento(botao, acao) {
    if (botao) {
        botao.disabled = true;
        botao.classList.add('is-loading');
    }
    try {
        return await acao();
    } finally {
        if (botao) {
            botao.disabled = false;
            botao.classList.remove('is-loading');
        }
    }
}
