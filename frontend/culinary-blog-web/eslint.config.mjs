import { dirname } from "path";
import { fileURLToPath } from "url";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

const eslintConfig = [
  // Next.js + TypeScript base rules
  ...compat.extends("next/core-web-vitals", "next/typescript"),

  // Airbnb ruleset (compatible with flat config via FlatCompat)
  ...compat.extends("airbnb", "airbnb-typescript"),

  // Prettier MUST be last — disables rules that conflict with Prettier formatting
  ...compat.extends("prettier"),

  {
    rules: {
      // Allow default exports (required by Next.js pages/app)
      "import/prefer-default-export": "off",
      // Allow .tsx extension for JSX
      "react/jsx-filename-extension": ["warn", { extensions: [".tsx", ".jsx"] }],
      // Next.js handles <a> tags differently
      "jsx-a11y/anchor-is-valid": "off",
      // Allow spreading props
      "react/jsx-props-no-spreading": "off",
      // Allow functions as React components without .displayName
      "react/display-name": "off",
    },
  },

  {
    ignores: [
      "node_modules/**",
      ".next/**",
      "out/**",
      "build/**",
      "next-env.d.ts",
      "coverage/**",
      "playwright-report/**",
      "test-results/**",
    ],
  },
];

export default eslintConfig;
