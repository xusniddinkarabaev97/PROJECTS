
const ME={role:"1",name:"1",id:1};
const CHAIN=1,DT=1;
const PL={low:"🟢 Низкий",normal:"🔵 Обычный",high:"🟠 Высокий",urgent:"🔴 Срочно"};
const CLR=["#007AFF","#AF52DE","#34C759","#FF9500","#5856D6","#5AC8FA","#FF3B30","#30B0C7"];
let docs=[],curId=null,curAct=null;
let sigPad=null, sigCtx=null, drawing=false;

function initSigPad() {
  const canvas = document.getElementById('sig-pad');
  if (!canvas) return;
  sigPad = canvas;
  sigCtx = canvas.getContext('2d');
  
  // Resize to actual display size
  const resize = () => {
    const r = canvas.getBoundingClientRect();
    canvas.width = r.width;
    canvas.height = r.height;
    sigCtx.strokeStyle = getComputedStyle(document.documentElement).getPropertyValue('--text').trim() || '#000';
    sigCtx.lineWidth = 3;
    sigCtx.lineJoin = 'round';
    sigCtx.lineCap = 'round';
  };
  window.addEventListener('resize', resize);
  resize();

  const getPos = (e) => {
    const r = canvas.getBoundingClientRect();
    const x = (e.touches ? e.touches[0].clientX : e.clientX) - r.left;
    const y = (e.touches ? e.touches[0].clientY : e.clientY) - r.top;
    return [x, y];
  };

  const start = (e) => { drawing = true; const [x,y] = getPos(e); sigCtx.beginPath(); sigCtx.moveTo(x,y); e.preventDefault(); };
  const move = (e) => { if(!drawing) return; const [x,y] = getPos(e); sigCtx.lineTo(x,y); sigCtx.stroke(); e.preventDefault(); };
  const stop = () => { drawing = false; };

  canvas.addEventListener('mousedown', start); canvas.addEventListener('mousemove', move); window.addEventListener('mouseup', stop);
  canvas.addEventListener('touchstart', start); canvas.addEventListener('touchmove', move); window.addEventListener('touchend', stop);
}

function clearSig() { if(sigCtx) sigCtx.clearRect(0, 0, sigPad.width, sigPad.height); }

function fmt(s){if(!s)return"—";const d=new Date((s.includes("T")?s:s.replace(" ","T"))+"Z");return isNaN(d)?s:d.toLocaleString("ru",{day:"2-digit",month:"short",year:"numeric",hour:"2-digit",minute:"2-digit"});}
function fmtD(s){if(!s)return"—";const d=new Date(s.includes("T")?s:s+"T00:00:00");return isNaN(d)?s:d.toLocaleDateString("ru",{day:"2-digit",month:"long",year:"numeric"});}

function getIcon(st){
  const m={
    pending: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>`,
    approved:`<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="20 6 9 17 4 12"/></svg>`,
    rejected:`<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`,
    printed: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>`,
    draft:   `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>`
  };
  return m[st]||m.draft;
}
function getPIcon(p){
  const m={
    low:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="7 13 12 18 17 13"/><polyline points="7 6 12 11 17 6"/></svg>`,
    normal: ``,
    high:   `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="17 11 12 6 7 11"/><polyline points="17 18 12 13 7 18"/></svg>`,
    urgent: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"/></svg>`
  };
  return m[p]||"";
}

function sb(st){
  const m={
    pending: ["badge-pending", "На рассмотрении"],
    approved:["badge-approved","Утверждено"],
    rejected:["badge-rejected","Отклонено"],
    printed: ["badge-printed", "Закрыт"],
    draft:   ["badge-draft",   "Черновик"]
  };
  const [c, l] = m[st] || ["badge-draft", st];
  return `<span class="badge ${c}">${getIcon(st)} ${l}</span>`;
}
function pb(p){
  if(!p || p==="normal") return "";
  const m={low:"badge-draft", high:"badge-high", urgent:"badge-urgent"};
  return `<span class="badge ${m[p]||""}">${getPIcon(p)} ${PL[p]||p}</span>`;
}

