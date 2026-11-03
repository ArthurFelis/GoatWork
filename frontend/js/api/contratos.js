import { get, post } from './client.js';

export const listarContratos = () => get('/contratos');
export const obterContrato = (id) => get(`/contratos/${id}`);
export const gerarContrato = (idChat, valor, prazo) => post('/contratos', { idChat, valor, prazo });
export const assinarContrato = (id) => post(`/contratos/${id}/assinar`);
