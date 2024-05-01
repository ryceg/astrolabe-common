/** @type {import('next').NextConfig} */
const nextConfig = {
	output: "export",
	transpilePackages: ["hvams-common"],
	images: {
		unoptimized: true,
	},
	reactStrictMode: false,
};

module.exports = nextConfig;
