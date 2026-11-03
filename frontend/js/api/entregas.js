import { get, post } from './client.js';

export const obterEntregas = (idContrato) => get(`/entregas/contrato/${idContrato}`);
export const enviarEntrega = (idContrato, descricao) => post(`/entregas/contrato/${idContrato}`, { descricao });
export const aprovarEntrega = (id) => post(`/entregas/${id}/aprovar`);
export const rejeitarEntrega = (id) => post(`/entregas/${id}/rejeitar`);
