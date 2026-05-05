/**
 * Configuración del módulo
 */
const ADMIN_CONFIG = {
    logoutBtn: document.getElementById('btnLogout'),
    menuCards: document.querySelectorAll('.menu-card--active'),
    navDelay: 500,
    logDelay: 200
};

/**
 * Inicialización
 */
document.addEventListener('DOMContentLoaded', () => {
    initializeAdminPanel();
});

/**
 * Función principal de inicialización
 */
function initializeAdminPanel() {
    try {
        setupEventListeners();
        setupRippleEffects();
        logPanelAccess();
        monitorSessionHealth();
        loadStatistics();
        console.log('✓ Dashboard Administrativo inicializado correctamente');
    } catch (error) {
        console.error('✗ Error al inicializar panel administrativo:', error);
        showErrorMessage('Error al cargar el panel administrativo');
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
        const hospitalsElement = document.getElementById('hospitalsCount');

        // Mostrar estado de carga
        if (organsElement) organsElement.classList.add('loading');
        if (patientsElement) patientsElement.classList.add('loading');
        if (donorsElement) donorsElement.classList.add('loading');
        if (hospitalsElement) hospitalsElement.classList.add('loading');

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

        // Obtener estadísticas de hospitales
        const hospitalsResponse = await fetch('/api/hospital/activos', {
            credentials: 'include'
        });

        // Procesar respuestas
        const organsData = organsResponse.ok ? await organsResponse.json() : { count: 0 };
        const patientsData = patientsResponse.ok ? await patientsResponse.json() : { count: 0 };
        const donorsData = donorsResponse.ok ? await donorsResponse.json() : { count: 0 };
        const hospitalsData = hospitalsResponse.ok ? await hospitalsResponse.json() : { count: 0 };

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

        if (hospitalsElement) {
            hospitalsElement.textContent = hospitalsData.count || 0;
            hospitalsElement.classList.remove('loading');
        }

        console.log('✓ Estadísticas cargadas correctamente');

    } catch (error) {
        console.error('✗ Error al cargar estadísticas:', error);
        
        // En caso de error, mostrar 0 en los contadores
        const elements = [
            document.getElementById('organsCount'),
            document.getElementById('patientsCount'),
            document.getElementById('donorsCount'),
            document.getElementById('hospitalsCount')
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
    if (ADMIN_CONFIG.logoutBtn) {
        ADMIN_CONFIG.logoutBtn.addEventListener('click', handleLogoutClick);
    }

    ADMIN_CONFIG.menuCards.forEach((card, index) => {
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
    ADMIN_CONFIG.menuCards.forEach(card => {
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
    logUserAction('logout_click', 'Administrador inició cierre de sesión');
    cerrarSesion();
}

/**
 * Maneja click en tarjetas de menú
 */
function handleMenuCardClick(event) {
    const card = event.currentTarget;
    const href = card.getAttribute('href');
    const section = card.getAttribute('data-section');

    if (!href) return;

    // Añadir clase de carga
    card.classList.add('loading');
    card.style.pointerEvents = 'none';

    logUserAction(
        'admin_navigation',
        `Navegó a: ${section}`,
        { section }
    );

    setTimeout(() => {
        window.location.href = href;
    }, ADMIN_CONFIG.navDelay);
}

/**
 * Maneja tecla Enter/Space en tarjetas
 */
function handleMenuCardKeypress(event) {
    if (event.key === 'Enter' || event.code === 'Space') {
        event.preventDefault();
        handleMenuCardClick(event);
    }
}

/**
 * Registra acceso al panel
 */
function logPanelAccess() {
    logUserAction(
        'admin_panel_access',
        'Accesó al Dashboard Administrativo',
        {
            timestamp: new Date().toISOString()
        }
    );
}

/**
 * Registra acción del usuario
 */
function logUserAction(action, description, additionalData = {}) {
    try {
        const bitacoraEntry = {
            accion: action,
            descripcion: description,
            fecha: new Date().toISOString(),
            pagina: '/admin.html',
            ...additionalData
        };

        setTimeout(() => {
            fetch('/api/bitacora/registrar', {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(bitacoraEntry)
            }).catch(error => {
                console.warn('⚠ No se pudo registrar en bitácora:', error);
            });
        }, ADMIN_CONFIG.logDelay);

    } catch (error) {
        console.error('✗ Error al registrar acción:', error);
    }
}

/**
 * Muestra mensaje de error
 */
function showErrorMessage(message) {
    const errorDiv = document.createElement('div');
    errorDiv.setAttribute('role', 'alert');
    errorDiv.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: linear-gradient(135deg, #dc3545 0%, #a02a3a 100%);
        color: white;
        padding: 16px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
        font-weight: 500;
        z-index: 9999;
        animation: slideInRight 0.3s ease-out;
        max-width: 300px;
    `;
    errorDiv.textContent = `⚠️ ${message}`;

    document.body.appendChild(errorDiv);

    setTimeout(() => {
        errorDiv.style.animation = 'slideOutRight 0.3s ease-out forwards';
        setTimeout(() => errorDiv.remove(), 300);
    }, 5000);
}

/**
 * Monitorea salud de la sesión
 */
function monitorSessionHealth() {
    setInterval(async () => {
        try {
            const response = await fetch('/api/auth/check-session', {
                credentials: 'include',
                cache: 'no-cache'
            });

            if (!response.ok) {
                console.warn('⚠ Sesión perdida');
            }
        } catch (error) {
            console.warn('⚠ Error verificando sesión:', error);
        }
    }, 60000); // Cada minuto
}

/**
 * Registra salida del panel
 */
window.addEventListener('beforeunload', () => {
    logUserAction('admin_panel_exit', 'Salió del Dashboard Administrativo');
});

/**
 * Inyecta estilos dinámicos
 */
const dynamicStyles = `
    @keyframes rippleExpand {
        to {
            transform: scale(1);
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
        to {
            opacity: 0;
            transform: translateX(100px);
        }
    }
`;

const styleSheet = document.createElement('style');
styleSheet.textContent = dynamicStyles;
document.head.appendChild(styleSheet);

// Exportar para debugging
window.AdminPanel = {
    logAction: logUserAction,
    showError: showErrorMessage
};
