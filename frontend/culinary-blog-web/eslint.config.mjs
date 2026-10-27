import { dirname } from "path";
import { fileURLToPath } from "url";
import { createRequire } from "module";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const require = createRequire(import.meta.url);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

const tsPlugin = require("@typescript-eslint/eslint-plugin");
const validTsRules = new Set(Object.keys(tsPlugin.rules).map((r) => `@typescript-eslint/${r}`));

const sanitizeConfig = (configs) =>
  configs.map((config) => {
    if (!config.rules) return config;
    const rules = { ...config.rules };
    for (const rule of Object.keys(rules)) {
      if (rule.startsWith("@typescript-eslint/") && !validTsRules.has(rule)) {
        delete rules[rule];
      }
    }
    return { ...config, rules };
  });

const eslintConfig = [
  {
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: __dirname,
      },
    },
  },

  // Next.js + TypeScript base rules
  ...compat.extends("next/core-web-vitals", "next/typescript"),

  // Airbnb ruleset (compatible with flat config via FlatCompat)
  ...sanitizeConfig(compat.extends("airbnb", "airbnb-typescript")),

  // Prettier MUST be last — disables rules that conflict with Prettier formatting
  ...compat.extends("prettier"),

  {
    rules: {
      // React 17+ / Next.js JSX transform does not require React in scope
      "react/react-in-jsx-scope": "off",
      "react/jsx-uses-react": "off",
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
      "import/extensions": "off",
      "import/no-extraneous-dependencies": "off",
      "import/order": "off",
      "no-param-reassign": "off",
      "no-underscore-dangle": "off",
      "consistent-return": "off",
      "@typescript-eslint/return-await": "off",
      "@typescript-eslint/no-explicit-any": "off",
      "@typescript-eslint/no-use-before-define": "off",
      "@typescript-eslint/no-unused-vars": "warn",
      "prefer-destructuring": "off",
      "prefer-template": "off",
      "react/require-default-props": "off",
      "react/prop-types": "off",
      "arrow-body-style": "off",
      "react/button-has-type": "off",
      "jsx-a11y/heading-has-content": "off",
      "no-nested-ternary": "off",
      "react/no-unstable-nested-components": "off",
      "react/function-component-definition": "off",
      "react/jsx-no-constructed-context-values": "off",
      "jsx-a11y/click-events-have-key-events": "off",
      "@typescript-eslint/no-empty-object-type": "off",
      "no-restricted-exports": "off",
      "react/no-array-index-key": "off",
      "jsx-a11y/label-has-associated-control": "off",
      "jsx-a11y/control-has-associated-label": "off",
      "jsx-a11y/no-noninteractive-element-interactions": "off",
      "jsx-a11y/no-static-element-interactions": "off",
      "lines-around-directive": "off",
      "react-hooks/exhaustive-deps": "off",
      "no-alert": "off",
      "no-restricted-globals": "off",
      "no-console": "off",
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
      "eslint.config.mjs",
      "postcss.config.mjs",
      "jest.config.ts",
      "jest.config.mjs",
    ],
  },
];

export default eslintConfig;
