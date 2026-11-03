// Sessão do usuário (token JWT + resumo do usuário) guardada no navegador
const CHAVE = 'goatwork.sessao';

export function obterSessao() {
    try {
        const bruto = localStorage.getItem(CHAVE);
        return bruto ? JSON.parse(bruto) : null;
    } catch {
        return null;
    }
}

export function salvarSessao(sessao) {
    try {
        localStorage.setItem(CHAVE, JSON.stringify(sessao));
    } catch {
        /* navegação privada sem storage: segue só nesta página */
    }
}

export function limparSessao() {
    try {
        localStorage.removeItem(CHAVE);
    } catch {
        /* ignore */
    }
}

export const usuarioAtual = () => obterSessao()?.usuario ?? null;
export const tokenAtual = () => obterSessao()?.token ?? null;

/** Caminho até a raiz do front (index.html fica na raiz; as telas em /pages). */
export const RAIZ = location.pathname.includes('/pages/') ? '../' : '';
export const pagina = (nome) => `${RAIZ}pages/${nome}`;

/** Página inicial de cada perfil depois do login. */
export function inicioDoPerfil(tipo) {
    if (tipo === 'TRABALHADOR') return pagina('convites.html');
    if (tipo === 'ADMIN') return pagina('admin.html');
    return pagina('contratos.html');
}

/**
 * Garante que há alguém logado (e do tipo certo). Sem sessão → tela Entrar,
 * voltando para cá depois do login.
 */
export function exigirUsuario(tipos = null) {
    const usuario = usuarioAtual();
    if (!usuario) {
        const voltar = encodeURIComponent(location.pathname + location.search);
        location.replace(`${pagina('entrar.html')}?voltar=${voltar}`);
        throw new Error('redirecionando');
    }
    if (tipos && !tipos.includes(usuario.tipo)) {
        location.replace(inicioDoPerfil(usuario.tipo));
        throw new Error('redirecionando');
    }
    return usuario;
}

export const param = (nome) => new URLSearchParams(location.search).get(nome);
