export function NavLink({ to, children, collapsed }) {
  const isActive = window.location.hash === `#${to}`;
  return (
    <a
      href={`#${to}`}
      style={{
        display: "flex",
        alignItems: "center",
        padding: "10px 12px",
        borderRadius: 10,
        marginBottom: 4,
        textDecoration: "none",
        fontSize: 14,
        fontWeight: 600,
        transition: "all 0.2s",
        background: isActive
          ? "linear-gradient(135deg, rgba(91,140,62,0.15) 0%, rgba(61,107,37,0.08) 100%)"
          : "transparent",
        color: isActive ? "#5b8c3e" : "var(--text-secondary)",
        border: isActive ? "1px solid rgba(91,140,62,0.25)" : "1px solid transparent",
        justifyContent: collapsed ? "center" : "flex-start",
      }}
      onMouseEnter={(e) => {
        if (!isActive) {
          e.target.style.background = "rgba(91,140,62,0.06)";
          e.target.style.color = "#5b8c3e";
        }
      }}
      onMouseLeave={(e) => {
        if (!isActive) {
          e.target.style.background = "transparent";
          e.target.style.color = "var(--text-secondary)";
        }
      }}
      title={collapsed ? children?.toString() : undefined}
    >
      {children}
    </a>
  );
}
