/* ASSETO Landing — Main JS */

// ── PRELOADER ────────────────────────────────────────────────────────────────
window.addEventListener('load', () => {
  const preloader = document.getElementById('preloader');
  if (preloader) {
    preloader.style.opacity = '0';
    preloader.style.visibility = 'hidden';
    setTimeout(() => preloader.remove(), 600);
  }
});

// ── AURORA CANVAS BACKGROUND ─────────────────────────────────────────────────
(function() {
  const canvas = document.getElementById('hero-canvas');
  const ctx = canvas.getContext('2d');
  let W, H, time = 0;

  function resize() {
    W = canvas.width = window.innerWidth;
    H = canvas.height = window.innerHeight;
  }
  window.addEventListener('resize', resize);
  resize();

  function drawAurora() {
    ctx.clearRect(0, 0, W, H);
    time += 0.002;

    // 1. Elegant Deep Glows (Like the reference image)
    const blobs = [
      // Top Center (behind main title) - Soft Blue/White
      { x: W * 0.5 + Math.sin(time * 0.4) * W * 0.1, y: H * 0.2 + Math.cos(time * 0.3) * H * 0.1, r: Math.max(W, H) * 0.8, c: 'rgba(50, 150, 255, 0.08)' },
      
      // Middle Left - Soft Purple
      { x: W * 0.2 + Math.cos(time * 0.5) * W * 0.1, y: H * 0.6 + Math.sin(time * 0.4) * H * 0.2, r: Math.max(W, H) * 0.7, c: 'rgba(90, 40, 200, 0.07)' },
      
      // Middle Right - Soft Blue
      { x: W * 0.8 + Math.sin(time * 0.6) * W * 0.1, y: H * 0.7 + Math.cos(time * 0.5) * H * 0.2, r: Math.max(W, H) * 0.7, c: 'rgba(20, 100, 255, 0.06)' },
    ];

    blobs.forEach(b => {
      const g = ctx.createRadialGradient(b.x, b.y, 0, b.x, b.y, b.r);
      g.addColorStop(0, b.c);
      g.addColorStop(1, 'transparent');
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, W, H);
    });

    requestAnimationFrame(drawAurora);
  }
  drawAurora();
})();

// ── NAVBAR SCROLL ────────────────────────────────────────────────────────────
window.addEventListener('scroll', () => {
  const nav = document.getElementById('navbar');
  nav.classList.toggle('scrolled', window.scrollY > 20);
});

// ── MOBILE MENU ──────────────────────────────────────────────────────────────
function toggleMenu() {
  const menu = document.getElementById('nav-mobile');
  menu.classList.toggle('open');
}

// ── SCROLL REVEAL ────────────────────────────────────────────────────────────
const observer = new IntersectionObserver((entries) => {
  entries.forEach((entry, i) => {
    if (entry.isIntersecting) {
      setTimeout(() => entry.target.classList.add('visible'), i * 60);
    }
  });
}, { threshold: 0.1, rootMargin: '0px 0px -60px 0px' });

document.querySelectorAll('.reveal').forEach(el => observer.observe(el));

// ── COUNTER ANIMATION ────────────────────────────────────────────────────────
function animateCounter(el, target, duration = 2000) {
  let start = null;
  const step = timestamp => {
    if (!start) start = timestamp;
    const progress = Math.min((timestamp - start) / duration, 1);
    const eased = 1 - Math.pow(1 - progress, 3);
    el.textContent = Math.floor(eased * target).toLocaleString('ru');
    if (progress < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

const counterObserver = new IntersectionObserver(entries => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      const el = entry.target;
      const target = parseInt(el.dataset.target);
      animateCounter(el, target);
      counterObserver.unobserve(el);
    }
  });
}, { threshold: 0.5 });

document.querySelectorAll('.stat-num[data-target]').forEach(el => counterObserver.observe(el));

