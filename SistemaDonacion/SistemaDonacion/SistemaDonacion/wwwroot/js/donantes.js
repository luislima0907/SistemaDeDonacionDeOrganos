// Variables globales
let donantesData = [];
let organosData = [];
let hospitalesData = [];
let modalData = {
  tipo: null,
  id: null,
  estado: null
};
let confirmacionData = {
  tipo: null,
  id: null
};

// Al cargar la página
document.addEventListener('DOMContentLoaded', () => {
  cargarHospitales();
  cargarDonantesParaSelect();
  cargarDonantes();
  cargarOrganos();
});

// ============ FUNCIONES DE TAB ============
function mostrarTab(tabName) {
  // Ocultar todos los tabs
  const tabs = document.querySelectorAll('.tab-content');
  tabs.forEach(tab => tab.classList.remove('active'));

  // Ocultar todos los botones activos
  const btns = document.querySelectorAll('.tab-btn');
  btns.forEach(btn => btn.classList.remove('active'));

  // Mostrar el tab seleccionado
  document.getElementById(tabName).classList.add('active');
  
  // Marcar botón como activo
  event.target.classList.add('active');

  // Recargar datos si es necesario
  if (tabName === 'listar-donantes') {
    cargarDonantes();
  } else if (tabName === 'listar-organos') {
    cargarOrganos();
  }
}

// ============ FUNCIONES DE MENSAJES ============
function mostrarMensaje(texto, tipo = 'info') {
  const messageDiv = document.getElementById('message');
  messageDiv.innerHTML = `<div class="alert alert-${tipo}">${texto}</div>`;
  
  // Auto-limpiar después de 5 segundos
  setTimeout(() => {
    messageDiv.innerHTML = '';
  }, 5000);
}

// ============ FUNCIONES DE HOSPITALES ============

// Cargar hospitales desde API
async function cargarHospitales() {
  try {
    // Primero intentar cargar el hospital del usuario autenticado
    let res = await fetch('/api/hospital/mi-hospital', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    });

    if (res.status === 403 || res.status === 400) {
      // Si es administrador, cargar todos los hospitales
      res = await fetch('/api/hospital', {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include'
      });

      if (!res.ok) throw new Error('Error al cargar hospitales');

      hospitalesData = await res.json();
      actualizarSelectHospitales();
    } else if (res.ok) {
      // Cargar solo el hospital del usuario
      const hospital = await res.json();
      hospitalesData = [hospital];
      actualizarSelectHospitales();
      // Deshabilitar el select para que no puedan cambiar de hospital
      document.getElementById('hospitalId').disabled = true;
    } else {
      throw new Error('Error al cargar hospitales');
    }
  } catch (error) {
    console.error('Error:', error);
    mostrarMensaje('Error al cargar hospitales', 'error');
  }
}

// Actualizar select de hospitales
function actualizarSelectHospitales() {
  const select = document.getElementById('hospitalId');
  
  select.innerHTML = '<option value="">Seleccionar hospital...</option>';
  hospitalesData.forEach(hospital => {
    const option = document.createElement('option');
    option.value = hospital.id;
    option.textContent = `${hospital.nombre} (${hospital.ciudad})`;
    option.selected = true; // Seleccionar automáticamente si es solo uno
    select.appendChild(option);
  });
}

// ============ FUNCIONES DE DONANTES ============

// Cargar donantes para el select
async function cargarDonantesParaSelect() {
  try {
    const res = await fetch('/api/donante', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    });

    if (!res.ok) throw new Error('Error al cargar donantes');

    const donantes = await res.json();
    const select = document.getElementById('donanteId');
    
    select.innerHTML = '<option value="">Seleccionar donante...</option>';
    donantes.forEach(donante => {
      const option = document.createElement('option');
      option.value = donante.id;
      option.textContent = `${donante.nombre} (${donante.tipoSanguineo})`;
      select.appendChild(option);
    });
  } catch (error) {
    console.error('Error:', error);
    mostrarMensaje('Error al cargar donantes', 'error');
  }
}

