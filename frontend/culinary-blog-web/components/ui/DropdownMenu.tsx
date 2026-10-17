"use client";

import * as React from "react";
import { cn } from "@/lib/utils";

interface DropdownContextValue {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

const DropdownContext = React.createContext<DropdownContextValue | undefined>(
  undefined
);

export function DropdownMenu({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = React.useState(false);
  const containerRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        containerRef.current &&
        !containerRef.current.contains(event.target as Node)
      ) {
        setOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    if (open) {
      document.addEventListener("mousedown", handleClickOutside);
      document.addEventListener("keydown", handleKeyDown);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  return (
    <DropdownContext.Provider value={{ open, setOpen }}>
      <div ref={containerRef} className="relative inline-block text-left">
        {children}
      </div>
    </DropdownContext.Provider>
  );
}

export function DropdownMenuTrigger({
  children,
  className,
  ...props
}: {
  children: React.ReactNode;
  className?: string;
} & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  const context = React.useContext(DropdownContext);
  if (!context) throw new Error("DropdownMenuTrigger must be used inside DropdownMenu");

  const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
    props.onClick?.(e);
    context.setOpen((prev) => !prev);
  };

  return (
    <button
      type="button"
      onClick={handleClick}
      aria-expanded={context.open}
      aria-haspopup="true"
      className={cn("inline-flex items-center justify-center cursor-pointer", className)}
      {...props}
    >
      {children}
    </button>
  );
}

export function DropdownMenuContent({
  align = "right",
  className,
  children,
  ...props
}: {
  align?: "left" | "right" | "center";
  className?: string;
  children: React.ReactNode;
} & React.HTMLAttributes<HTMLDivElement>) {
  const context = React.useContext(DropdownContext);
  if (!context) throw new Error("DropdownMenuContent must be used inside DropdownMenu");

  if (!context.open) return null;

  const alignClasses =
    align === "left"
      ? "left-0"
      : align === "center"
      ? "left-1/2 -translate-x-1/2"
      : "right-0";

  return (
    <div
      role="menu"
      className={cn(
        "absolute z-50 mt-2 min-w-[12rem] overflow-hidden rounded-xl border border-gray-100 bg-white p-1 text-gray-800 shadow-xl ring-1 ring-black/5 animate-in fade-in-0 zoom-in-95",
        alignClasses,
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}

export function DropdownMenuItem({
  className,
  children,
  onClick,
  ...props
}: {
  className?: string;
  children: React.ReactNode;
  onClick?: (e: React.MouseEvent<HTMLDivElement>) => void;
} & React.HTMLAttributes<HTMLDivElement>) {
  const context = React.useContext(DropdownContext);

  const handleClick = (e: React.MouseEvent<HTMLDivElement>) => {
    onClick?.(e);
    context?.setOpen(false);
  };

  return (
    <div
      role="menuitem"
      tabIndex={0}
      onClick={handleClick}
      className={cn(
        "relative flex cursor-pointer select-none items-center gap-2 rounded-lg px-3 py-2 text-sm outline-none transition-colors hover:bg-emerald-50 hover:text-emerald-900 focus:bg-emerald-50 focus:text-emerald-900",
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}

export function DropdownMenuLabel({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("px-3 py-2 text-xs font-semibold uppercase tracking-wider text-gray-500", className)}
      {...props}
    />
  );
}

export function DropdownMenuSeparator({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      role="separator"
      className={cn("-mx-1 my-1 h-px bg-gray-100", className)}
      {...props}
    />
  );
}
