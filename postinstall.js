#!/usr/bin/env node
/**
 * postinstall.js — DlcManager
 * Creates the conventional folder structure expected by the package.
 */
const fs   = require('fs');
const path = require('path');

const root    = path.resolve(__dirname, '..', '..', '..');
const folders = [
  path.join(root, 'Assets', 'DlcPacks'),
  path.join(root, 'Assets', 'Resources', 'DlcPacks'),
  path.join(root, 'Assets', 'Scripts'),
];

folders.forEach(dir => {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
    console.log(`[DlcManager] Created: ${dir}`);
  } else {
    console.log(`[DlcManager] Already exists: ${dir}`);
  }
});