async function loadDocs(sf=""){docs=await(await fetch("/api/documents"+(sf?`?status=${sf}`:""))).json();renderList();document.getElementById("dsb-count").textContent=docs.length+" документ(ов)";}
async function loadStats(){const s=await(await fetch("/api/documents/stats")).json();document.getElementById("st-p").textContent=s.pending;document.getElementById("st-a").textContent=s.approved;document.getElementById("st-t").textContent=s.total;if(s.my_pending>0){document.getElementById("pending-badge").textContent=`Ждут: ${s.my_pending}`;document.getElementById("pending-badge").style.display="inline-flex";document.getElementById("my-p-txt").textContent=`${s.my_pending} документ(а) ждут вас`;document.getElementById("my-p").classList.add("show");}}
function filterDocs(st,btn){document.querySelectorAll(".dsb-filter").forEach(b=>b.classList.remove("on"));btn.classList.add("on");loadDocs(st);}
function renderList(){
  const el=document.getElementById("doc-list");
  const query=document.getElementById("doc-search").value.toLowerCase();
  let filtered=docs;
  if(query) filtered=docs.filter(d=>[d.doc_number,d.title,d.created_by_name].some(v=>v.toLowerCase().includes(query)));
  
  if(!filtered.length){
    el.innerHTML='<div style="padding:40px;text-align:center;color:var(--text3);font-size:13px;">Ничего не найдено</div>';
    return;
  }
  const today = new Date(); today.setHours(0,0,0,0);
  el.innerHTML=filtered.map(d=>{
    // Deadline color coding
    let deadlineBadge = "";
    if(d.deadline && d.status==="pending"){
      const dl = new Date(d.deadline); dl.setHours(0,0,0,0);
      const diff = Math.round((dl - today) / 86400000);
      if(diff < 0) deadlineBadge = `<span style="font-size:10px;font-weight:800;padding:2px 7px;border-radius:10px;background:rgba(255,59,48,.15);color:#FF3B30;">Просрочено</span>`;
      else if(diff === 0) deadlineBadge = `<span style="font-size:10px;font-weight:800;padding:2px 7px;border-radius:10px;background:rgba(255,59,48,.15);color:#FF3B30;">Сегодня!</span>`;
      else if(diff <= 2) deadlineBadge = `<span style="font-size:10px;font-weight:800;padding:2px 7px;border-radius:10px;background:rgba(255,149,0,.15);color:#FF9500;">Через ${diff}д</span>`;
      else deadlineBadge = `<span style="font-size:10px;font-weight:600;padding:2px 7px;border-radius:10px;background:rgba(52,199,89,.1);color:#34C759;">до ${fmtD(d.deadline)}</span>`;
    }
    return `
    <div class="doc-item${d.id===curId?" active":""}" id="dli-${d.id}" onclick="openDoc(${d.id})">
      <div class="di-top">
        <span class="di-num">${d.doc_number}</span>
        ${deadlineBadge}
        <div style="margin-left:auto;">${sb(d.status)}</div>
      </div>
      <div class="di-title">${d.title}</div>
      <div class="di-meta">${DT[d.doc_type]||d.doc_type} · ${d.created_by_name}</div>
      <div class="di-footer">
        <span>${fmtD(d.created_at)}</span>
        <span style="display:flex;align-items:center;gap:4px;">
          <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="opacity:0.5;"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
          ${d.approved_steps||0}/${d.total_steps||0}
          ${d.comment_count>0?` · <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="opacity:0.5;"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg> ${d.comment_count}`:""}
        </span>
      </div>
    </div>`;
  }).join("");
}

