const { spawn } = require("node:child_process");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const project = path.join(root, "native", "ServerRock.Native", "ServerRock.Native.csproj");
const framework = process.env.SERVER_ROCK_NATIVE_FRAMEWORK || "net10.0-desktop";
const args = ["run", "--project", project, "--framework", framework];

const child = spawn("dotnet", args, {
  cwd: root,
  env: process.env,
  stdio: "inherit",
  windowsHide: false,
});

const forwardSignal = (signal) => {
  if (!child.killed) child.kill(signal);
};
process.on("SIGINT", () => forwardSignal("SIGINT"));
process.on("SIGTERM", () => forwardSignal("SIGTERM"));

child.on("error", (error) => {
  console.error(`Unable to start the native app with dotnet: ${error.message}`);
  console.error("Install the .NET 10 SDK or run the transitional Electron path with: npm run start:electron");
  process.exitCode = 1;
});

child.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exitCode = code ?? 1;
});
