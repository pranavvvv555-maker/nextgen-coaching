const fs = require('fs');
const path = require('path');
const packageRoot = path.dirname(require.resolve('motion/package.json'));
const { version } = require('motion/package.json');
const destination = path.join(__dirname, '..', 'wwwroot', 'js', 'vendor');
fs.mkdirSync(destination, { recursive: true });
fs.copyFileSync(path.join(packageRoot, 'dist', 'motion.js'), path.join(destination, `motion-${version}.js`));
fs.copyFileSync(path.join(packageRoot, 'LICENSE.md'), path.join(destination, 'MOTION-LICENSE.md'));
console.log(`Copied Motion ${version} for local serving.`);
