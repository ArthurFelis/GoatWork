
export function carregarNavbar() {
    const alvo = document.querySelector("#navbar");

    if (!alvo) return;

    alvo.innerHTML = `
        <header class="gw-navbar">

            <a class="gw-brand"
               href="index.html"
               aria-label="GoatWork — início">

                <span class="gw-brand-icon" aria-hidden="true">G</span>
                <span>GoatWork</span>
            </a>

            <nav class="gw-nav-links"
                 aria-label="Navegação principal">

                <a href="pages/profissionais.html">
                    Encontrar profissionais
                </a>

                <a href="#como-funciona">
                    Como funciona?
                </a>

                <a href="pages/seguranca.html">
                    Segurança
                </a>
            </nav>

            <div class="gw-nav-actions">
                <a class="gw-nav-login" href="pages/login.html">
                    ♙ Entrar
                </a>

                <a class="gw-nav-cta"
                   href="pages/cadastro-profissional.html">
                    Seja um profissional
                </a>
            </div>

            <button
                class="gw-menu-toggle"
                type="button"
                aria-label="Abrir menu"
                aria-expanded="false">

                ☰
            </button>

        </header>
    `;

    const toggle = alvo.querySelector(".gw-menu-toggle");
    const nav = alvo.querySelector(".gw-nav-links");

    toggle?.addEventListener("click", () => {
        const aberto = nav.classList.toggle("is-open");

        toggle.setAttribute("aria-expanded", String(aberto));

        toggle.setAttribute(
            "aria-label",
            aberto ? "Fechar menu" : "Abrir menu"
        );

        toggle.textContent = aberto ? "×" : "☰";
    });
}
