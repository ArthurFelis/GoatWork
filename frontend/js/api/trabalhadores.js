import { get, put } from './client.js';

export function listarTrabalhadores({ busca = '', categoria = '' } = {}) {
    const qs = new URLSearchParams();
    if (busca) qs.set('busca', busca);
    if (categoria) qs.set('categoria', categoria);
    const sufixo = qs.toString() ? `?${qs}` : '';
    return get(`/trabalhadores${sufixo}`, { auth: false });
}

export const destaques = () => get('/trabalhadores/destaques', { auth: false });
export const obterTrabalhador = (id) => get(`/trabalhadores/${id}`, { auth: false });
export const meuPerfil = () => get('/trabalhadores/me');
export const salvarMeuPerfil = (dados) => put('/trabalhadores/me', dados);
