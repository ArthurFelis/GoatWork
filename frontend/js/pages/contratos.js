import { carregarNavbar } from '../components/navbar.js';
import { carregarFooter } from '../components/footer.js';
import { listarContratos } from '../api/contratos.js';
import { listarPedidos } from '../api/pedidos.js';
import { exigirUsuario } from '../utils/storage.js';
import { esc, moeda, quando } from '../utils/formatadores.js';

const usuario = exigirUsuario(['CLIENTE', 'TRABALHADOR']);
carregarNavbar('contratos');
carregarFooter();

const $ = (s) => document.querySelector(s);
const ehCliente = usuario.tipo === 'CLIENTE';
const seta = (texto, href) => `<a class="link" href="${href}">${texto} →</a>`;

const SITUACAO = {
    AGUARDANDO_ASSINATURA: ['Aguardando assinatura', 'badge--peach'],
    AGUARDANDO_PAGAMENTO: ['Aguardando pagamento', 'badge--peach'],
    EM_EXECUCAO: ['Em execução', 'badge--blue'],
    ENTREGA_ENVIADA: ['Entrega enviada', 'badge--blue'],
    EM_DISPUTA: ['Em disputa', 'badge--sand'],
    CONCLUIDO: ['Concluído', 'badge--sand'],
    CANCELADO: ['Cancelado', 'badge--red']
};

function proximoPasso(c) {
    const contrato = `contrato.html?id=${c.id}`;
    const entrega = `entrega.html?contrato=${c.id}`;
    switch (c.etapa) {
        case 'AGUARDANDO_ASSINATURA':
            return c.assinadoPorMim ? seta('Ver contrato', contrato) : seta('Ver e assinar', contrato);
        case 'AGUARDANDO_PAGAMENTO':
            return ehCliente ? seta('Pagar', `pagamento.html?contrato=${c.id}`) : seta('Ver contrato', contrato);
        case 'EM_EXECUCAO':
            return ehCliente ? seta('Acompanhar', entrega) : seta('Enviar entrega', entrega);
        case 'ENTREGA_ENVIADA':
            return ehCliente ? seta('Aprovar entrega', entrega) : seta('Ver entrega', entrega);
        case 'EM_DISPUTA':
            return seta('Ver disputa', `disputa.html?contrato=${c.id}`);
        case 'CONCLUIDO':
            return c.avaliadoPorMim ? seta('Ver contrato', contrato) : seta('Avaliar', `avaliacao.html?contrato=${c.id}`);
        default:
            return c.idDisputa ? seta('Ver disputa', `disputa.html?contrato=${c.id}`) : seta('Ver contrato', contrato);
    }
}

function cardPedido(p) {
    let badge;
    let meta = `${esc(p.categoria)} · `;
    let acao;
    if (p.status === 'RASCUNHO') {
        badge = '<span class="badge badge--sand">Rascunho</span>';
        meta += `criado ${quando(p.dataCriacao)}`;
        acao = seta('Continuar pedido', `novo-pedido.html?pedido=${p.id}`);
    } else if (p.status === 'EM_BRIEFING') {
        badge = '<span class="badge badge--peach">Em briefing</span>';
        meta += `criado ${quando(p.dataCriacao)}`;
        acao = seta('Continuar com a IA', `chat-ia.html?pedido=${p.id}`);
    } else if (p.idChat) {
        badge = '<span class="badge badge--blue">Convite aceito</span>';
        meta += `${esc(p.trabalhadorAceito)} aceitou`;
        acao = seta('Abrir chat', `chat.html?chat=${p.idChat}`);
    } else {
        badge = '<span class="badge badge--peach">Aguardando aceite</span>';
        meta += `enviado ${quando(p.dataCriacao)}`;
        acao = seta('Ver briefing', `revisao-briefing.html?pedido=${p.id}`);
    }
    return `
        <article class="pedido-card">
            <div class="pedido-topo"><h3>${esc(p.titulo)}</h3>${badge}</div>
            <p class="pedido-meta">${meta}</p>
            ${acao}
        </article>`;
}

async function carregar() {
    if (ehCliente) {
        $('#btn-novo').hidden = false;
        $('#secao-pedidos').hidden = false;
    } else {
        $('#titulo').textContent = 'Contratos';
        $('#col-parte').textContent = 'Cliente';
    }

    try {
        const [contratos, pedidos] = await Promise.all([
            listarContratos(),
            ehCliente ? listarPedidos() : Promise.resolve([])
        ]);

        if (ehCliente) {
            const abertos = pedidos.filter((p) => ['RASCUNHO', 'EM_BRIEFING', 'PUBLICADO'].includes(p.status));
            $('#pedidos').innerHTML = abertos.length
                ? abertos.map(cardPedido).join('')
                : '<div class="empty" style="grid-column:1/-1"><strong>Nenhum pedido em aberto</strong>Conte o que você precisa e a IA monta o briefing.</div>';
        }

        $('#contratos').innerHTML = contratos.length
            ? contratos.map((c) => {
                const [rotulo, classe] = SITUACAO[c.etapa] ?? [c.status, 'badge--sand'];
                return `
                    <tr>
                        <td>${esc(c.numero)}</td>
                        <td>${esc(c.servico)}</td>
                        <td>${esc(c.outraParte)}</td>
                        <td>${moeda(c.valor)}</td>
                        <td><span class="badge ${classe}">${rotulo}</span></td>
                        <td>${proximoPasso(c)}</td>
                    </tr>`;
            }).join('')
            : `<tr><td colspan="6"><div class="empty" style="border:0">${ehCliente
                ? 'Os contratos aparecem aqui quando você fecha um acordo no chat.'
                : 'Os contratos aparecem aqui quando você fecha um acordo com um cliente.'}</div></td></tr>`;
    } catch (e) {
        $('#contratos').innerHTML = `<tr><td colspan="6"><div class="empty">${esc(e.message)}</div></td></tr>`;
    }
}

carregar();
