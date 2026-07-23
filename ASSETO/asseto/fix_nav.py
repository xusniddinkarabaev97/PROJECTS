import os
for f in ['landing/index.html', 'landing/index_en.html', 'landing/index_uz.html']:
    with open(f, 'r', encoding='utf-8') as file:
        c = file.read()
    
    # Check if mobile switcher is already there
    if 'class="lang-switcher-mobile"' not in c:
        current_lang = 'ru'
        if '_en' in f: current_lang = 'en'
        if '_uz' in f: current_lang = 'uz'
        
        switcher = f"""<div class="lang-switcher-mobile" style="display:flex; justify-content:center; gap:16px; padding:12px; font-size:16px; font-weight:800; border-bottom:1px solid var(--border);">
      <a href="index.html" style="color: {'var(--accent)' if current_lang == 'ru' else 'var(--text2)'}; text-decoration:none;">RU</a>
      <a href="index_en.html" style="color: {'var(--accent)' if current_lang == 'en' else 'var(--text2)'}; text-decoration:none;">EN</a>
      <a href="index_uz.html" style="color: {'var(--accent)' if current_lang == 'uz' else 'var(--text2)'}; text-decoration:none;">UZ</a>
    </div>"""
        
        c = c.replace('<div class="nav-mobile" id="nav-mobile">', '<div class="nav-mobile" id="nav-mobile">\n    ' + switcher)
        
        with open(f, 'w', encoding='utf-8') as file:
            file.write(c)
