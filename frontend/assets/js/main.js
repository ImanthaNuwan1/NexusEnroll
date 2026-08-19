// ── NexusEnroll Frontend JavaScript ──

document.addEventListener('DOMContentLoaded', function() {
    // Auto-dismiss flash messages after 5 seconds
    const flash = document.getElementById('flash-msg');
    if (flash) {
        setTimeout(() => {
            flash.style.opacity = '0';
            flash.style.transform = 'translateX(100%)';
            setTimeout(() => flash.remove(), 300);
        }, 5000);
    }

    // Mobile nav toggle
    const navToggle = document.querySelector('.nav-toggle');
    if (navToggle) {
        navToggle.addEventListener('click', function() {
            document.querySelector('.nav-links').classList.toggle('open');
        });
    }

    // Confirm destructive actions
    document.querySelectorAll('form[onsubmit]').forEach(form => {
        form.addEventListener('submit', function(e) {
            const msg = this.getAttribute('onsubmit');
            if (msg && msg.includes('confirm')) {
                // Already handled by inline onsubmit
            }
        });
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) target.scrollIntoView({ behavior: 'smooth' });
        });
    });

    // Form validation helpers
    document.querySelectorAll('input[type="password"]').forEach(input => {
        input.addEventListener('input', function() {
            if (this.value.length > 0 && this.value.length < 6) {
                this.style.borderColor = '#ef4444';
            } else {
                this.style.borderColor = '';
            }
        });
    });
});

// ── AJAX Helper for API Calls ──
function apiRequest(endpoint, method = 'GET', data = null, callback = null) {
    const xhr = new XMLHttpRequest();
    xhr.open(method, 'includes/api-bridge.php?endpoint=' + encodeURIComponent(endpoint), true);
    xhr.setRequestHeader('Content-Type', 'application/json');
    xhr.onreadystatechange = function() {
        if (xhr.readyState === 4) {
            let response = null;
            try { response = JSON.parse(xhr.responseText); } catch(e) { response = xhr.responseText; }
            if (callback) callback(xhr.status, response);
        }
    };
    xhr.send(data ? JSON.stringify(data) : null);
}
