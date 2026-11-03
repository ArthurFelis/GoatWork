import { listarChats } from '../api/chats.js';
import { listarPedidos } from '../api/pedidos.js';
import { esc } from '../utils/formatadores.js';

/**
 * Barra lateral "CONVERSAS" (telas Mensagens / Conversa com a IA).
 * @param {HTMLElement} alvo
 * @param {{usuario: object, ativo: 'ia'|number|null, idPedidoIa?: number}} opcoes
 * @returns {Promise<{chats: object[], pedidos: object[]}>}
 */
export async function carregarConversas(alvo, { usuario, ativo = null, idPedidoIa = null }) {
    const ehCliente = usuario.tipo === 'CLIENTE';
    const [chats, pedidos] = await Promise.all([
        listarChats(),
        ehCliente ? listarPedidos() : Promise.resolve([])
    ]);

    const itens = [];

    if (ehCliente) {
        const emBriefing = pedidos.find((p) => p.status === 'EM_BRIEFING');
        const idIa = idPedidoIa ?? emBriefing?.id;
        itens.push(`
            <li><a class="conversa ${ativo === 'ia' ? 'is-active' : ''}" href="${idIa ? `chat-ia.html?pedido=${idIa}` : 'novo-pedido.html'}">
                <strong>Assistente IA</strong><span>Triagem do pedido</span>
            </a></li>`);
    }

    for (const c of chats) {
        itens.push(`
            <li><a class="conversa ${ativo === c.idChat ? 'is-active' : ''}" href="chat.html?chat=${c.idChat}">
                <strong>${esc(c.titulo)}</strong><span>${esc(c.subtitulo)}</span>
            </a></li>`);
    }

    // Pedidos ainda sem profissional: o chat só abre depois do aceite do convite
    if (ehCliente) {
        pedidos
            .filter((p) => !p.idChat && (p.status === 'PUBLICADO' || (p.status === 'EM_BRIEFING' && p.id === idPedidoIa)))
            .slice(0, 3)
            .forEach((p) => itens.push(`
                <li><div class="conversa is-disabled" title="${esc(p.titulo)}">
                    <strong>${esc(p.trabalhadorPreferido ?? 'Profissionais')}</strong><span>Liberado após o aceite</span>
                </div></li>`));
    }

    alvo.innerHTML = `<h2>Conversas</h2>${itens.length ? `<ul>${itens.join('')}</ul>` : '<p class="muted" style="font-size:13px">Nenhuma conversa ainda.</p>'}`;
    return { chats, pedidos };
}
