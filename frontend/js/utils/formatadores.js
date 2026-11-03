const moedaFmt = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

/** 650 → "R$ 650,00" */
export const moeda = (valor) => moedaFmt.format(Number(valor ?? 0)).replace(/ /g, ' ');

/** "R$ 650,00" ou "650,00" → 650 */
export function lerMoeda(texto) {
    if (texto === null || texto === undefined) return null;
    const limpo = String(texto).replace(/[^\d,.-]/g, '').replace(/\./g, '').replace(',', '.');
    if (!limpo) return null;
    const n = Number(limpo);
    return Number.isFinite(n) ? n : null;
}

/** Valor de referência do profissional: 90, "h" → "R$ 90/h" */
export function valorReferencia(valor, unidade) {
    if (valor === null || valor === undefined) return '';
    const inteiro = Number(valor) % 1 === 0;
    const txt = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: inteiro ? 0 : 2, maximumFractionDigits: 2 }).format(valor);
    return `R$ ${txt}/${unidade || 'h'}`;
}

/** "2026-10-10" (DateOnly) → Date local, sem fuso */
export function lerData(iso) {
    if (!iso) return null;
    if (/^\d{4}-\d{2}-\d{2}$/.test(iso)) {
        const [a, m, d] = iso.split('-').map(Number);
        return new Date(a, m - 1, d);
    }
    return new Date(iso);
}

/** → "10/10/2026" */
export function data(iso) {
    const d = lerData(iso);
    return d ? d.toLocaleDateString('pt-BR') : '';
}

/** → "10/10/2026 às 17:05" */
export function dataHora(iso) {
    const d = lerData(iso);
    if (!d) return '';
    return `${d.toLocaleDateString('pt-BR')} às ${d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`;
}

/** → "hoje às 10:42", "ontem às 16:10", "há 3 dias" */
export function quando(iso) {
    const d = lerData(iso);
    if (!d) return '';
    const hora = d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
    const hoje = new Date();
    const inicio = (x) => new Date(x.getFullYear(), x.getMonth(), x.getDate()).getTime();
    const dias = Math.round((inicio(hoje) - inicio(d)) / 86400000);
    if (dias <= 0) return `hoje às ${hora}`;
    if (dias === 1) return `ontem às ${hora}`;
    if (dias < 30) return `há ${dias} dias`;
    return d.toLocaleDateString('pt-BR');
}

/** Date → "2026-10-10" para <input type="date"> e para a API */
export function isoData(d) {
    if (!d) return '';
    const p = (n) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

/** "10/10/2026" → "2026-10-10" */
export function dataBrParaIso(texto) {
    const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec((texto || '').trim());
    if (!m) return null;
    const d = new Date(Number(m[3]), Number(m[2]) - 1, Number(m[1]));
    return d.getDate() === Number(m[1]) && d.getMonth() === Number(m[2]) - 1 ? isoData(d) : null;
}

export const nota = (n) => Number(n ?? 0).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

export function iniciais(nome = '') {
    const partes = nome.trim().split(/\s+/).filter(Boolean);
    if (!partes.length) return '?';
    if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase();
    return (partes[0][0] + partes.at(-1)[0]).toUpperCase();
}

/** Escapa texto vindo da API antes de inserir em HTML. */
export function esc(valor) {
    return String(valor ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
