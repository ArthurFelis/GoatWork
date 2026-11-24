// Campo de senha: botão "mostrar/ocultar" e lista de requisitos (iguais aos do back-end)

const OLHO = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/></svg>';
const OLHO_RISCADO = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3l18 18"/><path d="M10.6 5.1A10.7 10.7 0 0 1 12 5c6.4 0 10 7 10 7a17.6 17.6 0 0 1-3.2 4.1M6.6 6.6C3.8 8.4 2 12 2 12s3.6 7 10 7a9.8 9.8 0 0 0 5.4-1.6"/><path d="M9.9 9.9a3 3 0 0 0 4.2 4.2"/></svg>';

export const REGRAS_SENHA = [
    ['tamanho', 'No mínimo 8 caracteres', (s) => s.length >= 8],
    ['maiuscula', 'Uma letra maiúscula', (s) => /\p{Lu}/u.test(s)],
    ['minuscula', 'Uma letra minúscula', (s) => /\p{Ll}/u.test(s)],
    ['numero', 'Um número', (s) => /\d/.test(s)],
    ['especial', 'Um caractere especial (ex.: @ # ! $)', (s) => /[^\p{L}\d\s]/u.test(s)],
    ['espaco', 'Sem espaços', (s) => s.length > 0 && !/\s/.test(s)]
];

export const senhaValida = (s) => REGRAS_SENHA.every(([, , ok]) => ok(s));

/** Adiciona o botão de olho ao lado do campo de senha. */
export function ativarMostrarSenha(input) {
    if (!input || input.parentElement.classList.contains('senha-campo')) return;
    const caixa = document.createElement('div');
    caixa.className = 'senha-campo';
    input.parentNode.insertBefore(caixa, input);
    caixa.appendChild(input);

    const botao = document.createElement('button');
    botao.type = 'button';
    botao.className = 'senha-olho';
    botao.setAttribute('aria-label', 'Mostrar senha');
    botao.setAttribute('aria-pressed', 'false');
    botao.innerHTML = OLHO;
    caixa.appendChild(botao);

    botao.addEventListener('click', () => {
        const mostrar = input.type === 'password';
        input.type = mostrar ? 'text' : 'password';
        botao.innerHTML = mostrar ? OLHO_RISCADO : OLHO;
        botao.setAttribute('aria-label', mostrar ? 'Ocultar senha' : 'Mostrar senha');
        botao.setAttribute('aria-pressed', String(mostrar));
        input.focus();
    });
}

/** Lista de requisitos que vai ficando verde conforme a pessoa digita. */
export function ativarRequisitosSenha(input, alvo) {
    if (!input || !alvo) return;
    alvo.innerHTML = REGRAS_SENHA.map(([id, texto]) => `<li data-regra="${id}">${texto}</li>`).join('');
    const atualizar = () => {
        const s = input.value;
        REGRAS_SENHA.forEach(([id, , ok]) => alvo.querySelector(`[data-regra="${id}"]`).classList.toggle('is-ok', ok(s)));
    };
    input.addEventListener('input', atualizar);
    atualizar();
}
