import { get, post } from './client.js';

export const obterPagamento = (idContrato) => get(`/pagamentos/contrato/${idContrato}`);
export const pagar = (idContrato, dados) => post(`/pagamentos/contrato/${idContrato}`, dados);
