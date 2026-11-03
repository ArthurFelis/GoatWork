import { get, post } from './client.js';

export const obterAvaliacao = (idContrato) => get(`/avaliacoes/contrato/${idContrato}`);
export const avaliar = (idContrato, nota, comentario) => post(`/avaliacoes/contrato/${idContrato}`, { nota, comentario });