// Registrar donante
async function registrarDonante(event) {
  event.preventDefault();

  const nombre = document.getElementById('nombre').value.trim();
  const tipoSanguineo = document.getElementById('tipoSanguineo').value.trim();
  const edad = parseInt(document.getElementById('edad').value);
  const hospitalId = parseInt(document.getElementById('hospitalId').value);
  const observaciones = document.getElementById('observaciones').value.trim();

  // Validar
  if (!nombre || !tipoSanguineo || !edad || !hospitalId) {
    mostrarMensaje('Debe completar todos los campos obligatorios', 'error');
    return;
  }

  if (edad < 1 || edad > 120) {
    mostrarMensaje('La edad debe estar entre 1 y 120 años', 'error');
    return;
  }

  try {
    const btn = event.target.querySelector('button');
    btn.disabled = true;
    btn.innerHTML = '<span class="loading"></span> Registrando...';

    const res = await fetch('/api/donante', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        nombre,
        tipoSanguineo,
        edad,
        hospitalId,
        observaciones: observaciones || null
      })
    });

    const data = await res.json();

    if (res.ok) {
      mostrarMensaje('✓ Donante registrado correctamente', 'success');
      document.getElementById('formDonante').reset();
      cargarDonantesParaSelect();
      cargarDonantes();
    } else {
      mostrarMensaje(data.mensaje || 'Error al registrar donante', 'error');
    }
  } catch (error) {
    console.error('Error:', error);
    mostrarMensaje('Error al registrar donante', 'error');
  } finally {
    const btn = event.target.querySelector('button');
    btn.disabled = false;
    btn.textContent = 'Registrar Donante';
  }
}

// Cargar y mostrar donantes
async function cargarDonantes() {
  try {
    const res = await fetch('/api/donante', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    });

    if (!res.ok) throw new Error('Error al cargar donantes');

    donantesData = await res.json();
    mostrarTabladonantes();
  } catch (error) {
    console.error('Error:', error);
    document.getElementById('donantes-container').innerHTML = 
      '<div class="alert alert-error">Error al cargar donantes</div>';
  }
}

