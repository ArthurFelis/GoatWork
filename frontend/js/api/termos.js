import { get, post } from './client.js';

export const termoAtual = () => get('/termos/atual', { auth: false });
export const listarTermos = () => get('/termos');
export const publicarTermo = (dados) => post('/termos', dados);
