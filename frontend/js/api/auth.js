import { post, get } from './client.js';
import { salvarSessao, limparSessao } from '../utils/storage.js';

export async function entrar(dados) {
    const resposta = await post('/auth/login', dados, { auth: false });
    salvarSessao(resposta);
    return resposta.usuario;
}

export async function cadastrar(dados) {
    const resposta = await post('/auth/cadastro', dados, { auth: false });
    salvarSessao(resposta);
    return resposta.usuario;
}

export const eu = () => get('/auth/me');

export function sair() {
    limparSessao();
}
