import { get, post } from './client.js';

export const obterDisputa = (idContrato) => get(`/disputas/contrato/${idContrato}`);
export const abrirDisputa = (idContrato, motivo, entendo) => post(`/disputas/contrato/${idContrato}`, { motivo, entendo });
