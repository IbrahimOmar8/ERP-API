/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  output: 'standalone',
  compress: true,
  poweredByHeader: false,
  productionBrowserSourceMaps: false,
  experimental: {
    // Tree-shake the heaviest icon library so we only ship the icons we use.
    optimizePackageImports: ['lucide-react', '@tanstack/react-query'],
  },
};

module.exports = nextConfig;
