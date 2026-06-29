/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  transpilePackages: ["@accounting/ui", "@accounting/types"],
};

export default nextConfig;
