export function NavLink({ to, children, collapsed }) {
  const isActive = window.location.hash === `#${to}`;
  return (
    <a
      href={`#${to}`}
      style={{
        display: "flex",
        alignItems: "center",
        padding: "10px 12px",
        borderRadius: 8,
        marginBottom: 4,
        textDecoration: "none",
        fontSize: 14,
        fontWeight: 500,
        transition: "all 0.15s",
        background: isActive ? "var(--accent)" : "transparent",
        color: isActive ? "#fff" : "var(--text-secondary)",
        justifyContent: collapsed ? "center" : "flex-start",
      }}
      onMouseEnter={(e) => {
        if (!isActive) {
          e.target.style.background = "var(--bg-hover)";
          e.target.style.color = "var(--text-primary)";
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
