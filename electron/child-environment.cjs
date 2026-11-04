function isSensitiveEnvironmentName(name) {
  return name.includes("API_KEY") ||
    name.includes("TOKEN") ||
    name.includes("SECRET") ||
    name.includes("PASSWORD") ||
    name.includes("CREDENTIAL") ||
    name.includes("PRIVATE_KEY") ||
    name.includes("AUTHORIZATION") ||
    name.includes("COOKIE");
}

function createSafeChildEnvironment(source = process.env) {
  const environment = {};
  for (const [name, value] of Object.entries(source || {})) {
    if (!isSensitiveEnvironmentName(name.toUpperCase())) environment[name] = value;
  }
  return environment;
}

module.exports = { createSafeChildEnvironment, isSensitiveEnvironmentName };
