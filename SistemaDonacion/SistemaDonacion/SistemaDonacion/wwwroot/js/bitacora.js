let bitacorasActuales = [];
let bitacorasFiltradas = [];
let paginaActual = 1;
let registrosPorPagina = 25;
let totalPaginasBackend = 1;
let usingBackendPagination = false;
let debounceTimer = null;

// Cargar bitácoras al abrir la página
document.addEventListener('DOMContentLoaded', function () {
    cargarOpcionesDisponibles();
    conectarFiltros();
    conectarPageSize();
    conectarCerrarSesion();
    conectarVolverMenu();
    conectarExportarExcel();
    cargarBitacoras();

    const resumenTab = document.getElementById('resumen-tab');

    if (resumenTab) {
        resumenTab.addEventListener('click', function () {
            cargarResumen();
        });
    }
});

// Cargar opciones disponibles para filtros
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

        const selectTabla = document.getElementById('filterTabla');
        const selectAccion = document.getElementById('filterAccion');

        tablas.forEach(tabla => {
            const option = document.createElement('option');
            option.value = tabla;
            option.textContent = tabla;
            selectTabla.appendChild(option);
        });

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

// Conectar filtros
function conectarFiltros() {
    const diasSelect = document.getElementById('filterDias');
    const customRange = document.getElementById('customDateRange');
    const applyBtn = document.getElementById('applyFilters');
    const clearBtn = document.getElementById('clearFilters');
    const busquedaInput = document.getElementById('filterBusqueda');

    diasSelect.addEventListener('change', function () {
        if (this.value === 'custom') {
            customRange.style.display = 'grid';
        } else {
            customRange.style.display = 'none';
        }
    });

    applyBtn.addEventListener('click', function () {
        aplicarFiltros();
    });

    clearBtn.addEventListener('click', function () {
        limpiarFiltros();
    });

    if (busquedaInput) {
        busquedaInput.addEventListener('input', function () {
            clearTimeout(debounceTimer);

            debounceTimer = setTimeout(() => {
                paginaActual = 1;
                filtrarBusquedaLocal();
                mostrarTabla();
                actualizarEstadisticas();
            }, 500);
        });
    }
}

// Conectar selector de registros por página
function conectarPageSize() {
    const pageSizeSelect = document.getElementById('pageSizeSelect');

    if (!pageSizeSelect) return;

    pageSizeSelect.addEventListener('change', function () {
        registrosPorPagina = parseInt(this.value);
        paginaActual = 1;
        mostrarTabla();
    });
}

// Botón fijo de cerrar sesión
function conectarCerrarSesion() {
    const logoutButton = document.getElementById('logoutButton');

    if (!logoutButton) return;

    logoutButton.addEventListener('click', function () {
        localStorage.removeItem('token');
        sessionStorage.clear();
        window.location.href = 'login.html';
    });
}

// Botón superior para volver al menú
function conectarVolverMenu() {
    const backMenuButton = document.getElementById('backMenuButton');

    if (!backMenuButton) return;

    backMenuButton.addEventListener('click', function () {
        window.location.href = 'admin.html';
    });
}

// Cargar bitácoras
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
            const fechaInicio = document.getElementById('filterFechaInicio')?.value;
            const fechaFin = document.getElementById('filterFechaFin')?.value;

            if (!fechaInicio || !fechaFin) {
                alert('Por favor seleccione fecha inicio y fecha fin para el rango personalizado.');
                loadingDiv.classList.add('d-none');
                return;
            }

            url = `/api/bitacora/filtrada?fechaInicio=${encodeURIComponent(fechaInicio)}&fechaFin=${encodeURIComponent(fechaFin)}&pagina=${pagina}&pageSize=${registrosPorPagina}`;

            if (tablaVal) url += `&tabla=${encodeURIComponent(tablaVal)}`;
            if (accionVal) url += `&accion=${encodeURIComponent(accionVal)}`;

            usingBackendPagination = true;
        } else {
            url = `/api/bitacora/todas?dias=${encodeURIComponent(diasVal)}`;

            if (tablaVal) url += `&tabla=${encodeURIComponent(tablaVal)}`;
            if (accionVal) url += `&accion=${encodeURIComponent(accionVal)}`;

            usingBackendPagination = false;
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
            totalPaginasBackend = resultado.totalPaginas || 1;
            paginaActual = resultado.pagina || 1;
        } else {
            bitacorasActuales = resultado.data || [];
        }

        bitacorasFiltradas = [...bitacorasActuales];
        filtrarBusquedaLocal();
        mostrarTabla();
        actualizarEstadisticas();

        if (bitacorasFiltradas.length === 0) {
            emptyState.classList.remove('d-none');
            contentDiv.innerHTML = '';
            document.getElementById('paginationContainer').classList.add('d-none');
        }

    } catch (error) {
        console.error('Error al cargar bitácoras:', error);

        contentDiv.innerHTML = `
            <div class="alert alert-danger m-3" role="alert">
                <strong>Error:</strong> No se pudieron cargar los registros de auditoría. ${error.message}
            </div>
        `;
    } finally {
        loadingDiv.classList.add('d-none');
    }
}

