import { get, post } from './client.js';

export const listarChats = () => get('/chats');
export const obterChat = (id) => get(`/chats/${id}`);
export const enviarMensagem = (id, conteudo) => post(`/chats/${id}/mensagens`, { conteudo });
