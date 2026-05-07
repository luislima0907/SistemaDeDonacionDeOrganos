/**
 * Configuración del módulo Médico
 */
const MEDICO_CONFIG = {
    logoutBtn: document.getElementById('btnLogout'),
    menuCards: document.querySelectorAll('.menu-card--active'),
    navDelay: 500,
    logDelay: 200
};

/**
 * Inicialización
 */
document.addEventListener('DOMContentLoaded', () => {
    initializeMedicoPanel();
});

/**
 * Función principal de inicialización
 */
function initializeMedicoPanel() {
    try {
        setupEventListeners();
        setupRippleEffects();
        logPanelAccess();
        monitorSessionHealth();
        loadStatistics();
        console.log('✓ Dashboard Médico inicializado correctamente');
    } catch (error) {
        console.error('✗ Error al inicializar panel médico:', error);
        showErrorMessage('Error al cargar el panel médico');
    }
}

/**
 * Carga las estadísticas desde la API
 */
function loadStatistics() {
    fetchStatistics();
    
    // Recargar estadísticas cada 5 minutos según el requerimiento
    setInterval(fetchStatistics, 5 * 60 * 1000);
}

/**
 * Obtiene las estadísticas de la API
 */
async function fetchStatistics() {
    try {
        const organsElement = document.getElementById('organsCount');
        const patientsElement = document.getElementById('patientsCount');
        const donorsElement = document.getElementById('donorsCount');
        const assignmentsElement = document.getElementById('assignmentsCount');

        // Mostrar estado de carga
        if (organsElement) organsElement.classList.add('loading');
        if (patientsElement) patientsElement.classList.add('loading');
        if (donorsElement) donorsElement.classList.add('loading');
        if (assignmentsElement) assignmentsElement.classList.add('loading');

        // Obtener estadísticas de órganos disponibles
        const organsResponse = await fetch('/api/organo/disponibles', {
            credentials: 'include'
        });

        // Obtener estadísticas de pacientes activos
        const patientsResponse = await fetch('/api/paciente/activos', {
            credentials: 'include'
        });

        // Obtener estadísticas de donantes
        const donorsResponse = await fetch('/api/donante/activos', {
            credentials: 'include'
        });

        // Obtener estadísticas de asignaciones del médico actual
        const assignmentsResponse = await fetch('/api/asignacion/medico/activos', {
            credentials: 'include'
        });

        // Procesar respuestas
        const organsData = organsResponse.ok ? await organsResponse.json() : { count: 0 };
        const patientsData = patientsResponse.ok ? await patientsResponse.json() : { count: 0 };
        const donorsData = donorsResponse.ok ? await donorsResponse.json() : { count: 0 };
        const assignmentsData = assignmentsResponse.ok ? await assignmentsResponse.json() : { count: 0 };

        // Actualizar elementos DOM
        if (organsElement) {
            organsElement.textContent = organsData.count || 0;
            organsElement.classList.remove('loading');
        }

        if (patientsElement) {
            patientsElement.textContent = patientsData.count || 0;
            patientsElement.classList.remove('loading');
        }

        if (donorsElement) {
            donorsElement.textContent = donorsData.count || 0;
            donorsElement.classList.remove('loading');
        }

        if (assignmentsElement) {
            assignmentsElement.textContent = assignmentsData.count || 0;
            assignmentsElement.classList.remove('loading');
        }

        console.log('✓ Estadísticas cargadas correctamente');

    } catch (error) {
        console.error('✗ Error al cargar estadísticas:', error);
        
        // En caso de error, mostrar 0 en los contadores
        const elements = [
            document.getElementById('organsCount'),
            document.getElementById('patientsCount'),
            document.getElementById('donorsCount'),
            document.getElementById('assignmentsCount')
        ];

        elements.forEach(el => {
            if (el) {
                el.textContent = '0';
                el.classList.remove('loading');
            }
        });
    }
}

/**
 * Configura todos los event listeners
 */
function setupEventListeners() {
    if (MEDICO_CONFIG.logoutBtn) {
        MEDICO_CONFIG.logoutBtn.addEventListener('click', handleLogoutClick);
    }

    MEDICO_CONFIG.menuCards.forEach((card, index) => {
        card.addEventListener('click', handleMenuCardClick);
        card.addEventListener('keypress', handleMenuCardKeypress);
        
        // Stagger animation para tarjetas
        setTimeout(() => {
            card.style.animation = `gridFadeIn 0.5s ease-out forwards`;
        }, index * 50);
    });
}

