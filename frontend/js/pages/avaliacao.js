import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import { avaliar, obterAvaliacao } from '../api/avaliacoes.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { data, esc, moeda } from '../utils/formatadores.js';

exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('contratos');
carregarFooter();

const $ = (s) => document.querySelector(s);
const idContrato = Number(param('contrato'));
const LEGENDA = ['', 'Ruim', 'Regular', 'Bom', 'Muito bom', 'Excelente'];
const PAGAMENTO = { LIBERADO: 'Liberado', EM_CUSTODIA: 'Em custódia', REEMBOLSADO: 'Estornado', PENDENTE: 'Pendente', FALHOU: 'Falhou' };
const ESTRELA = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" aria-hidden="true"><path d="m12 2.8 2.85 5.78 6.38.93-4.62 4.5 1.09 6.35L12 17.36l-5.7 3 1.09-6.35-4.62-4.5 6.38-.93z"/></svg>';
let nota = 0;

function pintarEstrelas() {
    document.querySelectorAll('.estrela').forEach((b, i) => {
        b.classList.toggle('is-on', i < nota);
        b.setAttribute('aria-checked', String(i + 1 === nota));
    });
    $('#legenda').textContent = nota ? `${nota} de 5 · ${LEGENDA[nota]}` : 'Escolha de 1 a 5 estrelas';
}

function render(c) {
    const concluido = c.statusContrato === 'CONCLUIDO';
    $('#numero').textContent = `Contrato Nº ${c.numeroContrato}${concluido ? ' · Concluído' : ''}`;
    if (c.meuPapel === 'TRABALHADOR') {
        $('#titulo').textContent = 'Avalie o cliente';
        $('#subtitulo').textContent = 'Sua avaliação ajuda outros profissionais a conhecer o cliente.';
        $('#nota-lateral').textContent = 'Cada pessoa avalia a outra parte uma única vez por contrato concluído. O cliente também avalia você.';
    }
    $('#resumo').textContent = c.resumo;
    $('#valor').textContent = moeda(c.valor);
    $('#pagamento').textContent = PAGAMENTO[c.statusPagamento] ?? c.statusPagamento;
    $('#concluido').textContent = c.concluidoEm ? data(c.concluidoEm) : '—';

    const quem = `
        <div class="av-quem">
            <span class="avatar avatar--lg">${esc(c.iniciaisAvaliado)}</span>
            <div><strong>${esc(c.avaliado)}</strong><span>${esc(c.subtituloAvaliado)}</span></div>
        </div>`;

    if (c.jaAvaliado) {
        nota = c.minhaNota;
        $('#principal').innerHTML = `${quem}
            <span class="label">Sua nota</span>
            <div class="estrelas">${[1, 2, 3, 4, 5].map(() => `<span class="estrela">${ESTRELA}</span>`).join('')}</div>
            <p class="av-legenda" id="legenda"></p>
            <div class="en-estado"><strong>Obrigado pela avaliação!</strong>Ela já aparece na reputação de ${esc(c.avaliado)}.</div>
            <div class="actions"><a class="btn btn-primary" href="contratos.html">Voltar aos contratos</a></div>`;
        pintarEstrelas();
        return;
    }

    if (!concluido) {
        $('#principal').innerHTML = `${quem}<div class="en-estado"><strong>Avaliação indisponível</strong>Só é possível avaliar contratos concluídos.</div>
            <div class="actions"><a class="btn btn-secondary" href="contratos.html">Voltar aos contratos</a></div>`;
        return;
    }

    $('#principal').innerHTML = `${quem}
        <span class="label" id="rotulo-nota">Sua nota</span>
        <div class="estrelas" role="radiogroup" aria-labelledby="rotulo-nota">
            ${[1, 2, 3, 4, 5].map((n) => `<button class="estrela" type="button" role="radio" data-nota="${n}" aria-label="${n} de 5">${ESTRELA}</button>`).join('')}
        </div>
        <p class="av-legenda" id="legenda"></p>
        <div class="field av-comentario">
            <label class="label" for="comentario">Comentário <small>(opcional)</small></label>
            <textarea class="textarea" id="comentario" maxlength="1000" placeholder="Conte como foi o serviço: pontualidade, qualidade, comunicação."></textarea>
        </div>
        <div class="actions">
            <a class="btn btn-secondary" href="contratos.html">Agora não</a>
            <button class="btn btn-primary" type="button" id="btn-enviar">Enviar avaliação</button>
        </div>`;
    pintarEstrelas();

    document.querySelectorAll('.estrela').forEach((b) => b.addEventListener('click', () => {
        nota = Number(b.dataset.nota);
        pintarEstrelas();
    }));

    $('#btn-enviar').addEventListener('click', (e) => {
        if (!nota) return toastErro(new Error('Escolha uma nota de 1 a 5.'));
        return comCarregamento(e.currentTarget, async () => {
            try {
                render(await avaliar(idContrato, nota, $('#comentario').value.trim() || null));
                toast('Avaliação enviada. Obrigado!');
            } catch (err) {
                toastErro(err);
            }
        });
    });
}

async function carregar() {
    try {
        render(await obterAvaliacao(idContrato));
    } catch (e) {
        $('#principal').innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

carregar();
