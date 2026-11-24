// Aplica o tema (claro/escuro) ANTES de a página aparecer, para não piscar.
// Carregado como <script> comum no <head> de todas as telas.
(function () {
    var tema = null;
    try { tema = localStorage.getItem('goatwork.tema'); } catch (e) { /* sem storage */ }
    if (tema !== 'dark' && tema !== 'light') {
        tema = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    document.documentElement.setAttribute('data-theme', tema);
})();
