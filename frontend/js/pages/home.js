
import { carregarNavbar } from "../components/navbar.js";
import { carregarFooter } from "../components/footer.js";

carregarNavbar();
carregarFooter();

/*
 * Dados demonstrativos.
 * Substitua pelos dados reais da sua API quando estiver pronta.
 */
const profissionais = [
    {
        nome: "Carlos Mendes",
        categoria: "Desenvolvimento e tecnologia",
        local: "Remoto · Brasil",
        nota: "4,9",
        servicos: 38,
        imagem: "assets/images/profissional-carlos.jpg",
        selo: "TECNOLOGIA"
    },
    {
        nome: "Ana Souza",
        categoria: "Consultoria e negócios",
        local: "Remoto · Brasil",
        nota: "4,8",
        servicos: 54,
        imagem: "assets/images/profissional-ana.jpg",
        selo: "CONSULTORIA"
    },
    {
        nome: "Rafael Lima",
        categoria: "Design e criação",
        local: "Remoto · Brasil",
        nota: "4,9",
        servicos: 27,
        imagem: "assets/images/profissional-rafael.jpg",
        selo: "DESIGN"
    },
    {
        nome: "Equipe criativa",
        categoria: "Marketing e projetos",
        local: "Atendimento remoto",
        nota: "4,9",
        servicos: 42,
        imagem: "assets/images/equipe-profissionais.jpg",
        selo: "EQUIPE"
    }
];

/*
 * Cria um elemento com classe e texto.
 * O texto é inserido com textContent, não como HTML.
 */
function criarElemento(tag, classe, texto = "") {
    const elemento = document.createElement(tag);

    if (classe) {
        elemento.className = classe;
    }

    elemento.textContent = texto;

    return elemento;
}

/*
 * Monta o cartão de cada profissional.
 */
function criarCartao(profissional) {
    const cartao = criarElemento(
        "article",
        "professional-card"
    );

    const areaFoto = criarElemento(
        "div",
        "professional-photo"
    );

    const imagem = document.createElement("img");

    imagem.src = profissional.imagem;
    imagem.alt = profissional.nome;
    imagem.loading = "lazy";

    const selo = criarElemento(
        "span",
        "professional-badge",
        profissional.selo
    );

    areaFoto.append(imagem, selo);

    const corpo = criarElemento(
        "div",
        "professional-body"
    );

    const categoria = criarElemento(
        "p",
        "professional-category",
        profissional.categoria
    );

    const nome = criarElemento(
        "h3",
        "professional-name",
        profissional.nome
    );

    const local = criarElemento(
        "p",
        "professional-location",
        profissional.local
    );

    const avaliacao = criarElemento(
        "div",
        "professional-rating"
    );

    const estrela = criarElemento(
        "span",
        "rating-star",
        "★"
    );

    estrela.setAttribute("aria-hidden", "true");

    const nota = criarElemento(
        "span",
        "rating-score",
        profissional.nota
    );

    const separador = criarElemento("span", "", "·");

    const quantidade = criarElemento(
        "span",
        "",
        `${profissional.servicos} projetos`
    );

    avaliacao.append(
        estrela,
        nota,
        separador,
        quantidade
    );

    const link = document.createElement("a");

    link.className = "professional-link";
    link.href = "pages/profissionais.html";
    link.setAttribute(
        "aria-label",
        `Ver perfil de ${profissional.nome}`
    );

    const textoLink = criarElemento(
        "span",
        "",
        "↗"
    );

    const texto = document.createTextNode("Ver perfil ");

    link.append(texto, textoLink);

    corpo.append(
        categoria,
        nome,
        local,
        avaliacao,
        link
    );

    cartao.append(areaFoto, corpo);

    /*
     * Se uma imagem falhar, esconde somente a imagem
     * quebrada e preserva o restante do cartão.
     */
    imagem.addEventListener("error", () => {
        imagem.hidden = true;
        areaFoto.classList.add("professional-photo-error");
    }, { once: true });

    return cartao;
}

/*
 * Renderiza os cartões na página.
 */
function carregarProfissionais() {
    const grade = document.querySelector(
        "#professionals-grid"
    );

    if (!grade) return;

    const fragmento = document.createDocumentFragment();

    profissionais.forEach((profissional) => {
        fragmento.appendChild(
            criarCartao(profissional)
        );
    });

    grade.replaceChildren(fragmento);
}

/*
 * Inicializa os componentes e os profissionais.
 */
function iniciarHome() {
    carregarProfissionais();
}

if (document.readyState === "loading") {
    document.addEventListener(
        "DOMContentLoaded",
        iniciarHome,
        { once: true }
    );
} else {
    iniciarHome();
}
