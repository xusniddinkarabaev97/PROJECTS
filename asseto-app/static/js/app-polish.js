/* ── ASSETO PREMIUM UI POLISH: CUSTOM DIALOGS ── */

function showConfirm(title, message) {
    return new Promise((resolve) => {
        const existing = document.getElementById('ios-dialog-overlay');
        if (existing) existing.remove();

        const overlay = document.createElement('div');
        overlay.id = 'ios-dialog-overlay';
        overlay.className = 'ios-dialog-overlay';

        const dialog = document.createElement('div');
        dialog.className = 'ios-dialog';

        dialog.innerHTML = `
            <div class="ios-dialog-body">
                <div class="ios-dialog-title">${title}</div>
                <div class="ios-dialog-message">${message}</div>
            </div>
            <div class="ios-dialog-actions">
                <button class="ios-dialog-btn cancel" id="ios-confirm-cancel">Отмена</button>
                <button class="ios-dialog-btn confirm" id="ios-confirm-ok">ОК</button>
            </div>
        `;

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Trigger transition
        setTimeout(() => overlay.classList.add('show'), 10);

        document.getElementById('ios-confirm-cancel').addEventListener('click', () => {
            overlay.classList.remove('show');
            setTimeout(() => {
                overlay.remove();
                resolve(false);
            }, 250);
        });

        document.getElementById('ios-confirm-ok').addEventListener('click', () => {
            overlay.classList.remove('show');
            setTimeout(() => {
                overlay.remove();
                resolve(true);
            }, 250);
        });
    });
}

function showAlert(title, message) {
    return new Promise((resolve) => {
        const existing = document.getElementById('ios-dialog-overlay');
        if (existing) existing.remove();

        const overlay = document.createElement('div');
        overlay.id = 'ios-dialog-overlay';
        overlay.className = 'ios-dialog-overlay';

        const dialog = document.createElement('div');
        dialog.className = 'ios-dialog';

        dialog.innerHTML = `
            <div class="ios-dialog-body">
                <div class="ios-dialog-title">${title}</div>
                <div class="ios-dialog-message">${message}</div>
            </div>
            <div class="ios-dialog-actions">
                <button class="ios-dialog-btn confirm" id="ios-alert-ok" style="font-weight:600;">ОК</button>
            </div>
        `;

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Trigger transition
        setTimeout(() => overlay.classList.add('show'), 10);

        document.getElementById('ios-alert-ok').addEventListener('click', () => {
            overlay.classList.remove('show');
            setTimeout(() => {
                overlay.remove();
                resolve(true);
            }, 250);
        });
    });
}

// Global alert/confirm override wrapper for non-blocking alerts (e.g. status notifications)
// But for logic flows (where return values are checked), developers should use await showAlert() / await showConfirm()
window.showToast = function(msg, type = 'ok') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position:fixed;top:24px;left:50%;transform:translateX(-50%);z-index:9999;display:flex;flex-direction:column;gap:8px;pointer-events:none;width:100%;max-width:380px;padding:0 20px;';
        document.body.appendChild(container);
        
        const style = document.createElement('style');
        style.textContent = `
            .t-msg {
                background: rgba(255, 255, 255, 0.85);
                backdrop-filter: blur(20px); -webkit-backdrop-filter: blur(20px);
                border: .5px solid rgba(0,0,0,0.08);
                border-radius: 18px; padding: 12px 18px;
                box-shadow: 0 10px 30px rgba(0,0,0,0.08);
                display: flex; align-items: center; gap: 12px;
                animation: t-in 0.5s cubic-bezier(0.2, 0.8, 0.2, 1) forwards;
                pointer-events: auto;
            }
            [data-theme="dark"] .t-msg {
                background: rgba(44, 44, 46, 0.9);
                border-color: rgba(255,255,255,0.1);
                box-shadow: 0 10px 30px rgba(0,0,0,0.3);
            }
            .t-icon { width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
            .t-ok .t-icon { background: #34C759; color: #fff; }
            .t-err .t-icon { background: #FF3B30; color: #fff; }
            .t-text { font-size: 14px; font-weight: 600; color: var(--text); }
            @keyframes t-in { from { opacity: 0; transform: translateY(-20px) scale(0.9); } to { opacity: 1; transform: translateY(0) scale(1); } }
            @keyframes t-out { from { opacity: 1; transform: scale(1); } to { opacity: 0; transform: scale(0.9); } }
            .t-hide { animation: t-out 0.3s ease-in forwards !important; }
        `;
        document.head.appendChild(style);
    }

    const el = document.createElement('div');
    el.className = `t-msg t-${type}`;
    const icon = type === 'ok' ? '✓' : '✕';
    el.innerHTML = `<div class="t-icon">${icon}</div><div class="t-text">${msg}</div>`;
    container.appendChild(el);

    setTimeout(() => {
        el.classList.add('t-hide');
        setTimeout(() => el.remove(), 300);
    }, 3500);
};
