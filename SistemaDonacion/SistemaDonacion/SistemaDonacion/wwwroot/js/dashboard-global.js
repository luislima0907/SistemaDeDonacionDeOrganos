/**
 * dashboard-global.js
 * Script compartido para todos los dashboards (Admin, Médico, etc.)
 * Reutilizable en cualquier página que necesite los componentes comunes
 * 
 * Incluye:
 * - Verificación de sesión
 * - Cierre de sesión con modal de confirmación
 * - Detección de inactividad
 * - Limpieza de almacenamiento
 */

/**
 * Verificar sesión al cargar la página
 */
(async () => {
    try {
        const res = await fetch('/api/auth/current', { credentials: 'include' });
        if (!res.ok) {
            window.location.replace('/login.html');
            return;
        }
    } catch {
        window.location.replace('/login.html');
    }
})();

/**
 * Abre un modal de confirmación antes de cerrar sesión
 */
function cerrarSesion() {
    const overlay = document.createElement('div');
    overlay.id = 'logoutOverlay';
    overlay.style.cssText = `
        position: fixed; 
        inset: 0; 
        background: rgba(0,0,0,0.45);
        display: flex; 
        align-items: center; 
        justify-content: center; 
        z-index: 9999;
    `;

    overlay.innerHTML = `
        <div class="modal-content" style="
            background: white; 
            border-radius: 8px; 
            padding: 1.5rem 2rem;
            max-width: 320px; 
            width: 90%; 
            text-align: center; 
            box-shadow: 0 4px 20px rgba(0,0,0,0.2);
            animation: modalSlideIn 0.3s ease-out;
        ">
            <p style="
                font-size: 1.1rem; 
                font-weight: bold; 
                margin: 0 0 0.5rem; 
                color: #333;
            ">¿Cerrar sesión?</p>
            <p style="
                font-size: 0.9rem; 
                color: #666; 
                margin: 0 0 1.25rem;
            ">
                Tu sesión activa será finalizada de forma segura.
            </p>
            <div class="modal-buttons" style="
                display: flex; 
                gap: 8px;
            ">
                <button onclick="cancelarLogout()"
                    style="
                        flex:1; 
                        padding: 8px; 
                        border: 1px solid #ddd; 
                        border-radius: 4px;
                        background: #f5f5f5; 
                        cursor: pointer; 
                        font-size: 0.9rem; 
                        color: #333; 
                        font-weight: 500;
                        transition: all 0.2s ease;
                    "
                    onmouseover="this.style.background='#efefef'"
                    onmouseout="this.style.background='#f5f5f5'"
                >
                    Cancelar
                </button>
                <button onclick="confirmarLogout()"
                    style="
                        flex:1; 
                        padding: 8px; 
                        border: 1px solid #c0392b; 
                        border-radius: 4px;
                        background: #e74c3c; 
                        color: white; 
                        cursor: pointer;
                        font-size: 0.9rem; 
                        font-weight: bold;
                        transition: all 0.2s ease;
                    "
                    onmouseover="this.style.background='#d63025'"
                    onmouseout="this.style.background='#e74c3c'"
                >
                    Sí, cerrar sesión
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(overlay);
}

/**
 * Cancela el cierre de sesión
 */
function cancelarLogout() {
    const overlay = document.getElementById('logoutOverlay');
    if (overlay) {
        overlay.style.animation = 'modalSlideOut 0.3s ease-out forwards';
        setTimeout(() => overlay.remove(), 300);
    }
}

/**
 * Confirma el cierre de sesión y redirige al login
 */
async function confirmarLogout() {
    const overlay = document.getElementById('logoutOverlay');

    // Mostrar mensaje de cierre en progreso
    overlay.innerHTML = `
        <div class="modal-content" style="
            background: white; 
            border-radius: 8px; 
            padding: 2rem;
            text-align: center; 
            box-shadow: 0 4px 20px rgba(0,0,0,0.2);
        ">
            <p style="
                font-size: 1rem; 
                color: #333; 
                margin: 0;
            ">Cerrando sesión...</p>
        </div>
    `;

    try {
        const res = await fetch('/api/auth/logout', {
            method: 'POST',
            credentials: 'include'
        });

        if (res.ok) {
            // Limpiar almacenamiento local
            sessionStorage.clear();
            localStorage.clear();

            // Mostrar confirmación de éxito
            overlay.innerHTML = `
                <div class="modal-content" style="
                    background: white; 
                    border-radius: 8px; 
                    padding: 2rem;
                    text-align: center; 
                    box-shadow: 0 4px 20px rgba(0,0,0,0.2);
                ">
                    <p style="
                        font-size: 1.1rem; 
                        font-weight: bold; 
                        color: #27ae60; 
                        margin: 0 0 0.5rem;
                    ">
                        ✓ Sesión cerrada correctamente
                    </p>
                    <p style="
                        font-size: 0.9rem; 
                        color: #666; 
                        margin: 0;
                    ">
                        Redirigiendo...
                    </p>
                </div>
            `;

            // Redirigir después de 1 segundo
            setTimeout(() => {
                window.location.replace('/login.html');
            }, 1000);
        } else {
            throw new Error('Error en la respuesta del servidor');
        }
    } catch (error) {
        console.error('Error al cerrar sesión:', error);

        // Mostrar error
        overlay.innerHTML = `
            <div class="modal-content" style="
                background: white; 
                border-radius: 8px; 
                padding: 2rem;
                text-align: center; 
                box-shadow: 0 4px 20px rgba(0,0,0,0.2);
            ">
                <p style="
                    font-size: 1rem; 
                    font-weight: bold; 
                    color: #dc3545; 
                    margin: 0 0 0.5rem;
                ">
                    Error al cerrar sesión
                </p>
                <p style="
                    font-size: 0.9rem; 
                    color: #666; 
                    margin: 0 0 1rem;
                ">
                    Intenta nuevamente o cierra el navegador.
                </p>
                <button onclick="cancelarLogout()"
                    style="
                        padding: 8px 16px; 
                        background: #667eea; 
                        color: white; 
                        border: none;
                        border-radius: 4px; 
                        cursor: pointer; 
                        font-weight: 500;
                        transition: all 0.2s ease;
                    "
                    onmouseover="this.style.background='#5568d3'"
                    onmouseout="this.style.background='#667eea'"
                >
                    Aceptar
                </button>
            </div>
        `;
    }
}

/**
 * DETECCIÓN DE INACTIVIDAD
 * Cierra la sesión automáticamente después de 30 minutos sin actividad
 */

const INACTIVIDAD_MS = 30 * 60 * 1000; // 30 minutos
let timerInactividad;

function reiniciarTimer() {
    clearTimeout(timerInactividad);
    timerInactividad = setTimeout(mostrarSesionExpirada, INACTIVIDAD_MS);
}

function mostrarSesionExpirada() {
    const overlayExistente = document.getElementById('sessionExpiredOverlay');
    if (overlayExistente) overlayExistente.remove();

    const overlay = document.createElement('div');
    overlay.id = 'sessionExpiredOverlay';
    overlay.style.cssText = `
        position: fixed; 
        inset: 0; 
        background: rgba(0,0,0,0.6);
        display: flex; 
        align-items: center; 
        justify-content: center; 
        z-index: 99999;
    `;

    overlay.innerHTML = `
        <div style="
            background: white; 
            border-radius: 8px; 
            padding: 2rem;
            max-width: 320px; 
            width: 90%; 
            text-align: center;
            box-shadow: 0 4px 20px rgba(0,0,0,0.3);
            animation: modalSlideIn 0.3s ease-out;
        ">
            <p style="
                font-size: 1.3rem; 
                margin: 0 0 0.5rem;
            ">⏱️</p>
            <p style="
                font-size: 1rem; 
                font-weight: bold; 
                color: #333; 
                margin: 0 0 0.5rem;
            ">
                Su sesión ha expirado por inactividad
            </p>
            <p style="
                font-size: 0.85rem; 
                color: #666; 
                margin: 0 0 1rem;
            ">
                Será redirigido al login en breve...
            </p>
        </div>
    `;

    console.log('Sesión expirada por inactividad');
    document.body.appendChild(overlay);

    // Limpiar almacenamiento
    sessionStorage.clear();
    localStorage.clear();

    setTimeout(() => {
        window.location.replace('/login.html');
    }, 1500);
}

/**
 * Verifica la sesión periódicamente
 */
async function verificarSesion() {
    try {
        const res = await fetch('/api/auth/check-session', {
            credentials: 'include',
            cache: 'no-cache'
        });
        if (!res.ok) {
            mostrarSesionExpirada();
        }
    } catch {
        mostrarSesionExpirada();
    }
}

/**
 * Detectar actividad del usuario
 * Reinicia el timer cuando el usuario interactúa con la página
 */
['click', 'mousemove', 'keydown', 'scroll', 'touchstart'].forEach(evento => {
    document.addEventListener(evento, reiniciarTimer, { passive: true });
});

/**
 * Verificar sesión cada minuto
 */
setInterval(verificarSesion, 1 * 60 * 1000);

/**
 * Estilos para animaciones del modal
 */
const globalStyles = document.createElement('style');
globalStyles.textContent = `
    @keyframes modalSlideIn {
        from {
            opacity: 0;
            transform: translateY(-20px) scale(0.95);
        }
        to {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
    }

    @keyframes modalSlideOut {
        from {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
        to {
            opacity: 0;
            transform: translateY(-20px) scale(0.95);
        }
    }
`;
document.head.appendChild(globalStyles);

