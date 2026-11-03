import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { destaques } from '../api/trabalhadores.js';
import { esc, nota } from '../utils/formatadores.js';
import { usuarioAtual } from '../utils/storage.js';

carregarNavbar();
carregarFooter();

// Trabalhador logado não cria pedidos: o botão leva para os convites
const usuario = usuarioAtual();
if (usuario?.tipo === 'TRABALHADOR') {
    const btn = document.querySelector('#btn-comecar');
    btn.textContent = 'Ver convites';
    btn.href = 'pages/convites.html';
}

async function carregarDestaques() {
    const alvo = document.querySelector('#destaques');
    try {
        const lista = await destaques();
        if (!lista.length) {
            alvo.innerHTML = '<div class="empty" style="grid-column:1/-1">Em breve, profissionais avaliados por aqui.</div>';
            return;
        }
        alvo.innerHTML = lista.map((p) => `
            <a class="prof-card" href="pages/profissionais.html?destaque=${p.id}">
                <span class="avatar">${esc(p.iniciais)}</span>
                <strong>${esc(p.nome)}</strong>
                <span>${esc([p.profissao, p.cidade].filter(Boolean).join(' · '))}</span>
                <span class="prof-nota">★ ${nota(p.nota)} · ${p.servicos} serviços</span>
            </a>`).join('');
    } catch (e) {
        alvo.innerHTML = `<div class="empty" style="grid-column:1/-1">${esc(e.message)}</div>`;
    }
}

carregarDestaques();
