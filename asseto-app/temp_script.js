
  const DOC_TYPE_LABELS = {onboarding:'Приём сотрудника',dismissal:'Увольнение',writeoff:'Списание',transfer:'Акт приёма-передачи'};
  var PRIORITY_COLOR = {high:'var(--ios-red)',normal:'var(--accent)',low:'var(--text3)'};
  var PRIORITY_LABEL = {high:'Срочно',normal:'Обычный',low:'Низкий'};
  let chartCatPie = null, chartCondPie = null;


  function fmtDate(s) {
    if (!s) return '—';
    return new Date(s).toLocaleDateString('ru-RU', {day:'2-digit',month:'short'});
  }

  function renderDocItem(doc) {
    return `<div onclick="window.location='/documents?id=${doc.id}'"
      style="display:flex;align-items:center;gap:12px;padding:14px 16px;border-bottom:1px solid var(--separator2);cursor:pointer;transition:background .12s;"
      onmouseover="this.style.background='var(--surface2)'" onmouseout="this.style.background=''">
      <div style="width:36px;height:36px;border-radius:10px;background:rgba(0,122,255,.1);display:flex;align-items:center;justify-content:center;flex-shrink:0;">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--accent)" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
      </div>
      <div style="flex:1;min-width:0;">
        <div style="font-size:14px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${doc.title||doc.doc_number}</div>
        <div style="font-size:12px;color:var(--text2);margin-top:2px;">${DOC_TYPE_LABELS[doc.doc_type]||doc.doc_type} · ${doc.doc_number} · ${fmtDate(doc.created_at)}</div>
      </div>
      <span style="font-size:11px;font-weight:700;padding:3px 8px;border-radius:20px;background:${PRIORITY_COLOR[doc.priority]||'var(--accent)'};color:#fff;flex-shrink:0;">${PRIORITY_LABEL[doc.priority]||''}</span>
    </div>`;
  }

  // Override render to match the 10-column layout of dash_unified
  window.render = function() {
    try {
      let flt = Q ? items.filter(i =>
        [i.inv_num, i.model, i.employee, i.place, i.room, i.category].join(' ').toLowerCase().includes(Q.toLowerCase())
      ) : items;
      if (F.condition) flt = flt.filter(i => i.condition === F.condition);
      if (F.category) flt = flt.filter(i => i.category === F.category);
      if (F.status) flt = flt.filter(i => i.status === F.status);
      
      const start = curPage * PER;
      const slice = flt.slice(start, start + PER);
      const isEmpty = !flt.length;

      const emptyDesk = document.getElementById('empty-desk');
      if (emptyDesk) emptyDesk.style.display = isEmpty ? 'block' : 'none';
      
      const tbody = document.getElementById('tbody');
      if (tbody) {
          tbody.innerHTML = slice.map(i => {
              const checked = (window.bulkSelectedIds && window.bulkSelectedIds.has(i.id.toString())) ? 'checked' : '';
              return `
              <tr onclick="openEdit(${i.id})" class="fade-in">
                  <td style="text-align:center;" onclick="event.stopPropagation()">
                    <input type="checkbox" class="apple-checkbox row-checkbox" value="${i.id}" onchange="window.updateBulkSelection()" ${checked}>
                  </td>
                  <td><span class="inv-tag">${i.inv_num}</span></td>
                  <td style="font-size:13px;">${i.place || '—'}</td>
                  <td style="font-size:13px; color:var(--text2);">${i.category}</td>
                  <td style="font-weight:600;">${i.model || '—'}</td>
                  <td style="font-size:13px;">${i.room || '—'}</td>
                  <td>
                      ${i.employee && i.employee !== '—' ? `
                          <div style="display:flex; align-items:center; gap:8px;">
                              <div style="width:24px; height:24px; border-radius:50%; background:var(--accent); color:#fff; font-size:10px; font-weight:800; display:flex; align-items:center; justify-content:center;">${i.employee[0].toUpperCase()}</div>
                              <span style="font-size:13px; font-weight:500;">${i.employee}</span>
                          </div>
                      ` : '<span style="color:var(--text3);">—</span>'}
                  </td>
                  <td><span class="status-badge ${SM[i.status] || 'chip-free'}"><span class="chip-dot"></span>${i.status}</span></td>
                  <td><span class="status-badge ${CM[i.condition] || 'chip-good'}"><span class="chip-dot"></span>${i.condition || 'Хорошее'}</span></td>
                  <td style="text-align:right;">
                      <button class="btn btn-secondary btn-xs" onclick="event.stopPropagation();openEdit(${i.id})">
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                      </button>
                  </td>
              </tr>`;
          }).join('');
      }

      // Pagination
      const pg = document.getElementById('pager-desk');
      if (pg) {
          let phtml = '';
          const tPages = Math.ceil(flt.length / PER);
          if (tPages > 1) {
              for (let i=0; i<tPages; i++) {
                  phtml += `<button class="pg-btn${i===curPage?' active':''}" onclick="navTo(${i})">${i+1}</button>`;
              }
          }
          pg.innerHTML = phtml;
      }
    } catch(err) {
      const tbody = document.getElementById('tbody');
      if (tbody) tbody.innerHTML = `<tr><td colspan="10" style="color:red;padding:20px;">Render Error: ${err.message}</td></tr>`;
    }
  }

  // Global Error Handler for UI debugging
  window.addEventListener('error', function(e) {
    const main = document.querySelector('.main-content');
    if(main) {
      const errDiv = document.createElement('div');
      errDiv.style.cssText = "background: #f8d7da; color: #721c24; padding: 15px; margin-bottom: 20px; border-radius: 8px; font-family: monospace; z-index: 9999;";
      errDiv.innerHTML = `<strong>JS Error:</strong> ${e.message} <br><small>at ${e.filename}:${e.lineno}</small>`;
      main.insertBefore(errDiv, main.firstChild);
    }
  });

  window.addEventListener('unhandledrejection', function(e) {
    const main = document.querySelector('.main-content');
    if(main) {
      const errDiv = document.createElement('div');
      errDiv.style.cssText = "background: #f8d7da; color: #721c24; padding: 15px; margin-bottom: 20px; border-radius: 8px; font-family: monospace; z-index: 9999;";
      errDiv.innerHTML = `<strong>Promise Error:</strong> ${e.reason}`;
      main.insertBefore(errDiv, main.firstChild);
    }
  });


  function renderEmpty(msg) {
    return `<div style="padding:28px;text-align:center;color:var(--text3);font-size:13px;">${msg}</div>`;
  }
  function renderErr() {
    return `<div style="padding:20px;text-align:center;color:var(--ios-red);font-size:13px;">Ошибка загрузки</div>`;
  }

  function buildExecCharts(a) {
    const dark = isDark();
    const txtClr = dark ? '#94A3B8' : '#4B5563';

    // Pie chart for Categories
    const catCanvas = document.getElementById('chartCatPie');
    if (catCanvas && a.by_cat && a.by_cat.length) {
      if (chartCatPie) chartCatPie.destroy();
      chartCatPie = new Chart(catCanvas.getContext('2d'), {
        type: 'doughnut',
        data: {
          labels: a.by_cat.map(d => d.category),
          datasets: [{
            data: a.by_cat.map(d => d.cnt),
            backgroundColor: PALETTE,
            borderWidth: 0,
            hoverOffset: 4
          }]
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          cutout: '60%',
          plugins: { legend: { position: 'right', labels: { color: txtClr, font: { size: 11 } } } }
        }
      });
    }

    // Pie chart for Conditions
    const condCanvas = document.getElementById('chartCondPie');
    if (condCanvas && a.by_condition && a.by_condition.length) {
      if (chartCondPie) chartCondPie.destroy();
      const condColors = {'Хорошее':'#34C759', 'Потёрто':'#FF9500', 'Требует ремонта':'#FF3B30', 'Списано':'#8E8E93'};
      chartCondPie = new Chart(condCanvas.getContext('2d'), {
        type: 'bar',
        data: {
          labels: a.by_condition.map(d => d.condition),
          datasets: [{
            data: a.by_condition.map(d => d.cnt),
            backgroundColor: a.by_condition.map(d => condColors[d.condition] || '#4F46E5'),
            borderRadius: 6
          }]
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            y: { beginAtZero: true, grid: { color: isDark() ? '#333' : '#e5e7eb' } },
            x: { grid: { display: false } }
          }
        }
      });
    }
  }

  async function loadExecutiveData() {
    try {
      // 1. Fetch Stats & Toxic Assets
      const a = await (await fetch('/api/analytics')).json();
      const valEl = document.getElementById('exec-val');
      if (valEl) valEl.textContent = new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(a.total_value);
      
      const toxicEl = document.getElementById('toxic-list');
      if (toxicEl) {
        toxicEl.innerHTML = a.toxic_assets.map(x => `
          <div class="mini-list-item" style="display:flex;align-items:center;gap:14px;padding:12px;border-bottom:1px solid var(--border);cursor:pointer;transition:background 0.2s;border-radius:12px;" onmouseover="this.style.background='var(--surface2)'" onmouseout="this.style.background='transparent'" onclick="document.getElementById('search-input').value='${x.inv_num}'; doSearch('${x.inv_num}'); document.getElementById('main-table').scrollIntoView({behavior: 'smooth'})">
            <div class="mli-ico" style="background:rgba(255,59,48,0.1); color:var(--ios-red); width:36px; height:36px; border-radius:10px; display:flex; align-items:center; justify-content:center;">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
            </div>
            <div class="mli-body" style="flex:1;">
              <b style="font-size:14px;color:var(--text);">${x.model}</b>
              <div style="font-size:12px;color:var(--text2);margin-top:4px;display:flex;align-items:center;gap:8px;">
                <span style="padding:2px 6px;background:var(--surface2);border-radius:4px;font-weight:600;">${x.inv_num}</span>
                <span style="color:var(--ios-red);">${x.repair_count} ремонтов</span>
              </div>
            </div>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--text3)" stroke-width="2" stroke-linecap="round"><polyline points="9 18 15 12 9 6"/></svg>
          </div>
        `).join('') || '<div class="empty-mini" style="font-size:13px;color:var(--text3);padding:20px;text-align:center;background:var(--surface2);border-radius:12px;">Проблемных активов нет</div>';
      }

      const eolEl = document.getElementById('eol-list');
      if (eolEl) {
        eolEl.innerHTML = a.eol_assets.map(x => `
          <div class="mini-list-item" style="display:flex;align-items:center;gap:14px;padding:12px;border-bottom:1px solid var(--border);cursor:pointer;transition:background 0.2s;border-radius:12px;" onmouseover="this.style.background='var(--surface2)'" onmouseout="this.style.background='transparent'" onclick="document.getElementById('search-input').value='${x.inv_num}'; doSearch('${x.inv_num}'); document.getElementById('main-table').scrollIntoView({behavior: 'smooth'})">
            <div class="mli-ico" style="background:rgba(255,149,0,0.1); color:var(--ios-orange); width:36px; height:36px; border-radius:10px; display:flex; align-items:center; justify-content:center;">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M5 22h14"/><path d="M5 2h14"/><path d="M17 22v-4.172a2 2 0 0 0-.586-1.414L12 12l-4.414 4.414A2 2 0 0 0 7 17.828V22"/><path d="M7 2v4.172a2 2 0 0 0 .586 1.414L12 12l4.414-4.414A2 2 0 0 0 17 6.172V2"/></svg>
            </div>
            <div class="mli-body" style="flex:1;">
              <b style="font-size:14px;color:var(--text);">${x.model}</b>
              <div style="font-size:12px;color:var(--text2);margin-top:4px;display:flex;align-items:center;gap:8px;">
                <span style="padding:2px 6px;background:var(--surface2);border-radius:4px;font-weight:600;">${x.inv_num}</span>
                <span>Куплен ${x.purchase_date}</span>
              </div>
            </div>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--text3)" stroke-width="2" stroke-linecap="round"><polyline points="9 18 15 12 9 6"/></svg>
          </div>
        `).join('') || '<div class="empty-mini" style="font-size:13px;color:var(--text3);padding:20px;text-align:center;background:var(--surface2);border-radius:12px;">Устаревшей техники нет</div>';
      }
      buildExecCharts(a);

      // 2. Load Role Pending Docs
      const resp = await fetch('/api/dashboard/role-stats');
      const data = await resp.json();
      
      const execEl = document.getElementById('exec-pending-docs');
      if (execEl) {
        const docs = data.pending_docs || [];
        const execDocs = document.getElementById('exec-docs');
        const execTotal = document.getElementById('exec-total');
        if (execDocs) execDocs.textContent = data.pending_docs_count || 0;
        if (execTotal) execTotal.textContent = data.total_items || docs.length;
        execEl.innerHTML = docs.length
          ? docs.map(renderDocItem).join('')
          : renderEmpty('Нет документов, ожидающих вашей подписи');
      }

      // 3. Load Users
      const uRes = await fetch('/api/users');
      const users = await uRes.json();
      const uList = document.getElementById('exec-users-list');
      if (uList && users && users.length) {
        const rolesMap = {
          superadmin: {l:'Супер-Админ', c:'#5856D6'},
          director: {l:'Директор', c:'#FF3B30'},
          hr: {l:'HR', c:'#34C759'},
          aho: {l:'АХО', c:'#007AFF'},
          accountant: {l:'Бухгалтер', c:'#30B0C7'},
          employee: {l:'Сотрудник', c:'#8E8E93'}
        };
        uList.innerHTML = users.slice(0, 10).map(u => {
          const r = rolesMap[u.role] || {l:u.role, c:'#8E8E93'};
          const activeHtml = u.active ? '<span style="color:#34C759;">Активен</span>' : '<span style="color:#FF3B30;">Неактивен</span>';
          const itemsCountHtml = u.items_count > 0 
            ? `<div style="font-size:12px;color:var(--accent);font-weight:600;margin-top:4px;">📦 Техники: ${u.items_count} шт.</div>` 
            : `<div style="font-size:12px;color:var(--text3);margin-top:4px;">Нет техники</div>`;

          return `
            <tr style="border-bottom:1px solid var(--border);transition:background .1s;cursor:pointer;" onmouseover="this.style.background='var(--surface2)'" onmouseout="this.style.background=''" onclick="document.getElementById('search-input').value='${u.name}'; doSearch('${u.name}'); document.getElementById('main-table').scrollIntoView({behavior: 'smooth'})">
              <td style="padding:12px;font-size:13px;font-weight:600;">
                <div style="display:flex;align-items:center;gap:10px;">
                  <div style="width:32px;height:32px;border-radius:50%;background:${r.c}20;color:${r.c};display:flex;align-items:center;justify-content:center;font-size:14px;">${(u.name||'?')[0].toUpperCase()}</div>
                  <div>
                    <div>${u.name || '—'}</div>
                    <div style="font-size:11px;color:var(--text2);font-weight:400;margin-top:2px;">${u.email}</div>
                    ${itemsCountHtml}
                  </div>
                </div>
              </td>
              <td style="padding:12px;font-size:13px;color:var(--text2);">${u.department || '—'}</td>
              <td style="padding:12px;">
                <span style="font-size:11px;font-weight:600;padding:3px 8px;border-radius:12px;background:${r.c}1A;color:${r.c};">${r.l}</span>
              </td>
              <td style="padding:12px;font-size:12px;font-weight:500;">${activeHtml}</td>
            </tr>
          `;
        }).join('');
      }
    } catch (e) {
      console.error(e);
      document.getElementById('exec-pending-docs').innerHTML = renderErr();
    }
  }

  window.addEventListener('load', loadExecutiveData);

  function openAdd() {
    curStep = 1;
    document.getElementById('m-add').classList.add('open');
    showStep(1);
    // Clear inputs
    ['a-place','a-room','a-emp','a-model','a-serial','a-notes'].forEach(id=> {
      const el = document.getElementById(id);
      if(el) el.value = '';
    });
  }
  function showStep(s) {
    [1,2,3].forEach(step => {
      document.getElementById('step-'+step).style.display = step === s ? 'block' : 'none';
      document.getElementById('sp-'+step).classList.toggle('on', step === s);
    });
    document.getElementById('btn-prev').style.display = s > 1 ? 'block' : 'none';
    document.getElementById('btn-next').innerText = s === 3 ? 'Создать ✓' : 'Далее →';
  }
  function nextStep() {
    if (curStep < 3) {
      curStep++;
      showStep(curStep);
    } else {
      createAssetSubmit();
    }
  }
  function prevStep() {
    if (curStep > 1) {
      curStep--;
      showStep(curStep);
    }
  }
  async function createAssetSubmit() {
    const payload = {
      place: document.getElementById('a-place').value,
      room: document.getElementById('a-room').value,
      employee: document.getElementById('a-emp').value || '—',
      category: document.getElementById('a-cat').value,
      model: document.getElementById('a-model').value,
      serial_num: document.getElementById('a-serial').value,
      condition: document.getElementById('a-cond').value,
      notes: document.getElementById('a-notes').value,
      status: document.getElementById('a-emp').value ? 'Занято' : 'Свободно'
    };
    try {
      const r = await fetch('/api/items', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify(payload)
      });
      const d = await r.json();
      if (d.ok) {
        showToast('Техника добавлена ✓', 'ok');
        closeM('m-add');
        refresh();
      } else {
        showToast(d.error || 'Ошибка', 'err');
      }
    } catch(e) {
      showToast('Ошибка сети', 'err');
    }
  }

  // Edit modal
  async function openEdit(id) {
    const r = await (await fetch('/api/items')).json();
    const item = r.find(x => x.id === id);
    if (!item) return;
    document.getElementById('e-id').value = item.id;
    document.getElementById('e-inv').value = item.inv_num;
    document.getElementById('e-place').value = item.place;
    document.getElementById('e-room').value = item.room;
    document.getElementById('e-cat').value = item.category;
    document.getElementById('e-model').value = item.model || '';
    document.getElementById('e-serial').value = item.serial_num || '';
    document.getElementById('e-emp').value = item.employee || '';
    document.getElementById('e-status').value = item.status;
    document.getElementById('e-cond').value = item.condition;
    document.getElementById('e-notes').value = item.notes || '';
    
    // Load Item History
    try {
      const hres = await fetch(`/api/items/${item.id}/history`);
      const hdata = await hres.json();
      const hlist = document.getElementById('item-hist-list');
      hlist.innerHTML = hdata.map(h => `
        <div style="font-size:12px;padding:6px;background:var(--surface2);border-radius:6px;">
          <b>${h.action}</b> · <span style="color:var(--text3);">${new Date(h.ts).toLocaleDateString('ru')}</span>
        </div>
      `).join('') || '<div style="font-size:11px;color:var(--text3);">Нет истории изменений</div>';
    } catch(e) {}

    document.getElementById('m-edit').classList.add('open');
  }

  async function saveEdit() {
    const id = document.getElementById('e-id').value;
    const payload = {
      place: document.getElementById('e-place').value,
      room: document.getElementById('e-room').value,
      category: document.getElementById('e-cat').value,
      model: document.getElementById('e-model').value,
      serial_num: document.getElementById('e-serial').value,
      employee: document.getElementById('e-emp').value || '—',
      status: document.getElementById('e-status').value,
      condition: document.getElementById('e-cond').value,
      notes: document.getElementById('e-notes').value
    };
    const r = await fetch('/api/items/' + id, {
      method: 'PUT',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload)
    });
    const d = await r.json();
    if (d.ok) {
      showToast('Сохранено', 'ok');
      closeM('m-edit');
      refresh();
    } else {
      showToast(d.error || 'Ошибка', 'err');
    }
  }

  async function delItem() {
    if (!await showConfirm('Удаление', 'Удалить этот актив? Действие необратимо.')) return;
    const id = document.getElementById('e-id').value;
    await fetch('/api/items/' + id, { method: 'DELETE' });
    closeM('m-edit');
    showToast('Удалено', 'ok');
    refresh();
  }

  // Filters and actions
  function filterByCat(c) {
    F.category = c;
    load();
  }
  function resetAllFilters() {
    F = { status: '', room: '', category: '', employee: '', condition: '' };
    document.getElementById('search-input').value = '';
    Q = '';
    load();
  }
  
  // Excel import selectors
  function openImport() { document.getElementById('m-import').classList.add('open'); }
  async function handleFileSelect(file) {
    if(!file) return;
    const fd = new FormData();
    fd.append('file', file);
    const r = await fetch('/api/items/import', { method:'POST', body: fd });
    const j = await r.json();
    if (j.ok) {
      showToast(`Импортировано: ${j.imported}`, 'ok');
      closeM('m-import');
      refresh();
    } else {
      document.getElementById('imp-result').textContent = j.error || 'Ошибка';
    }
  }

  // AI OCR scanner
  async function handleOCR(file) {
    if(!file) return;
    const fd = new FormData();
    fd.append('file', file);
    showToast('AI сканирует накладную...', 'ok');
    const r = await fetch('/api/ai/ocr', { method:'POST', body: fd });
    const j = await r.json();
    if (j.error) {
      showToast(j.error, 'err');
      return;
    }
    showToast('Скан успешен! ' + j.message, 'ok');
    
    // Automatically open the "Add" modal for the first item
    if (j.items && j.items.length > 0) {
      const first = j.items[0];
      openAdd();
      setTimeout(() => {
        const catSelect = document.getElementById('a-cat');
        // Ensure category matches one of options
        Array.from(catSelect.options).forEach(opt => {
          if (opt.value.toLowerCase() === first.category.toLowerCase() || opt.value.includes(first.category)) {
            catSelect.value = opt.value;
          }
        });
        document.getElementById('a-model').value = first.model || '';
        showStep(2); // Jump to step 2 directly
      }, 300);
    }
  }

  // Init tables load
  window.addEventListener('load', () => {
    refresh();
  });