async function openDoc(id){curId=id;document.querySelectorAll(".doc-item").forEach(e=>e.classList.remove("active"));document.getElementById("dli-"+id)?.classList.add("active");const r=await(await fetch("/api/documents/"+id)).json();render(r);if(window.innerWidth<=768)document.getElementById("doc-sidebar").classList.remove("open");}
function render({doc,approvals,comments,chain}){
  document.getElementById("doc-empty").style.display="none";
  document.getElementById("doc-detail").classList.add("show");
  document.getElementById("dd-num").textContent=doc.doc_number;
  document.getElementById("dd-sbadge").innerHTML=sb(doc.status);
  document.getElementById("dd-pbadge").innerHTML=pb(doc.priority);
  document.getElementById("dd-title").textContent=doc.title;
  document.getElementById("dd-meta").textContent=`${DT[doc.doc_type]||doc.doc_type} · ${doc.created_by_name} · ${fmtD(doc.created_at)}`;
  if(doc.description){document.getElementById("dd-desc-wrap").style.display="block";document.getElementById("dd-desc").textContent=doc.description;}else document.getElementById("dd-desc-wrap").style.display="none";
  document.getElementById("dd-chain").innerHTML=chain.map(s=>{const a=approvals.find(x=>x.step===s.step);let cls="",ico=s.step;if(a?.action==="approved"){cls="done";ico=`<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="4"><polyline points="20 6 9 17 4 12"/></svg>`;}else if(a?.action==="rejected"){cls="rejected";ico=`<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="4"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>`;}else if(doc.current_step===s.step&&doc.status==="pending"){cls="current";ico=`<svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor"><circle cx="12" cy="12" r="10"/></svg>`;}return`<div class="chain-step ${cls}"><div class="cs-dot">${ico}</div><div class="cs-name">${s.label}</div></div>`;}).join("");
  document.getElementById("dd-appr").innerHTML=chain.map(s=>{const a=approvals.find(x=>x.step===s.step);let cls="pending",ico="…",who="",tm="",cmt="",sig="";if(a?.action==="approved"){cls="approved";ico=`<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="4"><polyline points="20 6 9 17 4 12"/></svg>`;who=a.approver_name||"";tm=fmt(a.acted_at);cmt=a.comment||"";if(a.signature)sig=`<div style="margin-top:8px;"><img src="${a.signature}" style="max-height:50px; border-radius:8px; background:#fff; border:1px solid var(--separator2);"></div>`;}else if(a?.action==="rejected"){cls="rejected";ico=`<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="4"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>`;who=a.approver_name||"";tm=fmt(a.acted_at);cmt=a.comment||"";}else if(doc.current_step===s.step&&doc.status==="pending"){cls="current";ico=`<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" class="spin"><circle cx="12" cy="12" r="10" stroke-dasharray="10 20"/></svg>`;}return`<div class="appr-item"><div class="appr-dot ${cls}">${ico}</div><div style="flex:1;"><div class="appr-role">${s.label}${cls==="current"?` <span style="font-size:10px;color:var(--ios-blue);font-weight:800;letter-spacing:0.5px;">(ОЖИДАЕТ)</span>`:""}</div>${who?`<div class="appr-who">${who}</div>`:""}${tm?`<div class="appr-time">${tm}</div>`:""}${cmt?`<div class="appr-cmt">${cmt}</div>`:""}${sig}</div></div>`;}).join("");
  document.getElementById("dd-comments").innerHTML=comments.map((c,i)=>`<div class="comment-item"><div class="comment-ava" style="background:${CLR[i%CLR.length]}">${(c.user_name||"?")[0].toUpperCase()}</div><div class="comment-bub"><div class="comment-who"><span>${c.user_name||"Система"}</span><span style="color:var(--text3)">${fmt(c.created_at)}</span></div><div class="comment-txt">${c.text}</div></div></div>`).join("")||'<div style="font-size:12px;color:var(--text3);padding:5px 0">Нет комментариев</div>';
  document.getElementById("dd-info").innerHTML=`<div class="info-row"><span class="info-key">Номер</span><span class="info-val" style="font-family:monospace">${doc.doc_number}</span></div><div class="info-row"><span class="info-key">Тип</span><span class="info-val">${DT[doc.doc_type]||doc.doc_type}</span></div><div class="info-row"><span class="info-key">Создал</span><span class="info-val">${doc.created_by_name}</span></div><div class="info-row"><span class="info-key">Дата</span><span class="info-val">${fmtD(doc.created_at)}</span></div>${doc.deadline?`<div class="info-row"><span class="info-key">Дедлайн</span><span class="info-val" style="color:#FF9500">${fmtD(doc.deadline)}</span></div>`:""}`;
  renderBanner(doc,chain);renderActions(doc,chain);
}
function renderBanner(doc,chain){
  const el=document.getElementById("role-banner");
  const canAppr=doc.status==="pending"&&(ME.role===doc.current_role||ME.role==="superadmin");
  const isMine=doc.created_by_name===ME.name;
  const currStep = chain.find(x=>x.role===doc.current_role) || {};
  
  if(canAppr){
    el.style.display="flex"; el.className="role-banner rb-warn";
    el.innerHTML=`<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                  <div style="display:flex; flex-direction:column; gap:2px;">
                    <span style="font-size:15px;">Документ ждёт вашего решения</span>
                    <span style="font-size:12px; opacity:0.8;">Вы согласуете как: <b>${currStep.label||ME.role}</b></span>
                  </div>`;
  }
  else if(isMine && doc.status==="pending"){
    el.style.display="flex"; el.className="role-banner rb-info";
    el.innerHTML=`<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                  <div style="display:flex; flex-direction:column; gap:2px;">
                    <span style="font-size:15px;">Заявка на рассмотрении</span>
                    <span style="font-size:12px; opacity:0.8;">Сейчас у: <b>${currStep.label||doc.current_role}</b></span>
                  </div>`;
  }
  else if(doc.status==="approved" && ME.role==="accountant"){
    el.style.display="flex"; el.className="role-banner rb-ok";
    el.innerHTML=`<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="20 6 9 17 4 12"/></svg>
                  <span>Полностью согласован — распечатайте и закройте документ</span>`;
  }
  else if(doc.status==="approved" && doc.doc_type==="onboarding" && (ME.role==="aho"||ME.role==="superadmin")){
    el.style.display="flex"; el.className="role-banner rb-ok";
    el.innerHTML=`<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M20 12V22H4V12"/><path d="M22 7H2v5h20V7z"/><path d="M12 22V7"/></svg>
                  <span>Документ утверждён — выдайте оборудование сотруднику <b>${doc.employee_name||''}</b></span>`;
  }
  else if(doc.status==="rejected" && isMine){
    el.style.display="flex"; el.className="role-banner rb-tip";
    el.style.background="rgba(255,59,48,0.1)"; el.style.borderColor="rgba(255,59,48,0.2)"; el.style.color="#FF3B30";
    el.innerHTML=`<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>
                  <span>Документ отклонен — ознакомьтесь с комментариями и создайте новый</span>`;
  }
  else el.style.display="none";
}
function renderActions(doc,chain){
  const el=document.getElementById("dd-act");
  const canAppr=doc.status==="pending"&&(ME.role===doc.current_role||ME.role==="superadmin");
  const currStep = chain.find(x=>x.role===doc.current_role) || {};
  let h="";
  if(canAppr){
    h+=`<button class="btn-approve" onclick="openAppr('approved','${doc.doc_number}')" style="box-shadow:0 8px 20px rgba(52,199,89,0.3);">✅ Согласовать</button>
        <button class="btn-reject" onclick="openAppr('rejected','${doc.doc_number}')">❌ Отклонить</button>`;
  }
  if(doc.status==="approved" && (ME.role==="accountant"||ME.role==="superadmin")){
    h+=`<button class="btn-print-close" onclick="printClose(${doc.id})" style="box-shadow:0 8px 20px rgba(99,102,241,0.3);">🖨️ Распечатать и закрыть</button>`;
  }
  if(!h){
    if(doc.status==="pending") h=`<div style="display:flex; align-items:center; gap:8px; font-size:14px; font-weight:700; color:var(--text3);"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg> Ожидает подписи: <span style="color:var(--ios-blue)">${currStep.label||doc.current_role}</span></div>`;
    else if(doc.status==="approved") h=`<span style="font-size:14px; font-weight:800; color:#34C759;">✅ ДОКУМЕНТ УТВЕРЖДЁН</span>`;
    else if(doc.status==="rejected") h=`<span style="font-size:14px; font-weight:800; color:#FF3B30;">❌ ДОКУМЕНТ ОТКЛОНЁН</span>`;
    else if(doc.status==="printed") h=`<span style="font-size:14px; font-weight:800; color:var(--ios-blue);">🖨️ ДОКУМЕНТ ЗАКРЫТ (РАСПЕЧАТАН)</span>`;
  }
  // Issue equipment button for AHO on approved onboarding docs
  if(doc.status==="approved" && doc.doc_type==="onboarding" && (ME.role==="aho"||ME.role==="superadmin")){
    h+=`<button class="btn-approve" onclick="issueOnboarding(${doc.id})" style="background:linear-gradient(135deg,#007AFF,#5856D6);box-shadow:0 8px 20px rgba(0,122,255,0.3);">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M20 12V22H4V12"/><path d="M22 7H2v5h20V7z"/><path d="M12 22V7"/></svg>
          Выдать оборудование
        </button>`;
  }
  // Always append print + forward for any open doc
  h+=`<button class="btn-secondary" onclick="printDoc()" title="Распечатать документ">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>
        Печать
      </button>
      <button class="btn-secondary" onclick="forwardDoc(${doc.id},'${doc.doc_number}')" title="Скопировать ссылку">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/></svg>
        Переслать
      </button>`;
  el.innerHTML=h;
}

