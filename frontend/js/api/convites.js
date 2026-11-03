import { get, post } from './client.js';

export const listarConvites = (status = '') => get(`/convites${status ? `?status=${status}` : ''}`);
export const aceitarConvite = (id) => post(`/convites/${id}/aceitar`);
export const recusarConvite = (id) => post(`/convites/${id}/recusar`);