// ── SMOOTH SCROLL ────────────────────────────────────────────────────────────
document.querySelectorAll('a[href^="#"]').forEach(a => {
  a.addEventListener('click', e => {
    const id = a.getAttribute('href');
    if (id === '#') return;
    const el = document.querySelector(id);
    if (el) {
      e.preventDefault();
      el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  });
});

// ── FORM SUBMIT ──────────────────────────────────────────────────────────────
function submitForm(e) {
  e.preventDefault();
  const form = e.target;
  const success = document.getElementById('form-success');
  form.style.display = 'none';
  success.style.display = 'block';
  success.style.animation = 'fadeInUp .5s ease';
}

// ── HERO PARALLAX ────────────────────────────────────────────────────────────
window.addEventListener('scroll', () => {
  const scrolled = window.scrollY;
  const hero = document.querySelector('.hero-mockup');
  if (hero && scrolled < window.innerHeight) {
    hero.style.transform = `translateY(${scrolled * 0.1}px)`;
  }
});

// ── MAGNETIC BUTTONS ──────────────────────────────────────────────────────────
document.querySelectorAll('.btn-primary, .btn-ghost').forEach(btn => {
  btn.addEventListener('mousemove', e => {
    const rect = btn.getBoundingClientRect();
    const x = e.clientX - rect.left - rect.width / 2;
    const y = e.clientY - rect.top - rect.height / 2;
    btn.style.transform = `translate(${x * 0.15}px, ${y * 0.15}px) translateY(-2px) scale(1.02)`;
  });
  btn.addEventListener('mouseleave', () => {
    btn.style.transform = '';
  });
});

// ── TYPEWRITER EFFECT (hero) ──────────────────────────────────────────────────
(function() {
  const words = ['Умно.', 'Быстро.', 'Просто.', 'Надёжно.'];
  let wi = 0, ci = 0, deleting = false;
  const target = document.querySelector('.gradient-text');
  if (!target) return;

  function type() {
    const word = words[wi];
    if (!deleting) {
      target.textContent = word.slice(0, ci + 1);
      ci++;
      if (ci === word.length) {
        deleting = true;
        setTimeout(type, 1800);
        return;
      }
    } else {
      target.textContent = word.slice(0, ci - 1);
      ci--;
      if (ci === 0) {
        deleting = false;
        wi = (wi + 1) % words.length;
      }
    }
    setTimeout(type, deleting ? 60 : 100);
  }
  setTimeout(type, 1200);
})();

// ── CURSOR GLOW (desktop only) ────────────────────────────────────────────────
if (window.innerWidth > 768) {
  const glow = document.createElement('div');
  glow.style.cssText = `
    position:fixed;width:300px;height:300px;border-radius:50%;
    background:radial-gradient(circle,rgba(0,122,255,0.06) 0%,transparent 70%);
    pointer-events:none;z-index:9999;transform:translate(-50%,-50%);
    transition:transform .1s ease,opacity .3s;
  `;
  document.body.appendChild(glow);
  window.addEventListener('mousemove', e => {
    glow.style.left = e.clientX + 'px';
    glow.style.top = e.clientY + 'px';
  });

  // Card Hover Glow effect (Apple style)
  document.querySelectorAll('.feature-card, .problem-card, .role-card, .price-card, .feature-big, .comp-side').forEach(card => {
    card.addEventListener('mousemove', e => {
      const rect = card.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      card.style.setProperty('--mouse-x', `${x}px`);
      card.style.setProperty('--mouse-y', `${y}px`);
    });
  });
}

// ── SCROLL PROGRESS BAR ───────────────────────────────────────────────────────
const progressBar = document.createElement('div');
progressBar.style.cssText = `
  position:fixed;top:0;left:0;height:3px;background:linear-gradient(90deg, var(--accent), var(--purple));
  z-index:9999;transition:width .1s;width:0;
`;
document.body.appendChild(progressBar);

window.addEventListener('scroll', () => {
  const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
  const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
  progressBar.style.width = (winScroll / height) * 100 + '%';
});

// ── RIPPLE EFFECT ────────────────────────────────────────────────────────────
document.querySelectorAll('.btn-primary, .btn-ghost').forEach(btn => {
  btn.addEventListener('click', function(e) {
    const rect = this.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    const circle = document.createElement('span');
    circle.classList.add('ripple');
    const diameter = Math.max(rect.width, rect.height);
    circle.style.width = circle.style.height = `${diameter}px`;
    circle.style.left = `${x - diameter / 2}px`;
    circle.style.top = `${y - diameter / 2}px`;
    
    this.appendChild(circle);
    setTimeout(() => circle.remove(), 600);
  });
});

// ── 3D SCROLL SCRUBBING ──────────────────────────────────────────────────────
const heroMockup = document.querySelector('.hero-mockup');
if (heroMockup) {
  window.addEventListener('scroll', () => {
    const scrollY = window.scrollY;
    const maxScroll = 600;
    const progress = Math.min(scrollY / maxScroll, 1);
    
    const rx = progress * 15; // tilt up
    const ry = progress * -10; // pan left
    
    heroMockup.style.setProperty('--rx', `${rx}deg`);
    heroMockup.style.setProperty('--ry', `${ry}deg`);
  });
}

// ── FAQ ACCORDION ────────────────────────────────────────────────────────────
document.querySelectorAll('.faq-q').forEach(q => {
  q.addEventListener('click', () => {
    const item = q.parentElement;
    const answer = item.querySelector('.faq-a');
    
    if (item.classList.contains('active')) {
      item.classList.remove('active');
      answer.style.maxHeight = '0';
    } else {
      item.classList.add('active');
      answer.style.maxHeight = answer.scrollHeight + 'px';
    }
  });
});

// ── ADD FADE-IN ANIMATION CSS ──────────────────────────────────────────────────
const style = document.createElement('style');
style.textContent = `
@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
`;
document.head.appendChild(style);

console.log('%cASSETO Landing v1.0', 'color:#007AFF;font-size:16px;font-weight:bold;');