function printDoc(){
  const pd = document.getElementById('print-date');
  if (pd) pd.textContent = new Date().toLocaleString('ru-RU');
  window.print();
}

async function issueOnboarding(id){
  if(!confirm('Создать выдачу оборудования для сотрудника? Документ будет закрыт.')) return;
  const r=await fetch(`/api/documents/${id}/issue-onboarding`,{method:'POST'});
  const d=await r.json();
  if(d.ok){toast('Выдача создана #'+d.issuance_id+' ✓','ok');await loadDocs();openDoc(id);}
  else toast(d.error||'Ошибка','err');
}

function forwardDoc(id, num){
  const url = window.location.origin + '/documents?id=' + id;
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(url).then(()=>{
      toast('Ссылка скопирована: ' + num, 'ok');
    }).catch(async ()=> await showAlert('Ссылка для копирования', url));
  } else {
    await showAlert('Ссылка для копирования', url);
  }
}
function closeDetail(){curId=null;document.getElementById("doc-detail").classList.remove("show");document.getElementById("doc-empty").style.display="flex";document.querySelectorAll(".doc-item").forEach(e=>e.classList.remove("active"));}

function openNewDoc(){updChain();document.getElementById("nd-t").value="";document.getElementById("nd-d").value="";document.getElementById("m-new").classList.add("open");}
function updChain(){const t=document.getElementById("nd-type").value,c=CHAIN[t]||[];document.getElementById("nd-chain").innerHTML=c.map((s,i)=>`<span style="display:inline-flex;align-items:center;gap:3px;font-size:11px;padding:3px 8px;background:var(--surface);border-radius:980px;"><span style="font-weight:700;color:var(--ios-blue)">${i+1}</span>${s.label}</span>${i<c.length-1?'<span style="color:var(--text3)">→</span>':""}`).join(" ");}
async function submitDoc(){const title=document.getElementById("nd-t").value.trim();if(!title){toast("Укажите название","err");return;}const r=await fetch("/api/documents",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({doc_type:document.getElementById("nd-type").value,title,description:document.getElementById("nd-d").value.trim(),priority:document.getElementById("nd-p").value,deadline:document.getElementById("nd-dl").value||null})});const d=await r.json();if(d.ok){toast(`${d.doc_number} отправлен ✓`,"ok");closeM("m-new");document.getElementById("nd-t").value="";document.getElementById("nd-d").value="";await loadDocs();await loadStats();openDoc(d.doc_id);}else toast(d.error||"Ошибка","err");}

