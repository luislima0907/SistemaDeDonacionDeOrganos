let bitacorasActuales = [];
let paginaActual = 1;
const registrosPorPagina = 10;

// Cargar bitácoras al abrir la página
document.addEventListener('DOMContentLoaded', function() {
    cargarBitacoras();
    
    // Cargar resumen cuando se hace clic en la pestaña
    document.getElementById('resumen-tab').addEventListener('click', function() {
        cargarResumen();
    });
});

// Función para cargar bitácoras
async function cargarBitacoras() {
    const loadingDiv = document.getElementById('bitacoraLoading');
    const contentDiv = document.getElementById('bitacoraTableContent');
    const emptyState = document.getElementById('emptyState');

    loadingDiv.classList.remove('d-none');
    contentDiv.innerHTML = '';
    emptyState.classList.add('d-none');
    paginaActual = 1;

    try {
        let url = `http://localhost:5135/api/bitacora/todas?dias=30`;

        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(`Error: ${response.status}`);
        }

        const resultado = await response.json();
        bitacorasActuales = resultado.data || [];

        // Actualizar estadísticas
        actualizarEstadisticas();

        // Mostrar tabla
        mostrarTabla();

        if (bitacorasActuales.length === 0) {
            emptyState.classList.remove('d-none');
            contentDiv.innerHTML = '';
        }

    } catch (error) {
        console.error('Error al cargar bitácoras:', error);
        const errorDiv = document.getElementById('bitacoraTableContent');
        if (errorDiv) {
            errorDiv.innerHTML = `
                <div class="alert alert-danger m-3" role="alert">
                    <strong>Error:</strong> No se pudieron cargar los registros de auditoría. ${error.message}
                </div>
            `;
        }
    } finally {
        loadingDiv.classList.add('d-none');
    }
}

// Actualizar estadísticas
function actualizarEstadisticas() {
    // Debug: ver qué acciones existen en los datos
    const accionesUnicas = [...new Set(bitacorasActuales.map(b => b.accion))];
    console.log('Acciones encontradas en la BD:', accionesUnicas);
    console.log('Total de registros:', bitacorasActuales.length);
    console.log('Datos completos:', bitacorasActuales);

    // Contar con búsqueda insensible a mayúsculas y espacios
    // Para Crear: busca "crear", "registrar", "crear", etc.
    const crear = bitacorasActuales.filter(b => 
        b.accion && (
            b.accion.toLowerCase().includes('crear') ||
            b.accion.toLowerCase().includes('registrar')
        )
    ).length;
    
    const actualizar = bitacorasActuales.filter(b => 
        b.accion && b.accion.toLowerCase().includes('actualizar')
    ).length;
    
    const eliminar = bitacorasActuales.filter(b => 
        b.accion && b.accion.toLowerCase().includes('eliminar')
    ).length;
    
    const total = bitacorasActuales.length;

    console.log('Estadísticas calculadas:', { crear, actualizar, eliminar, total });

    const statCrear = document.getElementById('statCrear');
    const statActualizar = document.getElementById('statActualizar');
    const statEliminar = document.getElementById('statEliminar');
    const statTotal = document.getElementById('statTotal');

    if (statCrear) statCrear.textContent = crear;
    if (statActualizar) statActualizar.textContent = actualizar;
    if (statEliminar) statEliminar.textContent = eliminar;
    if (statTotal) statTotal.textContent = total;
}

