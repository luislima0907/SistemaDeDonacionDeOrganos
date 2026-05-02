let bitacorasActuales = [];
let paginaActual = 1;
const registrosPorPagina = 10;
let totalPaginasBackend = 1;
let usingBackendPagination = false;

// Cargar bitácoras al abrir la página
document.addEventListener('DOMContentLoaded', function() {
    cargarOpcionesDisponibles();
    conectarFiltros();
    cargarBitacoras();
    
    // Cargar resumen cuando se hace clic en la pestaña
    document.getElementById('resumen-tab').addEventListener('click', function() {
        cargarResumen();
    });
});

// Función para cargar las opciones disponibles de tablas y acciones
async function cargarOpcionesDisponibles() {
    try {
        const response = await fetch('/api/bitacora/opciones', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });

        if (!response.ok) {
            console.error('Error al cargar opciones:', response.status);
            return;
        }

        const resultado = await response.json();
        const tablas = resultado.tablas || [];
        const acciones = resultado.acciones || [];

        // Poblar selector de tablas
        const selectTabla = document.getElementById('filterTabla');
        tablas.forEach(tabla => {
            const option = document.createElement('option');
            option.value = tabla;
            option.textContent = tabla;
            selectTabla.appendChild(option);
        });

        // Poblar selector de acciones
        const selectAccion = document.getElementById('filterAccion');
        acciones.forEach(accion => {
            const option = document.createElement('option');
            option.value = accion;
            option.textContent = accion;
            selectAccion.appendChild(option);
        });

    } catch (error) {
        console.error('Error al cargar opciones disponibles:', error);
    }
}

function conectarFiltros() {
    const diasSelect = document.getElementById('filterDias');
    const customRange = document.getElementById('customDateRange');
    const applyBtn = document.getElementById('applyFilters');
    const clearBtn = document.getElementById('clearFilters');

    diasSelect.addEventListener('change', function() {
        if (this.value === 'custom') {
            customRange.style.display = 'grid';
        } else {
            customRange.style.display = 'none';
        }
    });

    applyBtn.addEventListener('click', function() {
        aplicarFiltros();
    });

    clearBtn.addEventListener('click', function() {
        limpiarFiltros();
    });
}

// Función para cargar bitácoras
async function cargarBitacoras(pagina = 1) {
    const loadingDiv = document.getElementById('bitacoraLoading');
    const contentDiv = document.getElementById('bitacoraTableContent');
    const emptyState = document.getElementById('emptyState');

    loadingDiv.classList.remove('d-none');
    contentDiv.innerHTML = '';
    emptyState.classList.add('d-none');
    paginaActual = pagina;

    try {
        const diasVal = document.getElementById('filterDias')?.value || '30';
        const tablaVal = document.getElementById('filterTabla')?.value?.trim() || '';
        const accionVal = document.getElementById('filterAccion')?.value?.trim() || '';

        let url = '';
        usingBackendPagination = false;

        if (diasVal === 'custom') {
            // Usar endpoint filtrada con fechas concretas
            const fechaInicio = document.getElementById('filterFechaInicio')?.value;
            const fechaFin = document.getElementById('filterFechaFin')?.value;

            // Validaciones básicas
            if (!fechaInicio || !fechaFin) {
                alert('Por favor seleccione fecha inicio y fecha fin para el rango personalizado.');
                return;
            }

            // Construir query para /api/bitacora/filtrada
            url = `/api/bitacora/filtrada?fechaInicio=${encodeURIComponent(fechaInicio)}&fechaFin=${encodeURIComponent(fechaFin)}&pagina=${pagina}&pageSize=${registrosPorPagina}`;
            if (tablaVal) url += `&tabla=${encodeURIComponent(tablaVal)}`;
            if (accionVal) url += `&accion=${encodeURIComponent(accionVal)}`;
            usingBackendPagination = true;
        } else {
            // Usar endpoint todas con dias
            url = `/api/bitacora/todas?dias=${encodeURIComponent(diasVal)}`;
            if (tablaVal) url += `&tabla=${encodeURIComponent(tablaVal)}`;
            if (accionVal) url += `&accion=${encodeURIComponent(accionVal)}`;
            usingBackendPagination = false; // client-side pagination
        }

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

        if (usingBackendPagination) {
            bitacorasActuales = resultado.data || [];
            // Backend nos da info de paginado
            totalPaginasBackend = resultado.totalPaginas || 1;
            paginaActual = resultado.pagina || 1;

            // Actualizar estadísticas usando conteos devueltos
            document.getElementById('statRegistrar').textContent = resultado.totalRegistrar ?? '-';
            document.getElementById('statActualizar').textContent = resultado.totalActualizar ?? '-';
            document.getElementById('statEliminar').textContent = resultado.totalEliminar ?? '-';
            document.getElementById('statTotal').textContent = resultado.total ?? bitacorasActuales.length;
        } else {
            // Cuando usamos /todas devolvemos lista completa -> paginar en cliente
            bitacorasActuales = resultado.data || [];
            
            // Actualizar estadísticas usando conteos devueltos del backend
            document.getElementById('statRegistrar').textContent = resultado.totalRegistrar ?? '-';
            document.getElementById('statActualizar').textContent = resultado.totalActualizar ?? '-';
            document.getElementById('statEliminar').textContent = resultado.totalEliminar ?? '-';
            document.getElementById('statTotal').textContent = resultado.total ?? bitacorasActuales.length;
        }

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
    const accionesUnicas = [...new Set(bitacorasActuales.map(b => b.accion || b.Accion))];
    console.log('Acciones encontradas en la BD:', accionesUnicas);
    console.log('Total de registros:', bitacorasActuales.length);
    console.log('Datos completos:', bitacorasActuales);

    // Contar con búsqueda insensible a mayúsculas y espacios
    const crear = bitacorasActuales.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('crear') || accion.includes('registrar');
    }).length;
    
    const actualizar = bitacorasActuales.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('actualizar');
    }).length;
    
    const eliminar = bitacorasActuales.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('eliminar');
    }).length;
    
    const total = bitacorasActuales.length;

    console.log('Estadísticas calculadas:', { crear, actualizar, eliminar, total });

    const statCrear = document.getElementById('statRegistrar');
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

    // Si backend maneja paginado, mostramos lo que vino
    if (usingBackendPagination) {
        renderTabla(bitacorasActuales);
        actualizarPaginacionBackend();
        return;
    }

    // Paginación cliente
    const inicio = (paginaActual - 1) * registrosPorPagina;
    const fin = inicio + registrosPorPagina;
    const bitacorasPagina = bitacorasActuales.slice(inicio, fin);

    if (bitacorasPagina.length === 0) {
        contentDiv.innerHTML = '';
        return;
    }

    renderTabla(bitacorasPagina);
    // Mostrar paginación si hay más registros
    actualizarPaginacion();
}

