let pacientesData = [];
let pacienteEditando = null;

document.addEventListener('DOMContentLoaded', () => {
  cargarHospitalesPacientes();
  cargarPacientes();
});

function mostrarTab(tabName, event) {
  document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.getElementById(tabName).classList.add('active');
  event.target.classList.add('active');
  if (tabName === 'listar-pacientes') cargarPacientes();
}

function mostrarMensaje(texto, tipo = 'info') {
  const div = document.getElementById('message');
  div.innerHTML = `<div class="alert alert-${tipo}">${texto}</div>`;
  setTimeout(() => div.innerHTML = '', 5000);
}

async function cargarHospitalesPacientes() {
  try {
    // Primero intentar cargar el hospital del usuario autenticado
    let res = await fetch('/api/hospital/mi-hospital', { credentials: 'include' });
    
    if (res.status === 403 || res.status === 400) {
      // Si es administrador, cargar todos los hospitales
      res = await fetch('/api/hospital', { credentials: 'include' });
      if (!res.ok) throw new Error();
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
      // Cargar solo el hospital del usuario
      const hospital = await res.json();
      const select = document.getElementById('hospitalId');
      select.innerHTML = '';
      const opt = document.createElement('option');
      opt.value = hospital.id;
      opt.textContent = `${hospital.nombre} (${hospital.ciudad})`;
      opt.selected = true;
      select.appendChild(opt);
      // Deshabilitar el select para que no puedan cambiar de hospital
      select.disabled = true;
    } else {
      throw new Error();
    }
  } catch {
    mostrarMensaje('Error al cargar hospitales', 'error');
    const select = document.getElementById('hospitalId');
    select.innerHTML = '<option value="">Error al cargar hospitales</option>';
  }
}

async function registrarPaciente(event) {
  event.preventDefault();

  const nombre = document.getElementById('nombre').value.trim();
  const tipoSanguineo = document.getElementById('tipoSanguineo').value;
  const organoRequerido = document.getElementById('organoRequerido').value;
  const nivelUrgencia = document.getElementById('nivelUrgencia').value;
  const hospitalId = parseInt(document.getElementById('hospitalId').value);
  const observaciones = document.getElementById('observaciones').value.trim();

  if (!nombre || !tipoSanguineo || !organoRequerido || !nivelUrgencia || !hospitalId) {
    mostrarMensaje('Debe completar todos los campos obligatorios', 'error');
    return;
  }

  const btn = event.target.querySelector('button');
  btn.disabled = true;
  btn.textContent = 'Registrando...';

  try {
    const res = await fetch('/api/paciente', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ nombre, tipoSanguineo, organoRequerido, nivelUrgencia, hospitalId, observaciones: observaciones || null })
    });

    const data = await res.json();

    if (res.ok) {
      mostrarMensaje('✓ Paciente registrado correctamente', 'success');
      document.getElementById('formPaciente').reset();
      cargarPacientes();
    } else {
      mostrarMensaje(data.mensaje || 'No se pudo registrar el paciente, intente nuevamente', 'error');
    }
  } catch {
    mostrarMensaje('No se pudo registrar el paciente, intente nuevamente', 'error');
  } finally {
    btn.disabled = false;
    btn.textContent = 'Registrar Paciente';
  }
}

async function cargarPacientes() {
  try {
    const res = await fetch('/api/paciente', { credentials: 'include' });
    if (!res.ok) throw new Error();
    pacientesData = await res.json();
    mostrarTablaPacientes();
  } catch {
    document.getElementById('pacientes-container').innerHTML =
      '<div class="alert alert-error">Error al cargar pacientes</div>';
  }
}

function mostrarTablaPacientes() {
  const container = document.getElementById('pacientes-container');

  if (pacientesData.length === 0) {
    container.innerHTML = '<p style="text-align:center;color:#999;">No hay pacientes registrados</p>';
    return;
  }

  let html = `
    <table>
      <thead>
        <tr>
          <th>ID</th><th>Nombre</th><th>Tipo Sangre</th>
          <th>Órgano Requerido</th><th>Urgencia</th>
          <th>Hospital</th><th>Estado</th><th>Acciones</th>
        </tr>
      </thead><tbody>
  `;

  pacientesData.forEach(p => {
    const badgeClass = `badge-${p.estado.toLowerCase()}`;
    const estadoClass = p.estado.toLowerCase().replace(/\s+/g, '-');
    const badge = `<span class="badge badge-${estadoClass}">${p.estado}</span>`;
    const urgenciaClass = `urgencia-${p.nivelUrgencia.toLowerCase()}`;
    const hospital = p.hospital ? p.hospital.nombre : `Hospital #${p.hospitalId}`;
    
    // Desactivar edición para pacientes trasplantados o fallecidos (visual)
    const isInactive = p.estado === 'Trasplantado' || p.estado === 'Fallecido';
    const rowStyle = isInactive ? 'style="opacity:0.6;"' : '';

    html += `
      <tr ${rowStyle}>
        <td>#${p.id}</td>
        <td>${p.nombre}</td>
        <td><strong>${p.tipoSanguineo}</strong></td>
        <td>${p.organoRequerido}</td>
        <td><span class="${urgenciaClass}">${p.nivelUrgencia}</span></td>
        <td>${hospital}</td>
        <td>${badge}</td>
        <td>
          <div class="action-buttons">
            <button class="btn btn-warning btn-sm" onclick="abrirModalEditar(${p.id})">Editar</button>
          </div>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table>';
  container.innerHTML = html;
}

function abrirModalEditar(pacienteId) {
  const paciente = pacientesData.find(p => p.id === pacienteId);
  if (!paciente) return;

  pacienteEditando = paciente;
  document.getElementById('modalNombre').textContent = paciente.nombre;
  document.getElementById('modalEstado').value = paciente.estado;
  document.getElementById('modalUrgencia').value = paciente.nivelUrgencia;

  document.getElementById('modalEditar').classList.add('active');
}

function cerrarModal() {
  document.getElementById('modalEditar').classList.remove('active');
  pacienteEditando = null;
}

async function guardarCambios() {
  if (!pacienteEditando) return;

  const estado = document.getElementById('modalEstado').value.trim();
  const nivelUrgencia = document.getElementById('modalUrgencia').value.trim();

  if (!estado || !nivelUrgencia) {
    mostrarMensaje('Debe seleccionar estado y urgencia', 'error');
    return;
  }

  const btn = document.getElementById('btnGuardar');
  btn.disabled = true;
  btn.textContent = 'Guardando...';

  try {
    const res = await fetch(`/api/paciente/${pacienteEditando.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ estado, nivelUrgencia })
    });

    const data = await res.json();

    if (res.ok) {
      mostrarMensaje('✓ Paciente actualizado correctamente', 'success');
      cerrarModal();
      cargarPacientes();
    } else {
      mostrarMensaje(data.mensaje || 'No se pudo actualizar el paciente, intente nuevamente', 'error');
    }
  } catch {
    mostrarMensaje('No se pudo actualizar el paciente, intente nuevamente', 'error');
  } finally {
    btn.disabled = false;
    btn.textContent = 'Guardar';
  }
}

// Cerrar modal al hacer click fuera
document.addEventListener('click', (e) => {
  const modal = document.getElementById('modalEditar');
  if (e.target === modal) {
    cerrarModal();
  }
});
