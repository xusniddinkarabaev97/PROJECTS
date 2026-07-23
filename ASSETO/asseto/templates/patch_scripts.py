import sys

with open("dash_operations.html", "r", encoding="utf-8") as f:
    ops = f.read()

# Extract script content from ops
# Find start of scripts:
script_start = ops.find("  function openAdd()")
if script_start == -1:
    print("Could not find openAdd in operations")
    sys.exit(1)

script_end = ops.find("</script>", script_start)
ops_script = ops[script_start:script_end]

with open("dash_unified.html", "r", encoding="utf-8") as f:
    uni = f.read()

# Insert before </script>
insert_pos = uni.rfind("</script>")
final_uni = uni[:insert_pos] + "\n" + ops_script + "\n" + uni[insert_pos:]

with open("dash_unified.html", "w", encoding="utf-8") as f:
    f.write(final_uni)

print("Patched successfully")
