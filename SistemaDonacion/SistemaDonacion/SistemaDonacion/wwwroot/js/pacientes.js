// ============ VARIABLES GLOBALES ============
let pacientesData = [];
let pacienteEditando = null;

// ============ AL CARGAR EL DOCUMENTO ============
document.addEventListener('DOMContentLoaded', () => {
    cargarHospitalesPacientes();
    cargarPacientes();
});

// ============ NAVEGACIÓN Y TABS ============

// Función para el botón "Volver" (RF-21)
function regresarAlPanel() {
    const usuarioStr = localStorage.getItem('usuario');
    if (usuarioStr) {
        const usuario = JSON.parse(usuarioStr);
        if (usuario.rol === 'Admin') {
            window.location.href = 'admin.html';
        } else if (usuario.rol === 'Medico') {
            window.location.href = 'panel-medico.html';
        } else {
            window.history.back();
        }
    } else {
        window.history.back();
    }
}

// Cambio de pestañas
function mostrarTab(tabName, event) {
    // Ocultar todos los contenidos
    document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));
    // Desactivar todos los botones
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));

    // Activar lo seleccionado
    document.getElementById(tabName).classList.add('active');
    event.target.classList.add('active');

    if (tabName === 'listar-pacientes') cargarPacientes();
}

// ============ FUNCIONES DE APOYO ============
function mostrarMensaje(texto, tipo = 'info') {
    const div = document.getElementById('message');
    div.innerHTML = `<div class="alert alert-${tipo}">${texto}</div>`;
    setTimeout(() => div.innerHTML = '', 5000);
}

// ============ LLAMADAS A LA API (BACKEND) ============

async function cargarHospitalesPacientes() {
    try {
        let res = await fetch('/api/hospital/mi-hospital', { credentials: 'include' });

        if (res.status === 403 || res.status === 400) {
            res = await fetch('/api/hospital', { credentials: 'include' });
            const hospitales = await res.json();
            const select = document.getElementById('hospitalId');
            select.innerHTML = '<option value="">Seleccionar hospital...</option>';
            hospitales.forEach(h => {
                const opt = document.createElement('option');
                opt.value = h.id;
                opt.textContent = `${h.nombre} (${h.ciudad})`;
                select.appendChild(opt);
            });
        } else if (res.ok) {
            const hospital = await res.json();
            const select = document.getElementById('hospitalId');
            select.innerHTML = `<option value="${hospital.id}" selected>${hospital.nombre}</option>`;
            select.disabled = true;
        }
    } catch (error) {
        mostrarMensaje('Error al cargar hospitales', 'error');
    }
}

async function registrarPaciente(event) {
    event.preventDefault();
    const btn = event.target.querySelector('button');

    // Evitar doble envío (Regla de Negocio)
    if (btn.disabled) return;

    const nombre = document.getElementById('nombre').value.trim();
    const tipoSanguineo = document.getElementById('tipoSanguineo').value;
    const organoRequerido = document.getElementById('organoRequerido').value;
    const nivelUrgencia = document.getElementById('nivelUrgencia').value;
    const hospitalId = parseInt(document.getElementById('hospitalId').value);
    const observaciones = document.getElementById('observaciones').value.trim();

    btn.disabled = true;
    btn.textContent = 'Enviando...';

    try {
        const res = await fetch('/api/paciente', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ nombre, tipoSanguineo, organoRequerido, nivelUrgencia, hospitalId, observaciones })
        });

        if (res.ok) {
            mostrarMensaje('✓ Paciente registrado correctamente', 'success');
            document.getElementById('formPaciente').reset();
            mostrarTab('listar-pacientes', { target: document.querySelectorAll('.tab-btn')[1] });
        } else {
            const data = await res.json();
            mostrarMensaje(data.mensaje || 'Error al registrar', 'error');
        }
    } catch (error) {
        mostrarMensaje('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Registrar Paciente';
    }
}

async function cargarPacientes() {
    try {
        const res = await fetch('/api/paciente', { credentials: 'include' });
        pacientesData = await res.json();
        renderizarTabla();
    } catch (error) {
        document.getElementById('pacientes-container').innerHTML = 'Error al cargar datos';
    }
}

function renderizarTabla() {
    const container = document.getElementById('pacientes-container');
    if (pacientesData.length === 0) {
        container.innerHTML = '<p>No hay pacientes en lista de espera.</p>';
        return;
    }

    let html = `<table><thead><tr>
        <th>ID</th><th>Nombre</th><th>Urgencia</th><th>Estado</th><th>Acciones</th>
    </tr></thead><tbody>`;

    pacientesData.forEach(p => {
        const urgClass = p.nivelUrgencia.toLowerCase(); // alta, media, baja
        html += `
        <tr>
            <td>#${p.id}</td>
            <td>${p.nombre}</td>
            <td><span class="urgencia-badge ${urgClass}">${p.nivelUrgencia}</span></td>
            <td><span class="badge badge-${p.estado.toLowerCase().replace(' ', '-')}">${p.estado}</span></td>
            <td><button class="btn btn-warning btn-sm" onclick="abrirModalEditar(${p.id})">Editar</button></td>
        </tr>`;
    });

    html += '</tbody></table>';
    container.innerHTML = html;
}

// ============ MODAL (EDICIÓN) ============
function abrirModalEditar(id) {
    pacienteEditando = pacientesData.find(p => p.id === id);
    document.getElementById('modalNombre').textContent = pacienteEditando.nombre;
    document.getElementById('modalEstado').value = pacienteEditando.estado;
    document.getElementById('modalUrgencia').value = pacienteEditando.nivelUrgencia;
    document.getElementById('modalEditar').classList.add('active');
}

function cerrarModal() {
    document.getElementById('modalEditar').classList.remove('active');
}

async function guardarCambios() {
    const estado = document.getElementById('modalEstado').value;
    const nivelUrgencia = document.getElementById('modalUrgencia').value;

    try {
        const res = await fetch(`/api/paciente/${pacienteEditando.id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ estado, nivelUrgencia })
        });

        if (res.ok) {
            cerrarModal();
            cargarPacientes();
            mostrarMensaje('Actualizado correctamente', 'success');
        }
    } catch (error) {
        mostrarMensaje('Error al actualizar', 'error');
    }
}


// ============ FUNCIÓN DE NAVEGACIÓN 
function regresarAlPanel() {
    console.log("Intentando navegar de regreso...");

    // Obtenemos el usuario del localStorage
    const usuarioString = localStorage.getItem('usuario');

    if (usuarioString) {
        try {
            const usuario = JSON.parse(usuarioString);

            // Validamos el rol (Admin o Medico)
            if (usuario.rol === 'Admin') {
                window.location.href = 'admin.html';
            } else if (usuario.rol === 'Medico') {
                window.location.href = 'panel-medico.html';
            } else {
                // Si tiene otro rol, mandarlo al dashboard genérico
                window.location.href = 'dashboard.html';
            }
        } catch (e) {
            console.error("Error al leer la sesión:", e);
            window.location.href = 'login.html';
        }
    } else {
        // Si no hay nada en localStorage, mejor mandarlo al login
        console.warn("No hay sesión, enviando al login");
        window.location.href = 'login.html';
    }
}