// Búsqueda local por ID o usuario
function filtrarBusquedaLocal() {
    const busqueda = document.getElementById('filterBusqueda')?.value?.trim().toLowerCase() || '';

    if (!busqueda) {
        bitacorasFiltradas = [...bitacorasActuales];
        return;
    }

    bitacorasFiltradas = bitacorasActuales.filter(b => {
        const id = (b.id || b.Id || '').toString().toLowerCase();
        const registroId = (b.registroId || b.RegistroId || '').toString().toLowerCase();
        const usuario = (b.usuario?.nombre || b.NombreUsuario || 'Sistema').toString().toLowerCase();

        return id.includes(busqueda) ||
            registroId.includes(busqueda) ||
            usuario.includes(busqueda);
    });
}

// Actualizar estadísticas
function actualizarEstadisticas() {
    const crear = bitacorasFiltradas.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('crear') || accion.includes('registrar');
    }).length;

    const actualizar = bitacorasFiltradas.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('actualizar');
    }).length;

    const eliminar = bitacorasFiltradas.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('eliminar');
    }).length;

    const consulta = bitacorasFiltradas.filter(b => {
        const accion = (b.accion || b.Accion || '').toString().toLowerCase();
        return accion.includes('consulta') || accion.includes('consultar');
    }).length;

    document.getElementById('statRegistrar').textContent = crear;
    document.getElementById('statActualizar').textContent = actualizar;
    document.getElementById('statEliminar').textContent = eliminar;
    document.getElementById('statConsulta').textContent = consulta;
}

// Mostrar tabla con paginación
function mostrarTabla() {
    const contentDiv = document.getElementById('bitacoraTableContent');
    const emptyState = document.getElementById('emptyState');

    if (bitacorasFiltradas.length === 0) {
        contentDiv.innerHTML = '';
        emptyState.classList.remove('d-none');
        document.getElementById('paginationContainer').classList.add('d-none');
        return;
    }

    emptyState.classList.add('d-none');

    if (usingBackendPagination) {
        renderTabla(bitacorasFiltradas);
        actualizarPaginacionBackend();
        return;
    }

    const inicio = (paginaActual - 1) * registrosPorPagina;
    const fin = inicio + registrosPorPagina;
    const bitacorasPagina = bitacorasFiltradas.slice(inicio, fin);

    renderTabla(bitacorasPagina);
    actualizarPaginacion();
}

