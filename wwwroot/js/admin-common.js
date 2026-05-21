// admin-common.js - общие функции для всех админских страниц

// Функция добавления действия
function addAdminAction(icon, text) {
    console.log('Добавление действия:', icon, text);
    const actions = JSON.parse(sessionStorage.getItem('adminActions') || '[]');
    const now = new Date();
    const timeStr = `${now.getDate().toString().padStart(2, '0')}.${(now.getMonth() + 1).toString().padStart(2, '0')}.${now.getFullYear()} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
    
    actions.unshift({ icon, text, time: timeStr });
    
    if (actions.length > 50) actions.pop();
    
    sessionStorage.setItem('adminActions', JSON.stringify(actions));
    console.log('Текущие действия:', actions);
}

// Функция загрузки действий (для дашборда)
function loadRecentActionsToContainer(containerId) {
    const actions = JSON.parse(sessionStorage.getItem('adminActions') || '[]');
    const container = document.getElementById(containerId);
    
    if (!container) return;
    
    if (!actions || actions.length === 0) {
        container.innerHTML = `
            <div class="empty-actions">
                <div class="empty-icon">📭</div>
                <p>Нет действий</p>
                <span>Здесь будут отображаться ваши последние действия</span>
            </div>
        `;
        return;
    }
    
    const recentActions = actions.slice(0, 10);
    
    container.innerHTML = recentActions.map(action => `
        <div class="action-item">
            <div class="action-icon">${action.icon}</div>
            <div class="action-info">
                <div class="action-text">${escapeHtml(action.text)}</div>
                <div class="action-time">${action.time}</div>
            </div>
        </div>
    `).join('');
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, m => {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}

// Делаем функцию глобальной
window.addAdminAction = addAdminAction;