/**
 * login.js
 */

const LOGIN_CONFIG = {
    form: document.getElementById('loginForm'),
    usernameField: document.getElementById('username'),
    passwordField: document.getElementById('password'),
    submitBtn: document.getElementById('submitBtn'),
    messageContainer: document.getElementById('message'),
    togglePasswordBtn: document.getElementById('togglePassword'),
    usernameError: document.getElementById('usernameError'),
    passwordError: document.getElementById('passwordError'),
    usernameValidation: document.getElementById('usernameValidation'),
    passwordValidation: document.getElementById('passwordValidation'),
    minPasswordLength: 6,
    validationDelay: 300
};

let validationTimers = {
    username: null,
    password: null
};

document.addEventListener('DOMContentLoaded', () => {
    initializeLogin();
});

function initializeLogin() {
    try {
        setupEventListeners();
        console.log('✓ Formulario de login inicializado correctamente');
    } catch (error) {
        console.error('✗ Error al inicializar login:', error);
    }
}

function setupEventListeners() {
    LOGIN_CONFIG.usernameField.addEventListener('input', handleUsernameInput);
    LOGIN_CONFIG.usernameField.addEventListener('blur', validateUsername);
    LOGIN_CONFIG.usernameField.addEventListener('focus', clearUsernameError);

    LOGIN_CONFIG.passwordField.addEventListener('input', handlePasswordInput);
    LOGIN_CONFIG.passwordField.addEventListener('blur', validatePassword);
    LOGIN_CONFIG.passwordField.addEventListener('focus', clearPasswordError);

    LOGIN_CONFIG.togglePasswordBtn.addEventListener('click', togglePasswordVisibility);

    LOGIN_CONFIG.form.addEventListener('submit', handleFormSubmit);

    LOGIN_CONFIG.passwordField.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            LOGIN_CONFIG.form.dispatchEvent(new Event('submit'));
        }
    });
}

function handleUsernameInput(event) {
    const value = event.target.value.trim();
    
    clearTimeout(validationTimers.username);
    
    if (value.length === 0) {
        clearUsernameError();
        removeValidationState('username');
        return;
    }

    validationTimers.username = setTimeout(() => {
        validateUsername();
    }, LOGIN_CONFIG.validationDelay);
}

function validateUsername() {
    const username = LOGIN_CONFIG.usernameField.value.trim();
    const formGroup = LOGIN_CONFIG.usernameField.closest('.form-group');

    if (username.length === 0) {
        showUsernameError('El nombre de usuario es requerido');
        formGroup.classList.remove('valid');
        formGroup.classList.add('invalid');
        return false;
    }

    if (username.length < 3) {
        showUsernameError('El nombre de usuario debe tener al menos 3 caracteres');
        formGroup.classList.remove('valid');
        formGroup.classList.add('invalid');
        return false;
    }

    if (!/^[a-zA-Z0-9_-]+$/.test(username)) {
        showUsernameError('El nombre de usuario solo puede contener letras, números, guiones y guiones bajos');
        formGroup.classList.remove('valid');
        formGroup.classList.add('invalid');
        return false;
    }

    clearUsernameError();
    formGroup.classList.remove('invalid');
    formGroup.classList.add('valid');
    updateValidationIcon('username', '✓');
    return true;
}

function handlePasswordInput(event) {
    const value = event.target.value;
    
    clearTimeout(validationTimers.password);
    
    if (value.length === 0) {
        clearPasswordError();
        removeValidationState('password');
        return;
    }

    validationTimers.password = setTimeout(() => {
        validatePassword();
    }, LOGIN_CONFIG.validationDelay);
}

function validatePassword() {
    const password = LOGIN_CONFIG.passwordField.value;
    const formGroup = LOGIN_CONFIG.passwordField.closest('.form-group');

    if (password.length === 0) {
        showPasswordError('La contraseña es requerida');
        formGroup.classList.remove('valid');
        formGroup.classList.add('invalid');
        return false;
    }

    if (password.length < LOGIN_CONFIG.minPasswordLength) {
        showPasswordError(`La contraseña debe tener al menos ${LOGIN_CONFIG.minPasswordLength} caracteres`);
        formGroup.classList.remove('valid');
        formGroup.classList.add('invalid');
        return false;
    }

    clearPasswordError();
    formGroup.classList.remove('invalid');
    formGroup.classList.add('valid');
    updateValidationIcon('password', '✓');
    return true;
}

function showUsernameError(message) {
    LOGIN_CONFIG.usernameError.textContent = message;
    LOGIN_CONFIG.usernameError.style.animation = 'slideInUp 0.2s ease-out';
}

function clearUsernameError() {
    LOGIN_CONFIG.usernameError.textContent = '';
    removeValidationState('username');
}