function openAppr(act,num){
  curAct=act;
  document.getElementById("am-t").textContent=act==="approved"?"Согласовать":"Отклонить";
  document.getElementById("am-s").textContent=num;
  document.getElementById("am-c").value="";
  const b=document.getElementById("am-btn");
  b.textContent=act==="approved"?"✅ Согласовать":"❌ Отклонить";
  b.style.background=act==="approved"?"#34C759":"#FF3B30";
  
  const ss = document.getElementById("sig-section");
  if(act === "approved") { ss.style.display="block"; setTimeout(initSigPad, 100); }
  else { ss.style.display="none"; }
  
  document.getElementById("m-appr").classList.add("open");
}
async function doApprove(){
  const c=document.getElementById("am-c").value.trim();
  let sig = null;
  if(curAct === "approved") {
    // Basic check if canvas is empty (optional but good)
    sig = sigPad.toDataURL('image/png');
  }
  
  const r=await fetch(`/api/documents/${curId}/approve`,{
    method:"POST",
    headers:{"Content-Type":"application/json"},
    body:JSON.stringify({action:curAct, comment:c, signature: sig})
  });
  const d=await r.json();
  if(d.ok){
    toast(curAct==="approved"?"Согласовано ✓":"Отклонено","ok");
    closeM("m-appr");
    await loadDocs();
    await loadStats();
    openDoc(curId);
  } else toast(d.error||"Ошибка","err");
}
async function printClose(id){if(!await showConfirm("Печать документа", "Пометить как распечатано и закрыть?"))return;const r=await fetch(`/api/documents/${id}/print`,{method:"POST"});const d=await r.json();if(d.ok){toast("Закрыт 🖨️","ok");await loadDocs();await loadStats();openDoc(id);}else toast(d.error||"Ошибка","err");}
async function sendComment(){const t=document.getElementById("ci").value.trim();if(!t||!curId)return;const r=await fetch(`/api/documents/${curId}/comments`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({text:t})});if((await r.json()).ok){document.getElementById("ci").value="";openDoc(curId);}}
function closeM(id){document.getElementById(id)?.classList.remove("open");}
document.querySelectorAll(".overlay").forEach(ov=>ov.addEventListener("click",e=>{if(e.target===ov)closeM(ov.id);}));
function toast(msg,type="ok"){const t=document.getElementById("toast");t.textContent=msg;t.className="toast show"+(type==="err"?" toast-err":"");clearTimeout(t._t);t._t=setTimeout(()=>t.classList.remove("show"),2800);}
if(window.innerWidth<=768)document.getElementById("mob-sb-btn").style.display="block";
updChain();loadDocs();loadStats();