/**
 * Configura efecto ripple en tarjetas
 */
function setupRippleEffects() {
    MEDICO_CONFIG.menuCards.forEach(card => {
        card.addEventListener('click', (e) => {
            createRipple(e, card);
        });
    });
}

/**
 * Crea efecto ripple en click
 */
function createRipple(event, card) {
    const ripple = document.createElement('span');
    const rect = card.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = event.clientX - rect.left - size / 2;
    const y = event.clientY - rect.top - size / 2;

    ripple.style.cssText = `
        position: absolute;
        top: ${y}px;
        left: ${x}px;
        width: ${size}px;
        height: ${size}px;
        background: rgba(255, 255, 255, 0.6);
        border-radius: 50%;
        transform: scale(0);
        animation: rippleExpand 0.6s ease-out;
        pointer-events: none;
    `;

    card.style.position = 'relative';
    card.style.overflow = 'hidden';
    card.appendChild(ripple);

    setTimeout(() => ripple.remove(), 600);
}

/**
 * Maneja click en botón logout
 */
function handleLogoutClick(event) {
    event.preventDefault();
    logUserAction('logout_click', 'Médico inició cierre de sesión');
    cerrarSesion();
}

/**
 * Maneja click en tarjetas de menú
 */
function handleMenuCardClick(event) {
    const section = event.currentTarget.getAttribute('data-section');
    logUserAction('menu_card_click', `Médico accedió a sección: ${section}`);
}

/**
 * Maneja tecla Enter en tarjetas de menú
 */
function handleMenuCardKeypress(event) {
    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        event.currentTarget.click();
    }
}

/**
 * Registra acciones del usuario en la bitácora
 */
function logUserAction(action, descripcion) {
    try {
        fetch('/api/bitacora/registrar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                accion: action,
                descripcion: descripcion,
                modulo: 'Dashboard Médico'
            })
        }).catch(err => console.warn('No se pudo registrar acción en bitácora:', err));
    } catch (error) {
        console.error('Error al registrar acción:', error);
    }
}

/**
 * Registra acceso al panel
 */
function logPanelAccess() {
    setTimeout(() => {
        logUserAction('panel_access', 'Médico accedió al panel médico');
    }, MEDICO_CONFIG.logDelay);
}

/**
 * Monitorea la salud de la sesión
 */
function monitorSessionHealth() {
    // Verificar sesión cada 10 minutos
    setInterval(async () => {
        try {
            const res = await fetch('/api/auth/check-session', {
                credentials: 'include',
                cache: 'no-cache'
            });
            if (!res.ok) {
                console.warn('Sesión no válida detectada');
            }
        } catch (error) {
            console.error('Error al verificar sesión:', error);
        }
    }, 10 * 60 * 1000);
}

/**
 * Muestra mensaje de error
 */
function showErrorMessage(mensaje) {
    const errorDiv = document.createElement('div');
    errorDiv.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: #dc3545;
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(220, 53, 69, 0.3);
        z-index: 1000;
        animation: slideInRight 0.3s ease-out;
    `;
    errorDiv.textContent = mensaje;
    document.body.appendChild(errorDiv);

    setTimeout(() => {
        errorDiv.style.animation = 'slideOutRight 0.3s ease-out forwards';
        setTimeout(() => errorDiv.remove(), 300);
    }, 4000);
}

/**
 * Animación ripple expandida
 */
const style = document.createElement('style');
style.textContent = `
    @keyframes rippleExpand {
        0% {
            transform: scale(0);
            opacity: 1;
        }
        100% {
            transform: scale(4);
            opacity: 0;
        }
    }

    @keyframes slideInRight {
        from {
            opacity: 0;
            transform: translateX(100px);
        }
        to {
            opacity: 1;
            transform: translateX(0);
        }
    }

    @keyframes slideOutRight {
        from {
            opacity: 1;
            transform: translateX(0);
        }
        to {
            opacity: 0;
            transform: translateX(100px);
        }
    }
`;
document.head.appendChild(style);