function showPasswordError(message) {
    LOGIN_CONFIG.passwordError.textContent = message;
    LOGIN_CONFIG.passwordError.style.animation = 'slideInUp 0.2s ease-out';
}

function clearPasswordError() {
    LOGIN_CONFIG.passwordError.textContent = '';
    removeValidationState('password');
}

function updateValidationIcon(field, icon) {
    const validationElement = field === 'username' 
        ? LOGIN_CONFIG.usernameValidation 
        : LOGIN_CONFIG.passwordValidation;
    validationElement.textContent = icon;
}

function removeValidationState(field) {
    const validationElement = field === 'username' 
        ? LOGIN_CONFIG.usernameValidation 
        : LOGIN_CONFIG.passwordValidation;
    validationElement.textContent = '';
}

function togglePasswordVisibility(event) {
    event.preventDefault();
    
    const isPassword = LOGIN_CONFIG.passwordField.type === 'password';
    LOGIN_CONFIG.passwordField.type = isPassword ? 'text' : 'password';
    
    LOGIN_CONFIG.togglePasswordBtn.textContent = isPassword ? '👁️' : '🔐';
    LOGIN_CONFIG.togglePasswordBtn.setAttribute('aria-pressed', isPassword);
}

async function handleFormSubmit(event) {
    event.preventDefault();

    clearMessage();

    const isUsernameValid = validateUsername();
    const isPasswordValid = validatePassword();

    if (!isUsernameValid || !isPasswordValid) {
        showMessage('Por favor, completa todos los campos correctamente', 'error');
        return;
    }

    const username = LOGIN_CONFIG.usernameField.value.trim();
    const password = LOGIN_CONFIG.passwordField.value;

    setButtonLoading(true);

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({ username, password })
        });

        const data = await response.json().catch(() => ({}));

        if (response.ok) {
            showMessage('✓ Inicio de sesión correcto. Redirigiendo...', 'success');
            
            logUserAction('login_success', `Usuario ${username} inició sesión correctamente`);

            setTimeout(() => {
                redirectUserByRole(data);
            }, 1500);
        } else {
            const errorMessage = data.message || 'Credenciales inválidas';
            showMessage(errorMessage, 'error');
            
            LOGIN_CONFIG.passwordField.value = '';
            clearPasswordError();
            removeValidationState('password');
            
            logUserAction('login_failed', `Intento fallido con usuario: ${username}`);
        }
    } catch (error) {
        console.error('Error al intentar login:', error);
        showMessage('Error al intentar iniciar sesión. Intente nuevamente.', 'error');
        logUserAction('login_error', `Error de conexión: ${error.message}`);
    } finally {
        setButtonLoading(false);
    }
}

function setButtonLoading(isLoading) {
    const buttonText = LOGIN_CONFIG.submitBtn.querySelector('.button-text');
    const buttonSpinner = LOGIN_CONFIG.submitBtn.querySelector('.button-spinner');

    if (isLoading) {
        LOGIN_CONFIG.submitBtn.disabled = true;
        buttonText.textContent = 'Enviando...';
        buttonSpinner.style.display = 'block';
    } else {
        LOGIN_CONFIG.submitBtn.disabled = false;
        buttonText.textContent = 'Iniciar Sesión';
        buttonSpinner.style.display = 'none';
    }
}

function showMessage(message, type = 'error') {
    LOGIN_CONFIG.messageContainer.textContent = message;
    LOGIN_CONFIG.messageContainer.className = `message-container show ${type}`;
    
    LOGIN_CONFIG.messageContainer.style.opacity = '1';
    LOGIN_CONFIG.messageContainer.style.visibility = 'visible';
}

function clearMessage() {
    LOGIN_CONFIG.messageContainer.textContent = '';
    LOGIN_CONFIG.messageContainer.className = 'message-container';
    LOGIN_CONFIG.messageContainer.style.opacity = '0';
    LOGIN_CONFIG.messageContainer.style.visibility = 'hidden';
}

function redirectUserByRole(data) {
    if (!data || !data.role) {
        window.location.href = '/admin.html';
        return;
    }

    const role = data.role.toLowerCase();

    if (role === 'administrador' || role === 'admin') {
        window.location.href = '/admin.html';
    } else if (role === 'medico') {
        window.location.href = '/medico.html';
    } else {
        window.location.href = '/';
    }
}

function logUserAction(action, description) {
    try {
        fetch('/api/bitacora/registrar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                accion: action,
                descripcion: description,
                modulo: 'Login'
            })
        }).catch(err => console.warn('No se pudo registrar acción en bitácora:', err));
    } catch (error) {
        console.warn('Error al registrar acción:', error);
    }
}

window.addEventListener('load', () => {
    LOGIN_CONFIG.togglePasswordBtn.textContent = '🔐';
});
