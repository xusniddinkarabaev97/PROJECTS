import os

for f in ['landing/index.html', 'landing/index_en.html', 'landing/index_uz.html']:
    with open(f, 'r', encoding='utf-8') as file:
        content = file.read()
    
    cdns = """
  <!-- Advanced Animations -->
  <script src="https://unpkg.com/@studio-freight/lenis@1.0.42/dist/lenis.min.js"></script>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/gsap.min.js"></script>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/ScrollTrigger.min.js"></script>
  <script src="js/main.js\">"""
    
    if 'gsap.min.js' not in content:
        content = content.replace('<script src="js/main.js">', cdns)
        with open(f, 'w', encoding='utf-8') as file:
            file.write(content)
print('CDNs injected')
