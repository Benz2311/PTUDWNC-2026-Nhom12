import React from "react";

export default function PublicRouteLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="w-full min-h-[calc(100vh-4rem-16rem)]">
      {children}
    </div>
  );
}
