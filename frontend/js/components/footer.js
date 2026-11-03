import { RAIZ, pagina } from '../utils/storage.js';

export function carregarFooter() {
    const alvo = document.querySelector('#footer');
    if (!alvo) return;

    alvo.innerHTML = `
        <footer class="gw-footer">
            <div class="gw-footer-inner">
                <a class="gw-footer-brand" href="${RAIZ}index.html">GoatWork</a>
                <p class="gw-footer-copy">Pagamento protegido · Contrato digital · Atendimento por IA</p>
                <nav class="gw-footer-links" aria-label="Links do rodapé">
                    <a href="${pagina('termos.html')}">Termos de uso</a>
                </nav>
            </div>
        </footer>
    `;
}