function mostrarTabladonantes() {
  const container = document.getElementById('donantes-container');

  if (donantesData.length === 0) {
    container.innerHTML = '<p style="text-align: center; color: #999;">No hay donantes registrados</p>';
    return;
  }

  let html = `
    <table>
      <thead>
        <tr>
          <th>ID</th>
          <th>Nombre</th>
          <th>Tipo Sangre</th>
          <th>Edad</th>
          <th>Hospital</th>
          <th>Estado</th>
          <th>Órganos</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
  `;

  donantesData.forEach(donante => {
    const estadoBadge = `<span class="badge badge-${donante.estado.toLowerCase()}">${donante.estado}</span>`;
    const fechaReg = new Date(donante.fechaRegistro).toLocaleDateString('es-ES');
    const hospital = donante.hospital ? donante.hospital.nombre : `Hospital #${donante.hospitalId}`;
    
    html += `
      <tr>
        <td>#${donante.id}</td>
        <td>${donante.nombre}</td>
        <td><strong>${donante.tipoSanguineo}</strong></td>
        <td>${donante.edad}</td>
        <td>${hospital}</td>
        <td>${estadoBadge}</td>
        <td>${donante.organos?.length || 0}</td>
        <td>
          <div class="action-buttons">
            <button class="btn btn-secondary" onclick="verOrganosDonante(${donante.id})">Ver</button>
            <button class="btn btn-primary" onclick="abrirModalEstadoDonante(${donante.id}, '${donante.estado}')">Estado</button>
          </div>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table>';
  container.innerHTML = html;
}

// Ver órganos de un donante
function verOrganosDonante(donanteId) {
  const donante = donantesData.find(d => d.id === donanteId);
  if (!donante) return;

  const organos = donante.organos || [];
  let html = `<div class="alert alert-info"><strong>${donante.nombre}</strong> tiene ${organos.length} órgano(s):</div>`;

  if (organos.length === 0) {
    html += '<p>Sin órganos registrados</p>';
  } else {
    html += '<ul>';
    organos.forEach(organo => {
      html += `
        <li>
          <strong>${organo.tipoOrgano}</strong> - 
          <span class="badge badge-${organo.estado.toLowerCase()}">${organo.estado}</span>
        </li>
      `;
    });
    html += '</ul>';
  }

  mostrarMensaje(html, 'info');
}

// ============ FUNCIONES DE ÓRGANOS ============

// Registrar órgano
async function registrarOrgano(event) {
  event.preventDefault();

  const donanteId = parseInt(document.getElementById('donanteId').value);
  const tipoOrgano = document.getElementById('tipoOrgano').value.trim();
  const compatibilidad = document.getElementById('compatibilidad').value.trim();

  // Validar
  if (!donanteId || !tipoOrgano) {
    mostrarMensaje('Debe seleccionar donante y tipo de órgano', 'error');
    return;
  }

  try {
    const btn = event.target.querySelector('button');
    btn.disabled = true;
    btn.innerHTML = '<span class="loading"></span> Registrando...';

    const res = await fetch('/api/organo', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        donanteId,
        tipoOrgano,
        compatibilidad: compatibilidad || null
      })
    });

    const data = await res.json();

    if (res.ok) {
      mostrarMensaje('✓ Órgano registrado correctamente', 'success');
      document.getElementById('formOrgano').reset();
      cargarOrganos();
      cargarDonantes();
    } else {
      mostrarMensaje(data.mensaje || 'Error al registrar órgano', 'error');
    }
  } catch (error) {
    console.error('Error:', error);
    mostrarMensaje('Error al registrar órgano', 'error');
  } finally {
    const btn = event.target.querySelector('button');
    btn.disabled = false;
    btn.textContent = 'Registrar Órgano';
  }
}

// Cargar y mostrar órganos
async function cargarOrganos() {
  try {
    const res = await fetch('/api/organo', {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    });

    if (!res.ok) throw new Error('Error al cargar órganos');

    organosData = await res.json();
    mostrarTablaOrganos();
  } catch (error) {
    console.error('Error:', error);
    document.getElementById('organos-container').innerHTML = 
      '<div class="alert alert-error">Error al cargar órganos</div>';
  }
}

function mostrarTablaOrganos() {
  const container = document.getElementById('organos-container');

  if (organosData.length === 0) {
    container.innerHTML = '<p style="text-align: center; color: #999;">No hay órganos registrados</p>';
    return;
  }

  let html = `
    <table>
      <thead>
        <tr>
          <th>ID</th>
          <th>Tipo Órgano</th>
          <th>Donante</th>
          <th>Tipo Sangre</th>
          <th>Estado</th>
          <th>Fecha Disponibilidad</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
  `;

  organosData.forEach(organo => {
    const estadoBadge = `<span class="badge badge-${organo.estado.toLowerCase()}">${organo.estado}</span>`;
    const fecha = new Date(organo.fechaDisponibilidad).toLocaleDateString('es-ES');
    const donante = organo.donante || {};

    html += `
      <tr>
        <td>#${organo.id}</td>
        <td><strong>${organo.tipoOrgano}</strong></td>
        <td>${donante.nombre || 'N/A'}</td>
        <td>${donante.tipoSanguineo || 'N/A'}</td>
        <td>${estadoBadge}</td>
        <td>${fecha}</td>
        <td>
          <div class="action-buttons">
            <button class="btn btn-primary" onclick="abrirModalEstadoOrgano(${organo.id}, '${organo.estado}')">Estado</button>
            ${organo.estado === 'Disponible' ? `<button class="btn btn-danger" onclick="eliminarOrgano(${organo.id})">Eliminar</button>` : ''}
          </div>
        </td>
      </tr>
    `;
  });

  html += '</tbody></table>';
  container.innerHTML = html;
}

// ============ FUNCIONES DE ESTADO ============

// Modal para cambiar estado de donante
function abrirModalEstadoDonante(donanteId, estadoActual) {
  const estados = ['Disponible', 'Asignado', 'Rechazado'];
  const select = document.getElementById('selectEstado');
  
  select.innerHTML = '';
  estados.forEach(estado => {
    const option = document.createElement('option');
    option.value = estado;
    option.textContent = estado;
    if (estado === estadoActual) option.selected = true;
    select.appendChild(option);
  });

  modalData = {
    tipo: 'donante',
    id: donanteId,
    estado: estadoActual
  };

  document.getElementById('observacionesEstado').value = '';
  document.getElementById('modalEstado').classList.add('show');
}

// Modal para cambiar estado de órgano
function abrirModalEstadoOrgano(organoId, estadoActual) {
  const estados = ['Disponible', 'Asignado', 'Descartado', 'Trasplantado'];
  const select = document.getElementById('selectEstado');
  
  select.innerHTML = '';
  estados.forEach(estado => {
    const option = document.createElement('option');
    option.value = estado;
    option.textContent = estado;
    if (estado === estadoActual) option.selected = true;
    select.appendChild(option);
  });

  modalData = {
    tipo: 'organo',
    id: organoId,
    estado: estadoActual
  };

  document.getElementById('observacionesEstado').value = '';
  document.getElementById('modalEstado').classList.add('show');
}

// Cerrar modal
function cerrarModal() {
  document.getElementById('modalEstado').classList.remove('show');
  modalData = { tipo: null, id: null, estado: null };
}

// Guardar cambio de estado
async function guardarCambioEstado() {
  if (!modalData.id) return;

  const nuevoEstado = document.getElementById('selectEstado').value;
  const observaciones = document.getElementById('observacionesEstado').value.trim();

  const endpoint = modalData.tipo === 'donante' 
    ? `/api/donante/${modalData.id}/estado`
    : `/api/organo/${modalData.id}/estado`;

  try {
    const res = await fetch(endpoint, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        nuevoEstado,
        observaciones: observaciones || null
      })
    });

    const data = await res.json();

    if (res.ok) {
      // Actualizar datos locales inmediatamente
      if (modalData.tipo === 'donante') {
        const donante = donantesData.find(d => d.id === modalData.id);
        if (donante) {
          donante.estado = nuevoEstado;
          mostrarTabladonantes();
        }
      } else {
        const organo = organosData.find(o => o.id === modalData.id);
        if (organo) {
          organo.estado = nuevoEstado;
          mostrarTablaOrganos();
        }
      }
      
      mostrarMensaje('✓ Estado actualizado correctamente', 'success');
      cerrarModal();
    } else {
      mostrarMensaje(data.mensaje || 'Error al actualizar estado', 'error');
    }
  } catch (error) {
    console.error('Error:', error);
    mostrarMensaje('Error al actualizar estado', 'error');
  }
}

// ============ FUNCIONES DE ELIMINACIÓN ============

// Abrir modal de confirmación para eliminar órgano
function eliminarOrgano(organoId) {
  const organo = organosData.find(o => o.id === organoId);
  if (!organo) return;

  confirmacionData = {
    tipo: 'organo',
    id: organoId
  };

  document.getElementById('mensajeConfirmacion').textContent = 
    `¿Está seguro de que desea eliminar el órgano "${organo.tipoOrgano}"? Esta acción no se puede deshacer.`;
  document.getElementById('modalConfirmacion').classList.add('show');
}

// Cerrar modal de confirmación
function cerrarModalConfirmacion() {
  document.getElementById('modalConfirmacion').classList.remove('show');
  confirmacionData = { tipo: null, id: null };
}

// Confirmar eliminación
async function confirmarEliminacion() {
  if (!confirmacionData.id) return;

  const endpoint = `/api/${confirmacionData.tipo}/${confirmacionData.id}`;
  const organoId = confirmacionData.id; // Guardar el ID antes de cerrar el modal

  try {
    const res = await fetch(endpoint, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include'
    });

    console.log('Respuesta del servidor:', res.status, res.ok);

    if (!res.ok) {
      const errorData = await res.json();
      console.error('Error del servidor:', errorData);
      mostrarMensaje(errorData.mensaje || 'Error al eliminar elemento', 'error');
      cerrarModalConfirmacion();
      return;
    }

    const data = await res.json();
    console.log('Datos de respuesta:', data);

    // Actualizar datos locales inmediatamente ANTES de cerrar el modal
    if (confirmacionData.tipo === 'organo') {
      console.log('Antes de eliminar - organosData:', organosData.length);
      
      // Encontrar el órgano que se va a eliminar
      const organoAEliminar = organosData.find(o => o.id === organoId);
      console.log('Órgano a eliminar:', organoAEliminar);
      
      // Eliminar del array de órganos
      organosData = organosData.filter(o => o.id !== organoId);
      console.log('Después de eliminar - organosData:', organosData.length);
      
      // También eliminar de la lista de órganos del donante si existe
      if (organoAEliminar && organoAEliminar.donanteId) {
        const donante = donantesData.find(d => d.id === organoAEliminar.donanteId);
        console.log('Donante encontrado:', donante);
        if (donante && donante.organos) {
          donante.organos = donante.organos.filter(o => o.id !== organoId);
          console.log('Órganos del donante después de eliminar:', donante.organos.length);
        }
      }
      
      // Redibujar ambas tablas para que se vea el cambio en todas partes
      console.log('Redibujando tablas...');
      mostrarTablaOrganos();
      mostrarTabladonantes();
    }
    
    mostrarMensaje('✓ Elemento eliminado correctamente', 'success');
    cerrarModalConfirmacion();
  } catch (error) {
    console.error('Error en confirmarEliminacion:', error);
    mostrarMensaje('Error al eliminar elemento', 'error');
    cerrarModalConfirmacion();
  }
}
