import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { carregarPainelTermos } from '../components/termos-painel.js';
import { entrar } from '../api/auth.js';
import { inicioDoPerfil, param, usuarioAtual } from '../utils/storage.js';
import { emailValido } from '../utils/validacoes.js';

carregarNavbar();
carregarFooter();
carregarPainelTermos(document.querySelector('#termos-painel'));

// Já logado: vai direto para a área do perfil
const logado = usuarioAtual();
if (logado) location.replace(param('voltar') || inicioDoPerfil(logado.tipo));

const form = document.querySelector('#form-entrar');
const botao = document.querySelector('#btn-entrar');
const aceite = document.querySelector('#aceite');
const erro = document.querySelector('#erro');
let tipo = param('tipo') === 'TRABALHADOR' ? 'TRABALHADOR' : 'CLIENTE';

function atualizarTipo() {
    document.querySelectorAll('#tipo-conta button').forEach((b) => {
        b.setAttribute('aria-pressed', String(b.dataset.tipo === tipo));
    });
    botao.textContent = tipo === 'CLIENTE' ? 'Entrar como cliente' : 'Entrar como trabalhador';
    document.querySelector('#link-cadastro').href = `cadastro.html?tipo=${tipo}`;
}

document.querySelectorAll('#tipo-conta button').forEach((b) => {
    b.addEventListener('click', () => {
        tipo = b.dataset.tipo;
        atualizarTipo();
    });
});

// O botão só habilita depois do aceite dos termos (regra 1)
aceite.addEventListener('change', () => {
    botao.disabled = !aceite.checked;
});

function mostrarErro(msg) {
    erro.textContent = msg;
    erro.hidden = !msg;
}

form.addEventListener('submit', async (e) => {
    e.preventDefault();
    mostrarErro('');
    const email = form.email.value.trim();
    const senha = form.senha.value;
    if (!emailValido(email)) return mostrarErro('Informe um e-mail válido.');
    if (!senha) return mostrarErro('Informe sua senha.');
    if (!aceite.checked) return mostrarErro('É preciso aceitar os termos de uso.');

    botao.disabled = true;
    botao.classList.add('is-loading');
    try {
        const usuario = await entrar({ tipo, email, senha, aceitouTermos: true });
        location.href = param('voltar') || inicioDoPerfil(usuario.tipo);
    } catch (err) {
        mostrarErro(err.message);
        botao.disabled = false;
        botao.classList.remove('is-loading');
    }
});

atualizarTipo();
