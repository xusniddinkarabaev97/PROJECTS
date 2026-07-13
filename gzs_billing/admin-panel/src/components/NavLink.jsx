export function NavLink({ to, children, collapsed }) {
  const isActive = window.location.hash === `#${to}`;
  return (
    <a
      href={`#${to}`}
      className={`flex items-center gap-3 px-3 py-2.5 rounded-xl transition text-sm no-underline ${
        isActive
          ? "bg-gradient-to-r from-teal-500/10 to-emerald-500/5 text-teal-400 border border-teal-500/20 shadow-sm shadow-teal-500/5"
          : "text-slate-400 hover:text-white hover:bg-slate-800/50"
      } ${collapsed ? "justify-center px-2" : ""}`}
      title={collapsed && typeof children === "string" ? children : undefined}
    >
      {children}
    </a>
  );
}