// Renderizar tabla
function renderTabla(bitacorasLista) {
    const contentDiv = document.getElementById('bitacoraTableContent');

    let html = `
        <table class="table table-hover mb-0">
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Usuario</th>
                    <th>Acción</th>
                    <th>Entidad</th>
                    <th>Fecha y hora</th>
                    <th>Descripción</th>
                    <th>Detalles</th>
                </tr>
            </thead>
            <tbody>
    `;

    bitacorasLista.forEach(bitacora => {
        const id = bitacora.id || bitacora.Id || '';
        const fecha = formatearFechaLocal(bitacora.fechaAccion || bitacora.FechaAccion);
        const accion = bitacora.accion || bitacora.Accion || 'Consulta';
        const accionClass = obtenerClaseAccion(accion);
        const usuario = bitacora.usuario?.nombre || bitacora.NombreUsuario || 'Sistema';
        const rol = bitacora.usuario?.rol || bitacora.RolUsuario || '';
        const tabla = bitacora.tabla || bitacora.Tabla || 'Sin entidad';
        const registroId = bitacora.registroId || bitacora.RegistroId || '';
        const descripcion = bitacora.detalles || bitacora.Detalles || `Acción realizada sobre el registro ${registroId || 'N/A'}`;

        html += `
            <tr>
                <td><code>#${id}</code></td>

                <td>
                    <div>${usuario}</div>
                    <small class="text-muted">${rol}</small>
                </td>

                <td>
                    <span class="action-badge ${accionClass}">${accion}</span>
                </td>

                <td>
                    <small>${tabla}</small><br>
                    <code>${registroId ? '#' + registroId : 'N/A'}</code>
                </td>

                <td>
                    <small>${fecha}</small>
                </td>

                <td>
                    <small>${descripcion}</small>
                </td>

                <td>
                    <button type="button" class="btn details-btn" onclick="mostrarDetalles(${id})">
                        Detalles
                    </button>
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

// Formatear fecha local
function formatearFechaLocal(fecha) {
    if (!fecha) return 'Sin fecha';

    return new Date(fecha).toLocaleString('es-GT', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
}

// Paginación cliente
function actualizarPaginacion() {
    const totalPaginas = Math.ceil(bitacorasFiltradas.length / registrosPorPagina);
    const paginationContainer = document.getElementById('paginationContainer');
    const paginaActualTexto = document.getElementById('paginaActualTexto');

    if (bitacorasFiltradas.length > 0) {
        paginationContainer.classList.remove('d-none');

        const inicio = ((paginaActual - 1) * registrosPorPagina) + 1;
        const fin = Math.min(paginaActual * registrosPorPagina, bitacorasFiltradas.length);

        document.getElementById('paginationInfo').textContent =
            `Mostrando ${inicio}-${fin} de ${bitacorasFiltradas.length} registros`;

        if (paginaActualTexto) {
            paginaActualTexto.textContent = `Página ${paginaActual} de ${totalPaginas}`;
        }
    } else {
        paginationContainer.classList.add('d-none');
    }
}

// Paginación backend
function actualizarPaginacionBackend() {
    const paginationContainer = document.getElementById('paginationContainer');
    const paginaActualTexto = document.getElementById('paginaActualTexto');

    if (bitacorasFiltradas.length > 0) {
        paginationContainer.classList.remove('d-none');

        document.getElementById('paginationInfo').textContent =
            `Mostrando ${bitacorasFiltradas.length} registros de la página actual`;

        if (paginaActualTexto) {
            paginaActualTexto.textContent = `Página ${paginaActual} de ${totalPaginasBackend}`;
        }
    } else {
        paginationContainer.classList.add('d-none');
    }
}

// Página anterior
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

// Página siguiente
function paginaSiguiente() {
    if (usingBackendPagination) {
        if (paginaActual < totalPaginasBackend) {
            cargarBitacoras(paginaActual + 1);
            window.scrollTo(0, 0);
        }
        return;
    }

    const totalPaginas = Math.ceil(bitacorasFiltradas.length / registrosPorPagina);

    if (paginaActual < totalPaginas) {
        paginaActual++;
        mostrarTabla();
        window.scrollTo(0, 0);
    }
}

// Clase CSS según acción
function obtenerClaseAccion(accion) {
    const texto = (accion || '').toString().toLowerCase();

    if (texto.includes('crear') || texto.includes('registrar')) return 'create';
    if (texto.includes('actualizar')) return 'update';
    if (texto.includes('eliminar')) return 'delete';
    if (texto.includes('consulta') || texto.includes('consultar')) return 'consulta';

    return 'consulta';
}

// Mostrar detalles en modal
function mostrarDetalles(bitacoraId) {
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
            <div class="detail-value">${formatearFechaLocal(bitacora.fechaAccion || bitacora.FechaAccion)}</div>
        </div>

        <div class="detail-row">
            <div class="detail-label">Usuario:</div>
            <div class="detail-value">${bitacora.usuario?.nombre || bitacora.NombreUsuario || 'Sistema'} (${bitacora.usuario?.rol || bitacora.RolUsuario || ''})</div>
        </div>

        <div class="detail-row">
            <div class="detail-label">Acción:</div>
            <div class="detail-value">
                <span class="action-badge ${obtenerClaseAccion(bitacora.accion || bitacora.Accion)}">
                    ${bitacora.accion || bitacora.Accion}
                </span>
            </div>
        </div>

        <div class="detail-row">
            <div class="detail-label">Entidad:</div>
            <div class="detail-value">${bitacora.tabla || bitacora.Tabla || 'Sin entidad'}</div>
        </div>

        <div class="detail-row">
            <div class="detail-label">ID Registro:</div>
            <div class="detail-value">#${bitacora.registroId || bitacora.RegistroId || 'N/A'}</div>
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

    if (bitacora.detalles || bitacora.Detalles) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">Detalles:</div>
                <div class="detail-value">${bitacora.detalles || bitacora.Detalles}</div>
            </div>
        `;
    }

    if (bitacora.datosAnteriores || bitacora.DatosAnteriores) {
        detallesHTML += crearFilaJson('Datos Anteriores:', bitacora.datosAnteriores || bitacora.DatosAnteriores);
    }

    if (bitacora.datosNuevos || bitacora.DatosNuevos) {
        detallesHTML += crearFilaJson('Datos Nuevos:', bitacora.datosNuevos || bitacora.DatosNuevos);
    }

    if (bitacora.detallesCambios || bitacora.DetallesCambios) {
        detallesHTML += `
            <div class="detail-row">
                <div class="detail-label">Cambios Específicos:</div>
                <div class="detail-value">${bitacora.detallesCambios || bitacora.DetallesCambios}</div>
            </div>
        `;
    }

    modalBody.innerHTML = detallesHTML;
    modal.show();
}

// Crear fila con JSON formateado
function crearFilaJson(label, valor) {
    let contenido = valor;

    try {
        contenido = JSON.stringify(JSON.parse(valor), null, 2);
    } catch (e) {
        contenido = valor;
    }

    return `
        <div class="detail-row">
            <div class="detail-label">${label}</div>
            <div class="detail-value">${contenido}</div>
        </div>
    `;
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
                                <th>Entidad</th>
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
                const primeraAccion = formatearFechaLocal(item.primeraAccion);
                const ultimaAccion = formatearFechaLocal(item.ultimaAccion);

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
    document.getElementById('filterBusqueda').value = '';
    document.getElementById('customDateRange').style.display = 'none';

    paginaActual = 1;
    cargarBitacoras(1);
}

// Aplicar filtros
function aplicarFiltros() {
    paginaActual = 1;
    cargarBitacoras(1);
}

// Exportar bitácora a CSV
function exportarACSV() {
    if (bitacorasFiltradas.length === 0) {
        alert('No hay registros para exportar');
        return;
    }

    let csv = 'ID,Fecha,Usuario,Rol,Acción,Entidad,Registro ID,Detalles,IP\n';

    bitacorasFiltradas.forEach(b => {
        const fecha = formatearFechaLocal(b.fechaAccion || b.FechaAccion);
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

// ============================================
// FUNCIONALIDAD DE EXPORTACIÓN A EXCEL
// ============================================

// Conectar evento del botón de exportación a Excel
function conectarExportarExcel() {
    const exportBtn = document.getElementById('exportarExcelBtn');
    
    if (!exportBtn) return;

    exportBtn.addEventListener('click', function() {
        exportarBitacoraAExcel();
    });
}

// Función principal de exportación a Excel
async function exportarBitacoraAExcel() {
    const exportBtn = document.getElementById('exportarExcelBtn');
    const exportMessage = document.getElementById('exportMessage');
    
    // Validar que hay registros
    if (bitacorasFiltradas.length === 0) {
        mostrarMensajeExportacion('No hay registros para exportar', 'error');
        return;
    }

    // Deshabilitar botón y mostrar spinner
    exportBtn.disabled = true;
    const spinner = exportBtn.querySelector('.export-spinner');
    const text = exportBtn.querySelector('.export-text');
    
    spinner.classList.remove('d-none');
    text.textContent = 'Generando...';

    try {
        // Recopilar filtros actuales
        const filtros = recopilarFiltrosActuales();

        // Enviar solicitud al backend
        const response = await fetch('/api/bitacora/exportar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(filtros)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || `Error ${response.status}: No se pudo generar el archivo`);
        }

        // Obtener el archivo
        const blob = await response.blob();
        
        // Crear nombre del archivo
        const nombreArchivo = generarNombreArchivoExcel(filtros);
        
        // Descargar el archivo
        descargarArchivo(blob, nombreArchivo);

        // Mostrar mensaje de éxito
        const recordCount = bitacorasFiltradas.length;
        mostrarMensajeExportacion(
            `✓ Archivo descargado correctamente (${recordCount} registros)`,
            'success'
        );

        // Limpiar después de 3 segundos
        setTimeout(() => {
            limpiarEstadoExportacion();
        }, 3000);

    } catch (error) {
        console.error('Error en exportación:', error);
        mostrarMensajeExportacion(
            `⚠️ Error al exportar: ${error.message}`,
            'error'
        );
        limpiarEstadoExportacion();
    }
}

// Recopilar los filtros actuales
function recopilarFiltrosActuales() {
    const diasSelect = document.getElementById('filterDias');
    const tablaSelect = document.getElementById('filterTabla');
    const accionSelect = document.getElementById('filterAccion');
    const fechaInicio = document.getElementById('filterFechaInicio');
    const fechaFin = document.getElementById('filterFechaFin');

    const dias = parseInt(diasSelect?.value || '30');
    const tabla = tablaSelect?.value?.trim() || null;
    const accion = accionSelect?.value?.trim() || null;
    
    let filtrosEnvio = {
        dias: dias !== -1 ? dias : null,
        tabla: tabla,
        accion: accion,
        exportarSoloResultadosVisibles: false
    };

    // Si es rango personalizado, agregar fechas
    if (dias === -1 || diasSelect?.value === 'custom') {
        const inicio = fechaInicio?.value;
        const fin = fechaFin?.value;
        
        if (inicio && fin) {
            filtrosEnvio.fechaInicio = inicio;
            filtrosEnvio.fechaFin = fin;
            filtrosEnvio.dias = null;
        }
    }

    return filtrosEnvio;
}

// Generar nombre del archivo Excel
function generarNombreArchivoExcel(filtros) {
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[-:]/g, '').replace('T', '_');
    const hasFilters = filtros.tabla || filtros.accion;

    if (hasFilters && filtros.tabla) {
        return `bitacora_filtrada_${filtros.tabla}_${timestamp}.xlsx`;
    }

    if (hasFilters) {
        return `bitacora_filtrada_${timestamp}.xlsx`;
    }

    return `bitacora_auditoria_${timestamp}.xlsx`;
}

// Descargar archivo
function descargarArchivo(blob, nombreArchivo) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombreArchivo;
    link.style.display = 'none';

    document.body.appendChild(link);
    link.click();
    
    setTimeout(() => {
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }, 100);
}

// Mostrar mensaje de exportación
function mostrarMensajeExportacion(mensaje, tipo) {
    const messageDiv = document.getElementById('exportMessage');
    
    if (!messageDiv) return;

    messageDiv.textContent = mensaje;
    messageDiv.className = `export-message ${tipo}`;
    messageDiv.classList.remove('d-none');
}

// Limpiar estado del botón de exportación
function limpiarEstadoExportacion() {
    const exportBtn = document.getElementById('exportarExcelBtn');
    const spinner = exportBtn?.querySelector('.export-spinner');
    const text = exportBtn?.querySelector('.export-text');
    const messageDiv = document.getElementById('exportMessage');

    if (exportBtn) {
        exportBtn.disabled = false;
    }
    
    if (spinner) {
        spinner.classList.add('d-none');
    }
    
    if (text) {
        text.textContent = 'Exportar a Excel';
    }

    // Ocultar mensaje después de animación
    if (messageDiv && !messageDiv.classList.contains('d-none')) {
        setTimeout(() => {
            messageDiv.classList.add('d-none');
        }, 3000);
    }
}

// Actualizar estado del botón según registros disponibles
function actualizarEstadoBotonExportacion() {
    const exportBtn = document.getElementById('exportarExcelBtn');
    
    if (!exportBtn) return;

    const tieneRegistros = bitacorasFiltradas.length > 0;
    
    exportBtn.disabled = !tieneRegistros;
    
    if (!tieneRegistros) {
        exportBtn.title = 'No hay registros para exportar';
    } else {
        exportBtn.title = `Exportar ${bitacorasFiltradas.length} registros a Excel`;
    }
}
