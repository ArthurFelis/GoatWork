import { get, post } from './client.js';

export const obterCarteira = () => get('/carteira');
export const sacar = (valor, tipoChave, chave) => post('/carteira/saques', { valor, tipoChave, chave });
