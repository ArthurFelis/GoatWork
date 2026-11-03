import { get, post, put } from './client.js';

export const conversaIa = (idPedido) => get(`/briefings/pedido/${idPedido}/conversa`);
export const enviarMensagemIa = (idPedido, conteudo) => post(`/briefings/pedido/${idPedido}/mensagens`, { conteudo });
export const obterBriefing = (idPedido) => get(`/briefings/pedido/${idPedido}`);
export const confirmarBriefing = (idPedido, dados) => put(`/briefings/pedido/${idPedido}/confirmar`, dados);
