const apiBase = '/api';

let rankingActual = [];
let organoSeleccionado = null;
let pacienteSeleccionadoParaAsignar = null;
let paginaActual = 1;
const registrosPorPagina = 10;

document.addEventListener('DOMContentLoaded', function () {
    conectarTabs();
    conectarBotones();
    conectarModales();
    cargarOrganosDisponibles();
});

// Tabs
function conectarTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');

    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            const tabId = this.dataset.tab;

            document.querySelectorAll('.tab-content').forEach(tab => {
                tab.classList.remove('active');
            });

            document.querySelectorAll('.tab-btn').forEach(btn => {
                btn.classList.remove('active');
            });

            document.getElementById(tabId).classList.add('active');
            this.classList.add('active');
        });
    });
}

// Botones principales
function conectarBotones() {
    document.getElementById('btnCargarRankingOrgano').addEventListener('click', cargarRankingPorOrgano);
    document.getElementById('btnCargarRankingTipo').addEventListener('click', cargarRankingPorTipo);

    document.getElementById('checkVerificado').addEventListener('change', toggleBotonConfirmar);
    document.getElementById('btnConfirmarAsignacion').addEventListener('click', confirmarAsignacion);
    document.getElementById('btnCancelarAsignacion').addEventListener('click', cerrarModalAsignar);

    document.getElementById('logoutButton').addEventListener('click', function () {
        localStorage.removeItem('token');
        sessionStorage.clear();
        window.location.href = 'login.html';
    });

    document.getElementById('backMenuButton').addEventListener('click', function () {
        window.location.href = 'admin.html';
    });
}

// Modales
function conectarModales() {
    document.getElementById('cerrarModalAsignar').addEventListener('click', cerrarModalAsignar);
    document.getElementById('cerrarModalDetalles').addEventListener('click', cerrarModalDetalles);
    document.getElementById('btnCerrarDetalles').addEventListener('click', cerrarModalDetalles);

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            cerrarModalAsignar();
            cerrarModalDetalles();
        }
    });

    document.getElementById('modalAsignar').addEventListener('click', function (event) {
        if (event.target.id === 'modalAsignar') {
            cerrarModalAsignar();
        }
    });

    document.getElementById('modalDetalles').addEventListener('click', function (event) {
        if (event.target.id === 'modalDetalles') {
            cerrarModalDetalles();
        }
    });
}

