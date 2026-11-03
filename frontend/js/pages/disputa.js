import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { toast, erro as toastErro, comCarregamento } from '../components/toast.js';
import { abrirDisputa, obterDisputa } from '../api/disputas.js';
import { exigirUsuario, param } from '../utils/storage.js';
import { dataHora, esc, moeda } from '../utils/formatadores.js';

exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('contratos');
carregarFooter();

const $ = (s) => document.querySelector(s);
const idContrato = Number(param('contrato'));
const SITUACAO = { GERADO: 'Gerado', ACEITO_CLIENTE: 'Aguardando assinatura', ACEITO_TRABALHADOR: 'Aguardando assinatura', ATIVO: 'Ativo', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado', EM_DISPUTA: 'Em disputa' };
const STATUS = { ABERTA: ['Aberta', 'badge--peach'], EM_ANALISE: ['Em análise', 'badge--blue'], RESOLVIDA: ['Resolvida', 'badge--sand'], ARQUIVADA: ['Arquivada', 'badge--sand'] };

function marcarEtapa(status) {
    const ordem = ['ABERTA', 'EM_ANALISE', 'RESOLVIDA'];
    const atual = ordem.indexOf(status ?? 'ABERTA');
    document.querySelectorAll('#etapas li').forEach((li, i) => {
        li.classList.toggle('is-atual', i === atual);
        li.classList.toggle('is-feito', i < atual);
    });
}

function render(c) {
    $('#numero').textContent = `Contrato Nº ${c.numeroContrato}`;
    const outraParte = c.meuPapel === 'TRABALHADOR' ? ['Cliente', c.cliente] : ['Trabalhador', c.trabalhador];
    const strip = `
        <dl class="info-strip ds-strip">
            <div><dt>${outraParte[0]}</dt><dd>${esc(outraParte[1])}</dd></div>
            <div><dt>Valor em custódia</dt><dd>${moeda(c.valorCustodia)}</dd></div>
            <div><dt>Situação do contrato</dt><dd>${SITUACAO[c.situacaoContrato] ?? c.situacaoContrato}</dd></div>
        </dl>`;

    const d = c.disputa;
    const aberta = d && ['ABERTA', 'EM_ANALISE'].includes(d.status);
    if (d && (aberta || c.situacaoContrato !== 'ATIVO')) {
        $('#titulo').textContent = `Disputa ${d.numero}`;
        const [rotulo, classe] = STATUS[d.status];
        $('#principal').innerHTML = `
            ${strip}
            <div class="ap-topo" style="margin-bottom:14px">
                <span class="label" style="margin:0">Motivo da disputa</span>
                <span class="badge ${classe}">${rotulo}</span>
            </div>
            <div class="box-cream">${esc(d.motivo)}</div>
            <p class="hint">Aberta por ${esc(d.abertaPor)} em ${esc(dataHora(d.dataAbertura))}.</p>
            ${d.decisao ? `<div class="ds-resultado"><span class="label">Decisão do administrador</span><div class="box-cream">${esc(d.decisao)}</div></div>` : ''}
            <div class="actions"><a class="btn btn-secondary" href="contratos.html">Voltar aos contratos</a></div>`;
        marcarEtapa(d.status);
        return;
    }

    if (c.situacaoContrato !== 'ATIVO' || c.statusPagamento !== 'EM_CUSTODIA') {
        $('#principal').innerHTML = `${strip}<div class="en-estado"><strong>Disputa indisponível</strong>A disputa só pode ser aberta em contratos ativos com o pagamento em custódia.</div>
            <div class="actions"><a class="btn btn-secondary" href="contrato.html?id=${c.idContrato}">Ver contrato</a></div>`;
        marcarEtapa('ABERTA');
        return;
    }

    $('#principal').innerHTML = `
        ${strip}
        <form id="form-disputa" novalidate>
            <div class="field ds-motivo">
                <label class="label" for="motivo">Motivo da disputa</label>
                <textarea class="textarea" id="motivo" maxlength="2000" placeholder="Explique o que aconteceu, com datas e detalhes."></textarea>
                <p class="hint">O administrador também terá acesso ao chat e ao contrato.</p>
            </div>
            <label class="check check-box">
                <input type="checkbox" id="entendo">
                <span>Entendo que o pagamento permanece em custódia até a decisão do administrador.</span>
            </label>
            <div class="actions">
                <a class="btn btn-secondary" href="${c.meuPapel === 'CLIENTE' ? `entrega.html?contrato=${c.idContrato}` : `contrato.html?id=${c.idContrato}`}">Cancelar</a>
                <button class="btn btn-primary" type="submit" id="btn-enviar">Enviar disputa</button>
            </div>
        </form>`;
    marcarEtapa('ABERTA');

    $('#form-disputa').addEventListener('submit', async (e) => {
        e.preventDefault();
        const motivo = $('#motivo').value.trim();
        if (motivo.length < 20) return toastErro(new Error('Explique o que aconteceu (mínimo de 20 caracteres).'));
        if (!$('#entendo').checked) return toastErro(new Error('Confirme que entende que o pagamento permanece em custódia.'));
        await comCarregamento($('#btn-enviar'), async () => {
            try {
                render(await abrirDisputa(idContrato, motivo, true));
                toast('Disputa aberta. Um administrador vai analisar o caso.');
            } catch (err) {
                toastErro(err);
            }
        });
    });
}

async function carregar() {
    try {
        render(await obterDisputa(idContrato));
    } catch (e) {
        $('#principal').innerHTML = `<div class="empty">${esc(e.message)}</div>`;
    }
}

carregar();
