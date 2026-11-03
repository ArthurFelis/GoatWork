import { get, post, put, del } from './client.js';

export const listarPedidos = () => get('/pedidos');
export const obterPedido = (id) => get(`/pedidos/${id}`);
export const criarPedido = (dados) => post('/pedidos', dados);
export const atualizarPedido = (id, dados) => put(`/pedidos/${id}`, dados);
export const cancelarPedido = (id) => del(`/pedidos/${id}`);