function renderTabla(bitacorasLista) {
    const contentDiv = document.getElementById('bitacoraTableContent');
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

    bitacorasLista.forEach(bitacora => {
        const fecha = new Date(bitacora.fechaAccion || bitacora.FechaAccion).toLocaleString('es-ES');
        const accionClass = obtenerClaseAccion(bitacora.accion || bitacora.Accion);
        const usuario = (bitacora.usuario?.nombre) || bitacora.NombreUsuario || 'Sistema';
        const rol = (bitacora.usuario?.rol) || bitacora.RolUsuario || '';

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
                    <span class="action-badge ${accionClass}">${bitacora.accion || bitacora.Accion}</span>
                </td>
                <td>
                    <small>${bitacora.tabla || bitacora.Tabla}</small>
                </td>
                <td>
                    <code>#${bitacora.registroId || bitacora.RegistroId}</code>
                </td>
            </tr>
        `;
    });

    html += `
            </tbody>
        </table>
    `;

    contentDiv.innerHTML = html;
}

// Actualizar controles de paginación (cliente)
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

// Actualizar paginación cuando backend controla paginado
function actualizarPaginacionBackend() {
    const paginationContainer = document.getElementById('paginationContainer');
    if (totalPaginasBackend > 1) {
        paginationContainer.classList.remove('d-none');
        document.getElementById('paginationInfo').textContent = 
            `Página ${paginaActual} de ${totalPaginasBackend}`;
    } else {
        paginationContainer.classList.add('d-none');
    }
}

// Navegar a página anterior
function paginaAnterior() {
    if (usingBackendPagination) {
        if (paginaActual > 1) {
            cargarBitacoras(paginaActual - 1);
            window.scrollTo(0, 0);
        }
        return;
    }

    if (paginaActual > 1) {
        paginaActual--;
        mostrarTabla();
        window.scrollTo(0, 0);
    }
}

// Navegar a página siguiente
function paginaSiguiente() {
    if (usingBackendPagination) {
        if (paginaActual < totalPaginasBackend) {
            cargarBitacoras(paginaActual + 1);
            window.scrollTo(0, 0);
        }
        return;
    }

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
        'Registrar': 'create',
        'Actualizar': 'update',
        'Eliminar': 'delete',
        'Consulta': 'consulta',
        'Consultar Ranking Prioridad': 'consulta'
    };
    return mapa[accion] || 'consulta';
}

// Mostrar detalles en modal
async function mostrarDetalles(bitacoraId) {
    const bitacora = bitacorasActuales.find(b => b.id === bitacoraId || b.Id === bitacoraId);
    if (!bitacora) return;

    const modalBody = document.getElementById('modalBody');
    const modal = new bootstrap.Modal(document.getElementById('detallesModal'));

    let detallesHTML = `
        <div class="detail-row">
            <div class="detail-label">ID:</div>
            <div class="detail-value">${bitacora.id || bitacora.Id}</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Fecha/Hora:</div>
            <div class="detail-value">${new Date(bitacora.fechaAccion || bitacora.FechaAccion).toLocaleString('es-ES')}</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Usuario:</div>
            <div class="detail-value">${bitacora.usuario?.nombre || bitacora.NombreUsuario || 'Sistema'} (${bitacora.usuario?.rol || bitacora.RolUsuario || ''})</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Acción:</div>
            <div class="detail-value"><span class="action-badge ${obtenerClaseAccion(bitacora.accion || bitacora.Accion)}">${bitacora.accion || bitacora.Accion}</span></div>
        </div>
        <div class="detail-row">
            <div class="detail-label">Tabla:</div>
            <div class="detail-value">${bitacora.tabla || bitacora.Tabla}</div>
        </div>
        <div class="detail-row">
            <div class="detail-label">ID Registro:</div>
            <div class="detail-value">#${bitacora.registroId || bitacora.RegistroId}</div>
        </div>
    `;

    if (bitacora.ipAddress || bitacora.IPAddress) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">IP Address:</div>
                <div class="detail-value">${bitacora.ipAddress || bitacora.IPAddress}</div>
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

    if (bitacora.datosAnteriores || bitacora.DatosAnteriores) {
        try {
            const prev = JSON.stringify(JSON.parse(bitacora.datosAnteriores || bitacora.DatosAnteriores), null, 2);
            detallesHTML += `
                <div class="detail-row">
                    <div class="detail-label">Datos Anteriores:</div>
                    <div class="detail-value">${prev}</div>
                </div>
            `;
        } catch (e) {
            detallesHTML += `
                <div class="detail-row">
                    <div class="detail-label">Datos Anteriores:</div>
                    <div class="detail-value">${bitacora.datosAnteriores || bitacora.DatosAnteriores}</div>
                </div>
            `;
        }
    }

    if (bitacora.datosNuevos || bitacora.DatosNuevos) {
        try {
            const nuevo = JSON.stringify(JSON.parse(bitacora.datosNuevos || bitacora.DatosNuevos), null, 2);
            detallesHTML += `
                <div class="detail-row">
                    <div class="detail-label">Datos Nuevos:</div>
                    <div class="detail-value">${nuevo}</div>
                </div>
            `;
        } catch (e) {
            detallesHTML += `
                <div class="detail-row">
                    <div class="detail-label">Datos Nuevos:</div>
                    <div class="detail-value">${bitacora.datosNuevos || bitacora.DatosNuevos}</div>
                </div>
            `;
        }
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
        const response = await fetch(`/api/bitacora/resumen?dias=30`, {
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
    document.getElementById('filterFechaInicio').value = '';
    document.getElementById('filterFechaFin').value = '';
    document.getElementById('customDateRange').style.display = 'none';
    cargarBitacoras();
}

// Aplicar filtros (invocado por botón)
function aplicarFiltros() {
    // Reiniciar paginación
    paginaActual = 1;
    cargarBitacoras(1);
}

// Exportar bitácora a CSV
function exportarACSV() {
    if (bitacorasActuales.length === 0) {
        alert('No hay registros para exportar');
        return;
    }

    let csv = 'ID,Fecha,Usuario,Rol,Acción,Tabla,Registro ID,Detalles,IP\n';

    bitacorasActuales.forEach(b => {
        const fecha = new Date(b.fechaAccion || b.FechaAccion).toLocaleString('es-ES');
        const usuario = b.usuario?.nombre || b.NombreUsuario || 'Sistema';
        const rol = b.usuario?.rol || b.RolUsuario || '';
        const detalles = (b.detalles || b.Detalles || '').toString().replace(/"/g, '""');
        const ip = b.ipAddress || b.IPAddress || 'N/A';

        csv += `${b.id || b.Id},"${fecha}","${usuario}","${rol}","${b.accion || b.Accion}","${b.tabla || b.Tabla}",${b.registroId || b.RegistroId},"${detalles}","${ip}"\n`;
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
