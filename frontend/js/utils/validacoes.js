export const soDigitos = (v = '') => String(v).replace(/\D/g, '');

export const emailValido = (email = '') => /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email.trim());

export function cpfValido(cpf = '') {
    const d = soDigitos(cpf);
    if (d.length !== 11 || /^(\d)\1+$/.test(d)) return false;
    const digito = (tam) => {
        let soma = 0;
        for (let i = 0; i < tam; i++) soma += Number(d[i]) * (tam + 1 - i);
        const resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    };
    return digito(9) === Number(d[9]) && digito(10) === Number(d[10]);
}

export function cnpjValido(cnpj = '') {
    const d = soDigitos(cnpj);
    if (d.length !== 14 || /^(\d)\1+$/.test(d)) return false;
    const digito = (tam) => {
        const pesos = tam === 12 ? [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2] : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        const soma = pesos.reduce((acc, p, i) => acc + Number(d[i]) * p, 0);
        const resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    };
    return digito(12) === Number(d[12]) && digito(13) === Number(d[13]);
}

export function mascaraCnpj(v) {
    const d = soDigitos(v).slice(0, 14);
    return d
        .replace(/^(\d{2})(\d)/, '$1.$2')
        .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
        .replace(/\.(\d{3})(\d)/, '.$1/$2')
        .replace(/(\d{4})(\d{1,2})$/, '$1-$2');
}

export function mascaraCpf(v) {
    const d = soDigitos(v).slice(0, 11);
    return d
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d{1,2})$/, '$1-$2');
}

export function mascaraTelefone(v) {
    const d = soDigitos(v).slice(0, 11);
    if (d.length <= 2) return d ? `(${d}` : '';
    if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
    if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
    return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

export function mascaraMoeda(v) {
    const d = soDigitos(v);
    if (!d) return '';
    const n = Number(d) / 100;
    return 'R$ ' + n.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function mascaraData(v) {
    const d = soDigitos(v).slice(0, 8);
    return d.replace(/(\d{2})(\d)/, '$1/$2').replace(/(\d{2})(\d)/, '$1/$2');
}

export function mascaraCartao(v) {
    return soDigitos(v).slice(0, 19).replace(/(\d{4})(?=\d)/g, '$1 ');
}

export function mascaraValidade(v) {
    const d = soDigitos(v).slice(0, 4);
    return d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` : d;
}

/** Liga uma máscara a um <input>. */
export function aplicarMascara(input, fn) {
    if (!input) return;
    input.addEventListener('input', () => {
        input.value = fn(input.value);
    });
}
