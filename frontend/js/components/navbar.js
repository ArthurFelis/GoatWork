import { RAIZ, pagina, usuarioAtual, limparSessao } from '../utils/storage.js';
import { esc } from '../utils/formatadores.js';

// Menus de cada perfil, exatamente como nas telas do Figma
const MENUS = {
    VISITANTE: [
        ['profissionais', 'Encontrar profissionais', 'profissionais.html'],
        ['mensagens', 'Mensagens', 'chat.html'],
        ['contratos', 'Contratos', 'contratos.html']
    ],
    CLIENTE: [
        ['profissionais', 'Encontrar profissionais', 'profissionais.html'],
        ['mensagens', 'Mensagens', 'chat.html'],
        ['contratos', 'Contratos', 'contratos.html']
    ],
    TRABALHADOR: [
        ['convites', 'Convites', 'convites.html'],
        ['mensagens', 'Mensagens', 'chat.html'],
        ['contratos', 'Contratos', 'contratos.html'],
        ['perfil', 'Meu perfil', 'perfil-trabalhador.html']
    ],
    ADMIN: [
        ['disputas', 'Disputas', 'admin.html'],
        ['admin-contratos', 'Contratos', 'admin.html?aba=contratos'],
        ['usuarios', 'Usuários', 'admin.html?aba=usuarios'],
        ['admin-termos', 'Termos de uso', 'admin.html?aba=termos']
    ]
};

/**
 * @param {string} ativo chave do item de menu destacado (ex.: "mensagens")
 */
export function carregarNavbar(ativo = '') {
    const alvo = document.querySelector('#navbar');
    if (!alvo) return;

    const usuario = usuarioAtual();
    const menu = MENUS[usuario?.tipo ?? 'VISITANTE'] ?? MENUS.VISITANTE;

    const links = menu.map(([chave, texto, arquivo]) => `
        <a href="${pagina(arquivo)}" ${chave === ativo ? 'aria-current="page"' : ''}>${texto}</a>`).join('');

    const acoes = usuario
        ? `<div class="gw-user">
               <button class="gw-user-btn" type="button" aria-haspopup="true" aria-expanded="false">
                   <span class="gw-avatar">${esc(usuario.iniciais)}</span>
                   <span class="gw-user-name">${esc(usuario.nome)}</span>
               </button>
               <div class="gw-user-menu" hidden>
                   <div class="gw-user-email">${esc(usuario.email)}</div>
                   ${usuario.tipo === 'CLIENTE' ? `<a href="${pagina('novo-pedido.html')}">+ Novo pedido</a>` : ''}
                   ${usuario.tipo === 'TRABALHADOR' ? `<a href="${pagina('perfil-trabalhador.html')}">Meu perfil</a>` : ''}
                   <a href="${pagina('termos.html')}">Termos de uso</a>
                   <button type="button" data-sair>Sair</button>
               </div>
           </div>`
        : `<a class="gw-nav-login" href="${pagina('entrar.html')}">Entrar</a>`;

    alvo.innerHTML = `
        <header class="gw-navbar">
            <a class="gw-brand" href="${RAIZ}index.html" aria-label="GoatWork — início">
                <span class="gw-brand-icon" aria-hidden="true">G</span>
                <span>Goat<em>Work</em></span>
            </a>
            <nav class="gw-nav-links" aria-label="Navegação principal">${links}</nav>
            <div class="gw-nav-actions">${acoes}</div>
            <button class="gw-menu-toggle" type="button" aria-label="Abrir menu" aria-expanded="false">☰</button>
        </header>
    `;

    const toggle = alvo.querySelector('.gw-menu-toggle');
    const nav = alvo.querySelector('.gw-nav-links');
    toggle.addEventListener('click', () => {
        const aberto = nav.classList.toggle('is-open');
        toggle.setAttribute('aria-expanded', String(aberto));
        toggle.textContent = aberto ? '×' : '☰';
    });

    const botaoUsuario = alvo.querySelector('.gw-user-btn');
    const menuUsuario = alvo.querySelector('.gw-user-menu');
    if (botaoUsuario) {
        botaoUsuario.addEventListener('click', (e) => {
            e.stopPropagation();
            menuUsuario.hidden = !menuUsuario.hidden;
            botaoUsuario.setAttribute('aria-expanded', String(!menuUsuario.hidden));
        });
        document.addEventListener('click', () => { menuUsuario.hidden = true; });
        alvo.querySelector('[data-sair]').addEventListener('click', () => {
            limparSessao();
            location.href = `${RAIZ}index.html`;
        });
    }
}
