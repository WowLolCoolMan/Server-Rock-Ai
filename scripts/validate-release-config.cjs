const requiredWhenSigning = [
  'WINDOWS_CODESIGN_CERT_BASE64',
  'WINDOWS_CODESIGN_CERT_PASSWORD',
  'WINDOWS_CODESIGN_TIMESTAMP_URL',
  'RELEASE_GPG_PRIVATE_KEY',
];

if (process.env.RELEASE_SIGNING_READY === 'true') {
  const missing = requiredWhenSigning.filter((name) => !String(process.env[name] || '').trim());
  if (missing.length) {
    console.error(`Release signing is enabled but required material is missing: ${missing.join(', ')}`);
    process.exit(1);
  }
}

const version = String(process.env.RELEASE_VERSION || '').replace(/^v/, '');
if (version && !/^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$/.test(version)) {
  console.error(`Invalid release version: ${version}`);
  process.exit(1);
}

console.log('Release configuration is internally consistent.');
