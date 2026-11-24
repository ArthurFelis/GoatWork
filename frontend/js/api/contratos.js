import { get, post } from './client.js';

export const listarContratos = () => get('/contratos');
export const obterContrato = (id) => get(`/contratos/${id}`);
/** "Enviar orçamento" (trabalhador): gera o contrato já assinado por ele. */
export const enviarOrcamento = (idChat, { valor, prazo, hora, local }) => post('/contratos', { idChat, valor, prazo, hora, local });
export const assinarContrato = (id) => post(`/contratos/${id}/assinar`);
export const recusarContrato = (id) => post(`/contratos/${id}/recusar`);
