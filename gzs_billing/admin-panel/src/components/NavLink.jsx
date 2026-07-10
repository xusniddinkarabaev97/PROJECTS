export function NavLink({ to, children, collapsed }) {
  const isActive = window.location.hash === `#${to}`;
  return (
    <a
      href={`#${to}`}
      className={`flex items-center px-3 py-2 rounded mb-1 transition-colors ${
        isActive
          ? "bg-[#1f6feb] text-white"
          : "text-[#8b949e] hover:bg-[#21262d] hover:text-[#c9d1d9]"
      } ${collapsed ? "justify-center" : ""}`}
      title={collapsed ? children?.toString() : undefined}
    >
      {children}
    </a>
  );
}
