"use client";

import { useEffect, useRef, useState } from "react";

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (options: {
            client_id: string;
            callback: (response: { credential: string }) => void;
          }) => void;
          renderButton: (element: HTMLElement, options: Record<string, string | number>) => void;
        };
      };
    };
  }
}

interface GoogleLoginButtonProps {
  onCredential: (credential: string) => Promise<void>;
}

export default function GoogleLoginButton({ onCredential }: GoogleLoginButtonProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [unavailable, setUnavailable] = useState(false);
  const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
  const onCredentialRef = useRef(onCredential);
  onCredentialRef.current = onCredential;

  useEffect(() => {
    if (!clientId) {
      setUnavailable(true);
      return;
    }

    let rendered = false;

    const render = () => {
      if (!window.google || !containerRef.current || rendered) {
        return;
      }

      try {
        window.google.accounts.id.initialize({
          client_id: clientId,
          callback: ({ credential }) => {
            onCredentialRef.current(credential).catch(() => undefined);
          },
        });
        containerRef.current.replaceChildren();
        window.google.accounts.id.renderButton(containerRef.current, {
          theme: "outline",
          size: "large",
          width: 360,
        });
        rendered = true;
      } catch (e) {
        console.warn("[GoogleLoginButton] Render error:", e);
      }
    };

    const existing = document.querySelector<HTMLScriptElement>(
      'script[src="https://accounts.google.com/gsi/client"]'
    );
    if (existing) {
      if (window.google) {
        render();
      } else {
        existing.addEventListener("load", render);
        return () => existing.removeEventListener("load", render);
      }
      return;
    }

    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.onload = render;
    script.onerror = () => setUnavailable(true);
    document.head.appendChild(script);
  }, [clientId]);

  if (unavailable) {
    return <p className="text-center text-sm text-gray-500">Google Login chưa được cấu hình.</p>;
  }

  return <div ref={containerRef} className="flex min-h-10 justify-center" aria-label="Đăng nhập với Google" />;
}
