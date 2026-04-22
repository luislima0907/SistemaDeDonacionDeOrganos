let pacientesData = [];

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
    const res = await fetch('/api/hospital', { credentials: 'include' });
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
  } catch {
    mostrarMensaje('Error al cargar hospitales', 'error');
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
          <th>Hospital</th><th>Estado</th><th>Fecha</th>
        </tr>
      </thead><tbody>
  `;

  pacientesData.forEach(p => {
    const badge = `<span class="badge badge-${p.estado.toLowerCase()}">${p.estado}</span>`;
    const urgenciaClass = `urgencia-${p.nivelUrgencia.toLowerCase()}`;
    const fecha = new Date(p.fechaRegistro).toLocaleDateString('es-ES');
    const hospital = p.hospital ? p.hospital.nombre : `Hospital #${p.hospitalId}`;

    html += `
      <tr>
        <td>#${p.id}</td>
        <td>${p.nombre}</td>
        <td><strong>${p.tipoSanguineo}</strong></td>
        <td>${p.organoRequerido}</td>
        <td><span class="${urgenciaClass}">${p.nivelUrgencia}</span></td>
        <td>${hospital}</td>
        <td>${badge}</td>
        <td>${fecha}</td>
      </tr>
    `;
  });

  html += '</tbody></table>';
  container.innerHTML = html;
}