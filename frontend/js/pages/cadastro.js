import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { carregarPainelTermos } from '../components/termos-painel.js';
import { cadastrar } from '../api/auth.js';
import { inicioDoPerfil, param, usuarioAtual } from '../utils/storage.js';
import { ativarMostrarSenha, ativarRequisitosSenha, senhaValida } from '../components/senha.js';
import { aplicarMascara, cnpjValido, cpfValido, emailValido, mascaraCnpj, mascaraCpf, mascaraTelefone } from '../utils/validacoes.js';

carregarNavbar();
carregarFooter();
carregarPainelTermos(document.querySelector('#termos-painel'));

const logado = usuarioAtual();
if (logado) location.replace(inicioDoPerfil(logado.tipo));

const $ = (s) => document.querySelector(s);
const botao = $('#btn-cadastrar');
const aceite = $('#aceite');
const erro = $('#erro');
let tipo = param('tipo') === 'TRABALHADOR' ? 'TRABALHADOR' : 'CLIENTE';
let tipoPessoa = 'FISICA';

// CPF ou CNPJ, conforme o tipo de pessoa (só cliente pode ser pessoa jurídica)
const usaCnpj = () => tipoPessoa === 'JURIDICA';
$('#cpf').addEventListener('input', () => { $('#cpf').value = usaCnpj() ? mascaraCnpj($('#cpf').value) : mascaraCpf($('#cpf').value); });

function atualizarDocumento() {
    const cnpj = usaCnpj();
    const campo = $('#cpf');
    const era = campo.dataset.doc;
    campo.dataset.doc = cnpj ? 'cnpj' : 'cpf';
    $('#rotulo-documento').textContent = cnpj ? 'CNPJ' : 'CPF';
    $('#rotulo-nome').textContent = cnpj ? 'Razão social' : 'Nome completo';
    $('#nome').placeholder = cnpj ? 'Nome da empresa' : 'Seu nome';
    $('#rotulo-exibicao').textContent = cnpj ? 'Nome fantasia' : 'Nome de exibição';
    campo.placeholder = cnpj ? '00.000.000/0000-00' : '000.000.000-00';
    if (era && era !== campo.dataset.doc) campo.value = '';
    campo.classList.remove('is-invalid');
}
aplicarMascara($('#telefone'), mascaraTelefone);
ativarMostrarSenha($('#senha'));
ativarMostrarSenha($('#confirmar'));
ativarRequisitosSenha($('#senha'), $('#requisitos-senha'));

function atualizarTipo() {
    document.querySelectorAll('#tipo-conta button').forEach((b) => b.setAttribute('aria-pressed', String(b.dataset.tipo === tipo)));
    document.querySelectorAll('[data-somente]').forEach((el) => { el.hidden = el.dataset.somente !== tipo; });
    botao.textContent = tipo === 'CLIENTE' ? 'Criar conta de cliente' : 'Criar conta de trabalhador';
    $('#link-entrar').href = `entrar.html?tipo=${tipo}`;
    atualizarDocumento();
}

document.querySelectorAll('#tipo-conta button').forEach((b) => b.addEventListener('click', () => {
    tipo = b.dataset.tipo;
    atualizarTipo();
}));

document.querySelectorAll('#tipo-pessoa button').forEach((b) => b.addEventListener('click', () => {
    tipoPessoa = b.dataset.valor;
    document.querySelectorAll('#tipo-pessoa button').forEach((x) => x.setAttribute('aria-pressed', String(x === b)));
    atualizarDocumento();
}));

aceite.addEventListener('change', () => { botao.disabled = !aceite.checked; });

function mostrarErro(msg, campo) {
    erro.textContent = msg;
    erro.hidden = !msg;
    document.querySelectorAll('.is-invalid').forEach((el) => el.classList.remove('is-invalid'));
    if (campo) {
        $(campo).classList.add('is-invalid');
        if (campo === '#senha') document.querySelector('#requisitos-senha').classList.add('is-destaque');
        $(campo).focus();
    }
    if (msg) erro.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
}

$('#form-cadastro').addEventListener('submit', async (e) => {
    e.preventDefault();
    const dados = {
        tipo,
        nome: $('#nome').value.trim(),
        email: $('#email').value.trim(),
        cpf: $('#cpf').value,
        telefone: $('#telefone').value.trim() || null,
        tipoPessoa,
        nomeExibicao: tipo === 'CLIENTE' ? ($('#nome-exibicao').value.trim() || null) : null,
        profissao: tipo === 'TRABALHADOR' ? ($('#profissao').value.trim() || null) : null,
        cidade: tipo === 'TRABALHADOR' ? ($('#cidade').value.trim() || null) : null,
        senha: $('#senha').value,
        aceitouTermos: aceite.checked
    };

    if (dados.nome.length < 3) return mostrarErro('Informe seu nome completo.', '#nome');
    if (!emailValido(dados.email)) return mostrarErro('Informe um e-mail válido.', '#email');
    if (usaCnpj() ? !cnpjValido(dados.cpf) : !cpfValido(dados.cpf)) return mostrarErro(usaCnpj() ? 'Informe um CNPJ válido.' : 'Informe um CPF válido.', '#cpf');
    if (tipo === 'TRABALHADOR' && !dados.profissao) return mostrarErro('Informe sua profissão.', '#profissao');
    if (!senhaValida(dados.senha)) return mostrarErro('A senha não atende a todos os requisitos listados abaixo do campo.', '#senha');
    if (dados.senha !== $('#confirmar').value) return mostrarErro('As senhas não conferem.', '#confirmar');
    if (!dados.aceitouTermos) return mostrarErro('É preciso aceitar os termos de uso.');
    mostrarErro('');

    botao.disabled = true;
    botao.classList.add('is-loading');
    try {
        const usuario = await cadastrar(dados);
        // Trabalhador novo completa o perfil (categorias de atuação) antes de receber convites
        location.href = usuario.tipo === 'TRABALHADOR' ? 'perfil-trabalhador.html?novo=1' : (param('voltar') || 'novo-pedido.html');
    } catch (err) {
        mostrarErro(err.message);
        botao.disabled = false;
        botao.classList.remove('is-loading');
    }
});

atualizarTipo();
