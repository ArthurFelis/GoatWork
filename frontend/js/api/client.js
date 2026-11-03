// Configuração central: URL base da API e helpers de fetch com token
import { tokenAtual, limparSessao, pagina } from '../utils/storage.js';

// A API serve o front em http://localhost:5080 (mesma origem → "/api").
// Abrindo o front por outro servidor (ex.: Live Server :5500), usa a API local.
export const API_URL = window.GOATWORK_API
    ?? (location.port === '5080' ? '/api' : 'http://localhost:5080/api');

export class ApiError extends Error {
    constructor(mensagem, status) {
        super(mensagem);
        this.status = status;
    }
}

export async function api(caminho, { metodo = 'GET', corpo, auth = true } = {}) {
    const headers = { Accept: 'application/json' };
    if (corpo !== undefined) headers['Content-Type'] = 'application/json';
    const token = tokenAtual();
    if (auth && token) headers.Authorization = `Bearer ${token}`;

    let resposta;
    try {
        resposta = await fetch(`${API_URL}${caminho}`, {
            method: metodo,
            headers,
            body: corpo !== undefined ? JSON.stringify(corpo) : undefined
        });
    } catch {
        throw new ApiError('Não foi possível conectar ao servidor da GoatWork. Verifique se a API está rodando.', 0);
    }

    if (resposta.status === 204) return null;

    let dados = null;
    const texto = await resposta.text();
    if (texto) {
        try {
            dados = JSON.parse(texto);
        } catch {
            dados = { mensagem: texto };
        }
    }

    if (!resposta.ok) {
        if (resposta.status === 401 && auth && token) {
            limparSessao();
            const voltar = encodeURIComponent(location.pathname + location.search);
            location.href = `${pagina('entrar.html')}?voltar=${voltar}`;
        }
        const mensagem = dados?.mensagem
            ?? (dados?.errors ? Object.values(dados.errors).flat()[0] : null)
            ?? 'Algo deu errado. Tente novamente.';
        throw new ApiError(mensagem, resposta.status);
    }
    return dados;
}

export const get = (caminho, opcoes) => api(caminho, opcoes);
export const post = (caminho, corpo, opcoes = {}) => api(caminho, { ...opcoes, metodo: 'POST', corpo: corpo ?? {} });
export const put = (caminho, corpo, opcoes = {}) => api(caminho, { ...opcoes, metodo: 'PUT', corpo: corpo ?? {} });
export const del = (caminho, opcoes = {}) => api(caminho, { ...opcoes, metodo: 'DELETE' });