// Cargar órganos disponibles
async function cargarOrganosDisponibles() {
    try {
        const response = await fetch(`${apiBase}/organo`, {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error('Error al cargar órganos');
        }

        const organos = await response.json();
        const disponibles = organos.filter(o => o.estado === 'Disponible');

        const select = document.getElementById('organoId');
        select.innerHTML = '<option value="">Seleccionar órgano...</option>';

        if (disponibles.length === 0) {
            select.innerHTML = '<option value="">No hay órganos disponibles</option>';

            document.getElementById('rankingOrganoContainer').innerHTML = `
                <div class="empty-state">
                    <h3>ℹ️ No hay órganos disponibles en este momento</h3>
                    <p>Cargue un nuevo donante o espere a que se disponibilicen órganos.</p>
                </div>
            `;
        }

        disponibles.forEach(organo => {
            const option = document.createElement('option');
            option.value = organo.id;
            option.textContent = `${organo.tipoOrgano} - ${organo.donante?.nombre || 'Sin info'} (${organo.donante?.tipoSanguineo || 'Sin sangre'})`;
            select.appendChild(option);
        });

        document.getElementById('statOrganos').textContent = disponibles.length;

    } catch (error) {
        mostrarMensaje(`Error: ${error.message}`, 'error');
    }
}

// Cargar ranking por órgano específico
async function cargarRankingPorOrgano() {
    const organoId = document.getElementById('organoId').value;

    if (!organoId) {
        mostrarMensaje('Seleccione un órgano disponible para consultar el ranking.', 'warning');
        return;
    }

    mostrarCarga('rankingOrganoContainer');

    try {
        const response = await fetch(`${apiBase}/organo/${organoId}/ranking`, {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error('Error al cargar ranking');
        }

        const ranking = await response.json();

        organoSeleccionado = organoId;
        rankingActual = ordenarRanking(ranking);
        paginaActual = 1;

        actualizarResumenRanking(rankingActual);
        mostrarRanking(rankingActual, 'rankingOrganoContainer', true);

    } catch (error) {
        mostrarMensaje(`Error: ${error.message}`, 'error');
        document.getElementById('rankingOrganoContainer').innerHTML = '';
    }
}

// Cargar ranking por tipo de órgano
async function cargarRankingPorTipo() {
    const tipoOrgano = document.getElementById('tipoOrganoSelect').value;
    const tipoSanguineo = document.getElementById('tipoSanguineoSelect').value;

    if (!tipoOrgano || !tipoSanguineo) {
        mostrarMensaje('Complete el tipo de órgano y el tipo de sangre del donante.', 'warning');
        return;
    }

    mostrarCarga('rankingTipoContainer');

    try {
        const response = await fetch(`${apiBase}/organo/ranking-tipo/${encodeURIComponent(tipoOrgano)}/${encodeURIComponent(tipoSanguineo)}`, {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error('Error al cargar ranking');
        }

        const data = await response.json();
        rankingActual = ordenarRanking(Array.isArray(data) ? data : (data.datos || []));
        organoSeleccionado = null;
        paginaActual = 1;

        mostrarRanking(rankingActual, 'rankingTipoContainer', false);

    } catch (error) {
        mostrarMensaje(`Error: ${error.message}`, 'error');
        document.getElementById('rankingTipoContainer').innerHTML = '';
    }
}

// Ordenar ranking por puntaje
function ordenarRanking(ranking) {
    return [...ranking].sort((a, b) => {
        const puntajeA = Number(a.puntajeTotal || 0);
        const puntajeB = Number(b.puntajeTotal || 0);
        return puntajeB - puntajeA;
    });
}

// Actualizar resumen
function actualizarResumenRanking(ranking) {
    document.getElementById('statCompatibles').textContent = ranking.length;

    if (ranking.length > 0) {
        const max = Math.max(...ranking.map(r => Number(r.puntajeTotal || 0)));
        document.getElementById('statPuntajeMaximo').textContent = max.toFixed(2);
    } else {
        document.getElementById('statPuntajeMaximo').textContent = '-';
    }
}

// Mostrar ranking en tabla
function mostrarRanking(ranking, containerId, permitirAsignar) {
    const container = document.getElementById(containerId);

    if (!ranking || ranking.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <h3>ℹ️ No hay pacientes compatibles para este órgano</h3>
                <p>Intente seleccionar otro órgano o tipo de sangre.</p>
            </div>
        `;
        return;
    }

    const inicio = (paginaActual - 1) * registrosPorPagina;
    const fin = inicio + registrosPorPagina;
    const pagina = ranking.slice(inicio, fin);
    const totalPaginas = Math.ceil(ranking.length / registrosPorPagina);

    const infoHtml = `
        <div class="ranking-info">
            <h3>📈 Información del Ranking</h3>
            <div class="ranking-stats">
                <div class="stat-card">
                    <strong>Pacientes Compatibles</strong>
                    <div class="stat-value">${ranking.length}</div>
                </div>
                <div class="stat-card">
                    <strong>Puntaje Máximo</strong>
                    <div class="stat-value">${Math.max(...ranking.map(r => Number(r.puntajeTotal || 0))).toFixed(2)}</div>
                </div>
                <div class="stat-card">
                    <strong>Puntaje Mínimo</strong>
                    <div class="stat-value">${Math.min(...ranking.map(r => Number(r.puntajeTotal || 0))).toFixed(2)}</div>
                </div>
            </div>
        </div>
    `;

    const tablaHtml = `
        <div class="table-responsive">
            <table class="ranking-table">
                <thead>
                    <tr>
                        <th style="text-align:center;">Posición</th>
                        <th>Nombre Paciente</th>
                        <th>Tipo Sangre</th>
                        <th>Órgano Requerido</th>
                        <th>Urgencia</th>
                        <th style="text-align:center;">Puntaje</th>
                        <th style="text-align:center;">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    ${pagina.map((paciente, index) => crearFilaRanking(paciente, inicio + index + 1, permitirAsignar)).join('')}
                </tbody>
            </table>
        </div>

        ${crearPaginacion(totalPaginas, containerId, permitirAsignar)}
    `;

    container.innerHTML = infoHtml + tablaHtml;
}

// Crear fila
function crearFilaRanking(paciente, posicion, permitirAsignar) {
    const urgencia = normalizarUrgencia(paciente.nivelUrgencia);
    const pacienteJson = encodeURIComponent(JSON.stringify(paciente));

    return `
        <tr>
            <td style="text-align:center;">
                <span class="posicion-badge">${posicion}°</span>
            </td>

            <td>${paciente.nombrePaciente || 'Sin nombre'}</td>
            <td>${paciente.tipoSanguineo || 'N/A'}</td>
            <td>${paciente.organoRequerido || 'N/A'}</td>

            <td>
                <span class="urgencia-${urgencia}">
                    ${paciente.nivelUrgencia || 'N/A'}
                </span>
            </td>

            <td style="text-align:center;">
                <span class="puntaje-badge">${Number(paciente.puntajeTotal || 0).toFixed(2)}</span>
            </td>

            <td>
                <div class="action-buttons">
                    ${permitirAsignar ? `
                        <button type="button" class="btn btn-primary btn-sm" onclick="abrirModalAsignarDesdeJson('${pacienteJson}')">
                            Asignar
                        </button>
                    ` : ''}

                    <button type="button" class="btn btn-secondary btn-sm" onclick="abrirModalDetallesDesdeJson('${pacienteJson}')">
                        Ver Detalles
                    </button>
                </div>
            </td>
        </tr>
    `;
}

// Normalizar urgencia
function normalizarUrgencia(urgencia) {
    const valor = (urgencia || '').toString().toLowerCase();

    if (valor.includes('alta')) return 'alta';
    if (valor.includes('media')) return 'media';
    if (valor.includes('baja')) return 'baja';

    return 'baja';
}

// Paginación
function crearPaginacion(totalPaginas, containerId, permitirAsignar) {
    if (totalPaginas <= 1) return '';

    return `
        <div class="ranking-info" style="margin-top:1rem;">
            <div class="action-buttons" style="justify-content:space-between;">
                <button type="button" class="btn btn-secondary btn-sm" onclick="cambiarPaginaRanking(-1, '${containerId}', ${permitirAsignar})" ${paginaActual === 1 ? 'disabled' : ''}>
                    Anterior
                </button>

                <strong>Página ${paginaActual} de ${totalPaginas}</strong>

                <button type="button" class="btn btn-secondary btn-sm" onclick="cambiarPaginaRanking(1, '${containerId}', ${permitirAsignar})" ${paginaActual === totalPaginas ? 'disabled' : ''}>
                    Siguiente
                </button>
            </div>
        </div>
    `;
}

function cambiarPaginaRanking(direccion, containerId, permitirAsignar) {
    const totalPaginas = Math.ceil(rankingActual.length / registrosPorPagina);
    const nuevaPagina = paginaActual + direccion;

    if (nuevaPagina < 1 || nuevaPagina > totalPaginas) return;

    paginaActual = nuevaPagina;
    mostrarRanking(rankingActual, containerId, permitirAsignar);
}

// Modal detalles
function abrirModalDetallesDesdeJson(json) {
    const paciente = JSON.parse(decodeURIComponent(json));
    abrirModalDetalles(paciente);
}

function abrirModalDetalles(paciente) {
    const body = document.getElementById('detallesBody');

    body.innerHTML = `
        <div class="detail-grid">
            <div class="detail-label">Nombre:</div>
            <div class="detail-value">${paciente.nombrePaciente || 'Sin nombre'}</div>

            <div class="detail-label">Tipo de sangre:</div>
            <div class="detail-value">${paciente.tipoSanguineo || 'N/A'}</div>

            <div class="detail-label">Órgano requerido:</div>
            <div class="detail-value">${paciente.organoRequerido || 'N/A'}</div>

            <div class="detail-label">Urgencia:</div>
            <div class="detail-value">${paciente.nivelUrgencia || 'N/A'}</div>

            <div class="detail-label">Puntaje:</div>
            <div class="detail-value">${Number(paciente.puntajeTotal || 0).toFixed(2)}</div>

            <div class="detail-label">Posición:</div>
            <div class="detail-value">${paciente.posicion || 'N/A'}</div>

            <div class="detail-label">ID Paciente:</div>
            <div class="detail-value">${paciente.pacienteId || 'N/A'}</div>
        </div>
    `;

    document.getElementById('modalDetalles').classList.add('active');
}

function cerrarModalDetalles() {
    document.getElementById('modalDetalles').classList.remove('active');
}

// Modal asignar
function abrirModalAsignarDesdeJson(json) {
    const paciente = JSON.parse(decodeURIComponent(json));
    abrirModalAsignar(paciente);
}

function abrirModalAsignar(paciente) {
    pacienteSeleccionadoParaAsignar = paciente;

    const selectOrgano = document.getElementById('organoId');
    const organoTexto = selectOrgano.options[selectOrgano.selectedIndex]?.text || 'N/A';

    document.getElementById('pacienteInfo').innerHTML = `
        <div class="detail-grid">
            <div class="detail-label">Órgano:</div>
            <div class="detail-value">${organoTexto}</div>

            <div class="detail-label">Paciente:</div>
            <div class="detail-value">${paciente.nombrePaciente}</div>

            <div class="detail-label">Tipo sangre:</div>
            <div class="detail-value">${paciente.tipoSanguineo}</div>

            <div class="detail-label">Órgano requerido:</div>
            <div class="detail-value">${paciente.organoRequerido}</div>

            <div class="detail-label">Urgencia:</div>
            <div class="detail-value">${paciente.nivelUrgencia}</div>

            <div class="detail-label">Puntaje:</div>
            <div class="detail-value">${Number(paciente.puntajeTotal || 0).toFixed(2)}</div>
        </div>
    `;

    document.getElementById('justificacion').value = '';
    document.getElementById('checkVerificado').checked = false;
    document.getElementById('btnConfirmarAsignacion').disabled = true;
    document.getElementById('btnConfirmarAsignacion').textContent = 'Confirmar Asignación';

    document.getElementById('modalAsignar').classList.add('active');
}

function cerrarModalAsignar() {
    document.getElementById('modalAsignar').classList.remove('active');
    pacienteSeleccionadoParaAsignar = null;
}

function toggleBotonConfirmar() {
    const check = document.getElementById('checkVerificado').checked;
    document.getElementById('btnConfirmarAsignacion').disabled = !check;
}

// Confirmar asignación
async function confirmarAsignacion() {
    if (!pacienteSeleccionadoParaAsignar || !organoSeleccionado) {
        mostrarMensaje('Error: Información incompleta para confirmar la asignación.', 'error');
        return;
    }

    const justificacion = document.getElementById('justificacion').value.trim();

    if (!justificacion) {
        mostrarMensaje('La justificación es obligatoria.', 'warning');
        return;
    }

    if (!document.getElementById('checkVerificado').checked) {
        mostrarMensaje('Debe verificar los datos antes de confirmar.', 'warning');
        return;
    }

    const btn = document.getElementById('btnConfirmarAsignacion');
    btn.disabled = true;
    btn.textContent = 'Confirmando...';

    try {
        const response = await fetch('/api/asignacion/confirmar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                organoId: parseInt(organoSeleccionado),
                pacienteId: pacienteSeleccionadoParaAsignar.pacienteId,
                justificacion: justificacion
            })
        });

        const data = await response.json();

        if (response.ok) {
            mostrarMensaje('Asignación confirmada correctamente.', 'success');
            cerrarModalAsignar();

            await cargarOrganosDisponibles();
            await cargarRankingPorOrgano();
        } else {
            mostrarMensaje(data.mensaje || 'Error al confirmar la asignación.', 'error');
            btn.disabled = false;
            btn.textContent = 'Confirmar Asignación';
        }

    } catch (error) {
        mostrarMensaje('Error al confirmar la asignación.', 'error');
        btn.disabled = false;
        btn.textContent = 'Confirmar Asignación';
    }
}

// Utilidades
function mostrarCarga(containerId) {
    document.getElementById(containerId).innerHTML = `
        <div class="loading">
            <div class="spinner"></div>
            <p>Cargando ranking...</p>
        </div>
    `;
}

function mostrarMensaje(texto, tipo) {
    const messageDiv = document.getElementById('message');
    messageDiv.innerHTML = `<div class="alert alert-${tipo}">${texto}</div>`;

    setTimeout(() => {
        messageDiv.innerHTML = '';
    }, 5000);
}