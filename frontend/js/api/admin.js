import { get, post } from './client.js';

export const resumoAdmin = () => get('/admin/resumo');
export const listarDisputasAdmin = (status = '') => get(`/admin/disputas${status ? `?status=${status}` : ''}`);
export const detalheDisputa = (id) => get(`/admin/disputas/${id}`);
export const analisarDisputa = (id) => post(`/admin/disputas/${id}/analisar`);
export const decidirDisputa = (id, decisao, justificativa) => post(`/admin/disputas/${id}/decidir`, { decisao, justificativa });
export const listarUsuariosAdmin = (tipo = '') => get(`/admin/usuarios${tipo ? `?tipo=${tipo}` : ''}`);
export const verificarTrabalhador = (id) => post(`/admin/usuarios/${id}/verificar`);
export const alterarStatusUsuario = (id, status) => post(`/admin/usuarios/${id}/status`, { status });
export const listarContratosAdmin = () => get('/admin/contratos');
