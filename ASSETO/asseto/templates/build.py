import sys

with open("dash_unified.html", "r", encoding="utf-8") as f:
    uni = f.read()

with open("dash_operations.html", "r", encoding="utf-8") as f:
    ops = f.read()

# 1. In uni, find the insertion point for inventory: before </div>\n</div>\n{% endblock %}
parts = uni.split("  </div>\n</div>\n{% endblock %}")
uni_top = parts[0]
uni_bottom = "  </div>\n</div>\n{% endblock %}" + parts[1]

# 2. In ops, extract everything from "<!-- Category Filter Bar -->" up to "{% endblock %}" (excluding it)
ops_parts = ops.split("<!-- Category Filter Bar -->")
ops_inventory = "<!-- Category Filter Bar -->" + ops_parts[1].split("{% endblock %}")[0]

# Add a header for Inventory
inventory_section = """
    <!-- ══ INVENTORY SECTION ══ -->
    <div style="margin-top:30px;background:var(--surface);border:.5px solid var(--border);border-radius:24px;padding:24px;" class="fade-up-delay-3">
      <div style="font-weight:700;font-size:18px;margin-bottom:16px;">Управление оборудованием</div>
""" + ops_inventory + "</div>\n"

# 3. Combine
final_html = uni_top + inventory_section + uni_bottom

# 4. Wrap executive only parts
final_html = final_html.replace('<!-- Pending Docs — самое важное для директора -->', 
    "{% if current_user.role in ('superadmin', 'director', 'deputy') %}\n    <!-- Pending Docs — самое важное для директора -->")
final_html = final_html.replace('</div>\n    </div>\n\n    <div class="charts-row', 
    "</div>\n    </div>\n    {% endif %}\n\n    <div class=\"charts-row")

final_html = final_html.replace('<div class="charts-row fade-up-delay-3"', 
    "{% if current_user.role in ('superadmin', 'director', 'deputy') %}\n    <div class=\"charts-row fade-up-delay-3\"")
final_html = final_html.replace('<!-- USERS LIST SECTION -->', 
    "{% endif %}\n\n    <!-- USERS LIST SECTION -->")

# 5. Fix the Chart Cond Pie to be a Bar chart
old_pie = """chartCondPie = new Chart(condCanvas.getContext('2d'), {
        type: 'pie',
        data: {
          labels: a.by_condition.map(d => d.condition),
          datasets: [{
            data: a.by_condition.map(d => d.cnt),
            backgroundColor: a.by_condition.map(d => condColors[d.condition] || '#4F46E5'),
            borderWidth: 0,
            hoverOffset: 4
          }]
        },
        options: {
          responsive: true, maintainAspectRatio: false,
          plugins: { legend: { position: 'right', labels: { color: txtClr, font: { size: 11 } } } }
        }
      });"""

new_bar = """chartCondPie = new Chart(condCanvas.getContext('2d'), {
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
      });"""
final_html = final_html.replace(old_pie, new_bar)

with open("dash_unified.html", "w", encoding="utf-8") as f:
    f.write(final_html)

print("Done")
