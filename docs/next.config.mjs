const isProd = process.env.NODE_ENV === 'production';
import { createMDX } from 'fumadocs-mdx/next';

const withMDX = createMDX();

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',
  basePath: isProd ? '/Jade' : '',
  assetPrefix: isProd ? '/Jade/' : '',
  images: {
    unoptimized: true,
  },
};

export default withMDX(nextConfig);