// Mostrar tabla con paginación
function mostrarTabla() {
    const contentDiv = document.getElementById('bitacoraTableContent');
    const inicio = (paginaActual - 1) * registrosPorPagina;
    const fin = inicio + registrosPorPagina;
    const bitacorasPagina = bitacorasActuales.slice(inicio, fin);

    if (bitacorasPagina.length === 0) {
        contentDiv.innerHTML = '';
        return;
    }

    let html = `
        <table class="table table-hover mb-0">
            <thead>
                <tr>
                    <th>Fecha/Hora</th>
                    <th>Usuario</th>
                    <th>Acción</th>
                    <th>Tabla</th>
                    <th>Registro ID</th>
                </tr>
            </thead>
            <tbody>
    `;

    bitacorasPagina.forEach(bitacora => {
        const fecha = new Date(bitacora.fechaAccion).toLocaleString('es-ES');
        const accionClass = obtenerClaseAccion(bitacora.accion);
        const usuario = bitacora.usuario?.nombre || 'Sistema';
        const rol = bitacora.usuario?.rol || '';

        html += `
            <tr>
                <td>
                    <small>${fecha}</small>
                </td>
                <td>
                    <div>${usuario}</div>
                    <small class="text-muted">${rol}</small>
                </td>
                <td>
                    <span class="action-badge ${accionClass}">${bitacora.accion}</span>
                </td>
                <td>
                    <small>${bitacora.tabla}</small>
                </td>
                <td>
                    <code>#${bitacora.registroId}</code>
                </td>
            </tr>
        `;
    });

    html += `
            </tbody>
        </table>
    `;

    contentDiv.innerHTML = html;

    // Mostrar paginación si hay más registros
    actualizarPaginacion();
}

// Actualizar controles de paginación
function actualizarPaginacion() {
    const totalPaginas = Math.ceil(bitacorasActuales.length / registrosPorPagina);
    const paginationContainer = document.getElementById('paginationContainer');

    if (totalPaginas > 1) {
        paginationContainer.classList.remove('d-none');
        document.getElementById('paginationInfo').textContent = 
            `Página ${paginaActual} de ${totalPaginas} (${bitacorasActuales.length} registros)`;
    } else {
        paginationContainer.classList.add('d-none');
    }
}

// Navegar a página anterior
function paginaAnterior() {
    if (paginaActual > 1) {
        paginaActual--;
        mostrarTabla();
        window.scrollTo(0, 0);
    }
}

// Navegar a página siguiente
function paginaSiguiente() {
    const totalPaginas = Math.ceil(bitacorasActuales.length / registrosPorPagina);
    if (paginaActual < totalPaginas) {
        paginaActual++;
        mostrarTabla();
        window.scrollTo(0, 0);
    }
}

// Obtener clase CSS para la acción
function obtenerClaseAccion(accion) {
    const mapa = {
        'Crear': 'create',
        'Actualizar': 'update',
        'Eliminar': 'delete',
        'Consulta': 'consulta',
        'Consultar Ranking Prioridad': 'consulta'
    };
    return mapa[accion] || 'consulta';
}

