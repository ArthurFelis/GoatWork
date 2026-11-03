import { get } from './client.js';

let cache = null;
export const listarCategorias = () => (cache ??= get('/categorias', { auth: false }));
