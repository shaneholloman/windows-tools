#!/usr/bin/env bun
// Renders an SVG to PNG, scaling up so the smallest dimension is at least 2048px.

import { readFileSync, writeFileSync, existsSync } from 'fs';
import { dirname, basename, join, resolve } from 'path';
import { Resvg } from '@resvg/resvg-js';

const MIN_DIM = 2048;
const SEP = '  ' + '\u2500'.repeat(58);

const rawArg = (process.argv[2] ?? '').replace(/^"|"$/g, '');
if (!rawArg || !existsSync(rawArg)) {
  console.error(`\n  ERROR: File not found: ${rawArg || '(no path given)'}\n`);
  process.exit(1);
}

const inputPath  = resolve(rawArg);
const outputPath = join(dirname(inputPath), basename(inputPath, '.svg') + '.png');
const svgData    = readFileSync(inputPath);

console.log('');
console.log('\x1b[36m  SVG TO PNG\x1b[0m');
console.log(SEP);
console.log(`  Input : ${basename(inputPath)}`);
console.log(`\x1b[90m  Dir   : ${dirname(inputPath)}\x1b[0m`);
console.log(SEP);
console.log('');

// Measure natural dimensions at 1:1 scale
const natural = new Resvg(svgData, { fitTo: { mode: 'zoom', value: 1 } });
const nw = natural.width;
const nh = natural.height;

if (nw === 0 || nh === 0) {
  console.error('  ERROR: Could not determine SVG dimensions (width or height is 0).');
  console.error('  The SVG may be missing width/height attributes and a viewBox.\n');
  process.exit(1);
}

const minDim = Math.min(nw, nh);
const scale  = minDim < MIN_DIM ? MIN_DIM / minDim : 1;
const outW   = Math.round(nw * scale);
const outH   = Math.round(nh * scale);

console.log(`\x1b[90m  Natural : ${nw} x ${nh} px\x1b[0m`);
if (scale > 1) {
  console.log(`\x1b[90m  Scale   : ${scale.toFixed(3)}x  (min dim ${minDim} -> ${MIN_DIM})\x1b[0m`);
  console.log(`\x1b[90m  Output  : ${outW} x ${outH} px\x1b[0m`);
} else {
  console.log(`\x1b[90m  Output  : ${outW} x ${outH} px  (min dim already >= ${MIN_DIM})\x1b[0m`);
}
console.log('');
console.log('\x1b[90m  Rendering...\x1b[0m');

const resvg = new Resvg(svgData, { fitTo: { mode: 'zoom', value: scale } });
const png   = resvg.render().asPng();
writeFileSync(outputPath, png);

console.log(`  \x1b[32mSaved : ${basename(outputPath)}\x1b[0m`);
console.log('');