// Mostrar detalles en modal
async function mostrarDetalles(bitacoraId) {
    const bitacora = bitacorasActuales.find(b => b.id === bitacoraId);
    if (!bitacora) return;

    const modalBody = document.getElementById('modalBody');
    const modal = new bootstrap.Modal(document.getElementById('detallesModal'));

    let detallesHTML = `
        <div class="detail-row">
            <div class="detail-label">ID:</div>
            <div class="detail-value">${bitacora.id}</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Fecha/Hora:</div>
            <div class="detail-value">${new Date(bitacora.fechaAccion).toLocaleString('es-ES')}</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Usuario:</div>
            <div class="detail-value">${bitacora.usuario?.nombre || 'Sistema'} (${bitacora.usuario?.rol || ''})</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Acción:</div>
            <div class="detail-value"><span class="action-badge ${obtenerClaseAccion(bitacora.accion)}">${bitacora.accion}</span></div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Tabla:</div>
            <div class="detail-value">${bitacora.tabla}</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">ID Registro:</div>
            <div class="detail-value">#${bitacora.registroId}</div>
        </div>
    `;

    if (bitacora.ipAddress) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">IP Address:</div>
                <div class="detail-value">${bitacora.ipAddress}</div>
            </div>
        `;
    }

    if (bitacora.detalles) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">Detalles:</div>
                <div class="detail-value">${bitacora.detalles}</div>
            </div>
        `;
    }

    if (bitacora.datosAnteriores) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">Datos Anteriores:</div>
                <div class="detail-value">${JSON.stringify(JSON.parse(bitacora.datosAnteriores), null, 2)}</div>
            </div>
        `;
    }

    if (bitacora.datosNuevos) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">Datos Nuevos:</div>
                <div class="detail-value">${JSON.stringify(JSON.parse(bitacora.datosNuevos), null, 2)}</div>
            </div>
        `;
    }

    if (bitacora.detallesCambios) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">Cambios Específicos:</div>
                <div class="detail-value">${bitacora.detallesCambios}</div>
            </div>
        `;
    }

    modalBody.innerHTML = detallesHTML;
    modal.show();
}

// Cargar resumen estadístico
async function cargarResumen() {
    const resumenContainer = document.getElementById('resumenContainer');

    try {
        const response = await fetch(`http://localhost:5135/api/bitacora/resumen?dias=30`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(`Error: ${response.status}`);
        }

        const resultado = await response.json();
        const resumen = resultado.data || [];

        let html = `
            <div class="row mb-4">
                <div class="col-md-6">
                    <p class="text-muted">
                        <strong>Período:</strong> ${resultado.fechaInicio} a ${resultado.fechaFin}<br>
                        <strong>Total de acciones registradas:</strong> ${resultado.totalRegistros}
                    </p>
                </div>
            </div>
        `;

        if (resumen.length === 0) {
            html += `
                <div class="alert alert-info">
                    No hay datos de resumen disponibles para el período seleccionado
                </div>
            `;
        } else {
            html += `
                <div class="table-responsive">
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Tabla</th>
                                <th>Acción</th>
                                <th>Total Acciones</th>
                                <th>Usuarios Involucrados</th>
                                <th>Registros Afectados</th>
                                <th>Primera Acción</th>
                                <th>Última Acción</th>
                            </tr>
                        </thead>
                        <tbody>
            `;

            resumen.forEach(item => {
                const primeraAccion = new Date(item.primeraAccion).toLocaleString('es-ES');
                const ultimaAccion = new Date(item.ultimaAccion).toLocaleString('es-ES');

                html += `
                    <tr>
                        <td><strong>${item.tabla}</strong></td>
                        <td><span class="action-badge ${obtenerClaseAccion(item.accion)}">${item.accion}</span></td>
                        <td><span class="badge bg-primary">${item.totalAcciones}</span></td>
                        <td><span class="badge bg-info">${item.usuariosInvolucrados}</span></td>
                        <td><span class="badge bg-secondary">${item.registrosAfectados}</span></td>
                        <td><small>${primeraAccion}</small></td>
                        <td><small>${ultimaAccion}</small></td>
                    </tr>
                `;
            });

            html += `
                        </tbody>
                    </table>
                </div>
            `;
        }

        resumenContainer.innerHTML = html;
    } catch (error) {
        console.error('Error al cargar resumen:', error);
        resumenContainer.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <strong>Error:</strong> No se pudo cargar el resumen. ${error.message}
            </div>
        `;
    }
}

// Limpiar filtros
function limpiarFiltros() {
    document.getElementById('filterDias').value = '30';
    document.getElementById('filterTabla').value = '';
    document.getElementById('filterAccion').value = '';
    cargarBitacoras();
}

// Exportar bitácora a CSV
function exportarACSV() {
    if (bitacorasActuales.length === 0) {
        alert('No hay registros para exportar');
        return;
    }

    let csv = 'ID,Fecha,Usuario,Rol,Acción,Tabla,Registro ID,Detalles,IP\n';

    bitacorasActuales.forEach(b => {
        const fecha = new Date(b.fechaAccion).toLocaleString('es-ES');
        const usuario = b.usuario?.nombre || 'Sistema';
        const rol = b.usuario?.rol || '';
        const detalles = (b.detalles || '').replace(/"/g, '""');
        const ip = b.ipAddress || 'N/A';

        csv += `${b.id},"${fecha}","${usuario}","${rol}","${b.accion}","${b.tabla}",${b.registroId},"${detalles}","${ip}"\n`;
    });

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `bitacora_${new Date().toISOString().slice(0, 10)}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
