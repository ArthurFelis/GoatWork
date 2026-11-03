import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { termoAtual } from '../api/termos.js';
import { esc } from '../utils/formatadores.js';

carregarNavbar();
carregarFooter();

async function carregar() {
    const itens = document.querySelector('#itens');
    try {
        const termo = await termoAtual();
        document.querySelector('#versao').textContent = `Versão ${termo.versao} · Válidos para clientes e trabalhadores.`;
        itens.innerHTML = termo.itens.map((item, i) => `
            <li class="${item.destaque ? 'is-destaque' : ''}">
                <span class="num-dot">${i + 1}</span>
                <span>${esc(item.texto)}</span>
            </li>`).join('');
    } catch (e) {
        itens.innerHTML = `<li class="empty">${esc(e.message)}</li>`;
    }
}

carregar();
