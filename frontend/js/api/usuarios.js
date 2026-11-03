import { get, put } from './client.js';

export const obterEu = () => get('/usuarios/me');
export const atualizarEu = (dados) => put('/usuarios/me', dados);
