import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  async headers() {
    return [
      "/admin/:path*",
      "/dashboard/:path*",
      "/profile",
      "/recipes/create",
      "/recipes/:slug/edit",
      "/recipes/:slug/cook",
      "/confirm-email",
      "/reset-password",
    ].map((source) => ({
      source,
      headers: [{ key: "X-Robots-Tag", value: "noindex, nofollow" }],
    }));
  },
  images: {
    remotePatterns: [
      { protocol: "http", hostname: "localhost" },
      { protocol: "http", hostname: "127.0.0.1" },
      { protocol: "http", hostname: "minio" },
      { protocol: "https", hostname: "**" },
    ],
  },
};

export default nextConfig;
