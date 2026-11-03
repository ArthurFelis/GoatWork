import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import { aceitarConvite, listarConvites, recusarConvite } from '../api/convites.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, moeda, quando } from '../utils/formatadores.js';

exigirUsuario(['TRABALHADOR']);
carregarNavbar('convites');
carregarFooter();

const lista = document.querySelector('#lista');
const abas = document.querySelector('#abas');
let status = ['ENVIADO', 'ACEITO', 'RECUSADO'].includes(param('status')) ? param('status') : 'ENVIADO';
const CHECK = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>';

function card(c) {
    if (c.status === 'RECUSADO') {
        return `
            <article class="cv-recusado">
                <div><h2>${esc(c.titulo)}</h2><span>${esc(c.cliente)} · enviado ${quando(c.dataEnvio)}</span></div>
                <span class="badge badge--outline">Convite recusado</span>
            </article>`;
    }
    const acoes = c.status === 'ACEITO'
        ? `<div class="cv-acoes cv-acoes--entre">
               <span class="badge badge--blue">${CHECK} Convite aceito</span>
               <a class="btn btn-primary" href="chat.html?chat=${c.idChat}">Abrir chat</a>
           </div>`
        : `<div class="cv-acoes">
               <button class="btn btn-outline" type="button" data-recusar="${c.id}">Recusar</button>
               <button class="btn btn-primary" type="button" data-aceitar="${c.id}">Aceitar e abrir chat</button>
           </div>`;
    return `
        <article class="cv-card">
            <div class="cv-topo">
                <div class="cv-quem">
                    <span class="avatar avatar--sm" style="width:40px;height:40px">${esc(c.iniciaisCliente)}</span>
                    <div><h2>${esc(c.titulo)}</h2><span>${esc(c.cliente)} · enviado ${quando(c.dataEnvio)}</span></div>
                </div>
                <span class="badge badge--peach">${esc(c.categoria)}</span>
            </div>
            <p class="cv-resumo">${esc(c.resumo)}</p>
            <dl class="info-strip">
                <div><dt>Local</dt><dd>${esc(c.local ?? 'A combinar')}</dd></div>
                <div><dt>Prazo desejado</dt><dd>${c.prazo ? data(c.prazo) : 'A combinar'}</dd></div>
                <div><dt>Orçamento estimado</dt><dd>${c.orcamento != null ? moeda(c.orcamento) : 'A combinar'}</dd></div>
            </dl>
            ${acoes}
        </article>`;
}

const VAZIO = {
    ENVIADO: '<strong>Nenhum convite pendente</strong>Mantenha suas categorias de atuação atualizadas em <a class="link" href="perfil-trabalhador.html">Meu perfil</a> para receber pedidos compatíveis.',
    ACEITO: '<strong>Nenhum convite aceito ainda</strong>Os convites aceitos abrem um chat com o cliente.',
    RECUSADO: '<strong>Nenhum convite recusado</strong>'
};

async function carregar() {
    abas.querySelectorAll('.chip').forEach((b) => b.setAttribute('aria-pressed', String(b.dataset.status === status)));
    lista.innerHTML = '<div class="loading">Carregando convites…</div>';
    try {
        const convites = await listarConvites(status);
        lista.innerHTML = convites.length ? convites.map(card).join('') : `<div class="empty">${VAZIO[status]}</div>`;
    } catch (e) {
        lista.innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

abas.addEventListener('click', (e) => {
    const aba = e.target.closest('.chip');
    if (!aba) return;
    status = aba.dataset.status;
    history.replaceState(null, '', `?status=${status}`);
    carregar();
});

lista.addEventListener('click', (e) => {
    const aceitar = e.target.closest('[data-aceitar]');
    const recusar = e.target.closest('[data-recusar]');
    if (aceitar) {
        comCarregamento(aceitar, async () => {
            try {
                const { idChat } = await aceitarConvite(aceitar.dataset.aceitar);
                location.href = `chat.html?chat=${idChat}`;
            } catch (err) {
                toastErro(err);
            }
        });
    } else if (recusar) {
        comCarregamento(recusar, async () => {
            try {
                await recusarConvite(recusar.dataset.recusar);
                toast('Convite recusado.');
                carregar();
            } catch (err) {
                toastErro(err);
            }
        });
    }
});

carregar();
