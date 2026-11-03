const { test, expect } = require("@playwright/test");
const path = require("node:path");
const fs = require("node:fs/promises");
const os = require("node:os");
const { createWorkspace } = require("../electron/workspace.cjs");
test("slash command parser supports fast, image and workflow actions", async () => {
  const { parseSlashCommands } = require("../commands.js");
  expect(parseSlashCommands("/fast /imagegen draw a cat")).toMatchObject({
    text: "draw a cat",
    fast: true,
    image: true,
  });
  expect(parseSlashCommands("/plan create an app")).toMatchObject({
    text: "create an app",
    plan: true,
  });
  expect(parseSlashCommands("/status")).toMatchObject({
    text: "",
    status: true,
  });
  expect(parseSlashCommands("/review this diff")).toMatchObject({
    text: "this diff",
    review: true,
  });
  expect(parseSlashCommands("/goal make a todo app")).toMatchObject({
    text: "make a todo app",
    goal: true,
  });
  expect(parseSlashCommands("normal message")).toMatchObject({
    text: "normal message",
    fast: false,
    image: false,
  });
});
test("workspace boots, shows command help, and opens preferences", async ({
  page,
}) => {
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await expect(
    page.getByRole("heading", { name: "What are we building today?" }),
  ).toBeVisible();
  await expect(page.locator("#prompt")).toHaveAttribute(
    "placeholder",
    /\/imagegen/,
  );
  await page.locator("#settings").click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await expect(page.locator("#providerSelect")).toHaveValue("Ollama");
});
test("workspace exposes model and tool entry points and advanced temperature controls", async ({ page }) => {
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await expect(page.locator("#openModelLibrary")).toBeVisible();
  await page.locator("#openConnections").click();
  await expect(page.locator("#settingsDialog")).toBeVisible();
  await page.locator("#settingsDialog .close").click();
  await page.locator(".advanced-menu summary").click();
  await page.locator("#temperatureControl").fill("1.2");
  await expect(page.locator("#temperatureValue")).toHaveText("1.2");
});
test("Core updates expose clear sign-in, stable check, download and recovery states", async ({ page }) => {
  await page.addInitScript(() => {
    let signedIn = true;
    window.desktopModels = {
      aiUpdateStatus: async () => ({ configured: true, signedIn, currentVersion: "0.1.2" }),
      aiUpdateSignIn: async () => { signedIn = true; return { signedIn }; },
      aiUpdateSignOut: async () => { signedIn = false; return { signedIn }; },
      aiUpdateCheck: async (ring) => ({ version: ring === "stable" ? "0.1.3" : "0.1.4-beta.1", currentVersion: "0.1.2", rollbackVersion: "0.1.2", available: true }),
      aiUpdateDownload: async (_ring, version) => ({ opened: true, version: version || "0.1.3" }),
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#settings").click();
  await page.locator("#coreUpdates summary").click();
  await expect(page.locator("#coreAccountStatus")).toHaveText(/Signed in/);
  await page.locator("#coreUpdateCheck").click();
  await expect(page.locator("#coreUpdateStatus")).toContainText("Version 0.1.3 is ready");
  await expect(page.locator("#coreUpdateDownload")).toBeEnabled();
  await expect(page.locator("#coreUpdateRollback")).toBeEnabled();
  await page.locator("#coreUpdateDownload").click();
  await expect(page.locator("#coreUpdateStatus")).toContainText("Complete the operating-system install");
  await page.locator("#coreUpdateRollback").click();
  await expect(page.locator("#coreUpdateStatus")).toContainText("verified recovery installer 0.1.2");
});
test("Core update actions stay disabled when this build has no pinned distribution configuration", async ({ page }) => {
  await page.addInitScript(() => {
    window.desktopModels = { aiUpdateStatus: async () => ({ configured: false, signedIn: false, currentVersion: "0.1.2" }) };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#settings").click();
  await page.locator("#coreUpdates summary").click();
  await expect(page.locator("#coreAccountStatus")).toHaveText("Core updates are not configured in this build.");
  await expect(page.locator("#coreAccountAction")).toBeDisabled();
  await expect(page.locator("#coreUpdateCheck")).toBeDisabled();
});
test("/fast selects the configured fast model and strips command before sending", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "big-model",
          fastModel: "small-model",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = { getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }), streamProviderChat: async (body, onChunk) => { window.lastProviderRequest = body; onChunk("hello"); }, cancelProviderChat: () => {} };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.evaluate(() => {});
  const checkRequest = async () => {
    const body = await page.evaluate(() => window.lastProviderRequest);
    expect(body.model).toBe("small-model");
    expect(body.messages.at(-1).content).toBe("say hello");
  };
  await page.locator("#prompt").fill("/fast say hello");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("hello");
  await checkRequest();
});
test("/goal creates a structured plan request", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "planner",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = { getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }), streamProviderChat: async (body, onChunk) => {
      window.lastPlanRequest = body;
      onChunk("Objective: build a recipe app.");
    }, cancelProviderChat: () => {} };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("/goal build a recipe app");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText(
    "Objective: build a recipe app",
  );
  const request = await page.evaluate(() => window.lastPlanRequest);
  expect(request.model).toBe("planner");
  expect(request.messages.at(-1).content).toContain("Create a practical plan");
  expect(request.messages.at(-1).content).toContain("build a recipe app");
});
test("voice toggle is present and explains unavailable recognition", async ({
  page,
}) => {
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.evaluate(() => {
    window.SpeechRecognition = undefined;
    window.webkitSpeechRecognition = undefined;
  });
  page.once("dialog", (d) => d.accept());
  await page.locator("#voice").click();
  await expect(page.locator("#voice")).not.toHaveClass(/recording/);
});

test("/imagegen sends prompt to ComfyUI", async ({ page }) => {
  await page.addInitScript(() => {
    const workflow = {
      1: { inputs: { text: "{{prompt}}" }, class_type: "Test" },
    };
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "x",
          comfyEndpoint: "http://127.0.0.1:8188",
          imageWorkflow: JSON.stringify(workflow),
        },
      }),
    );
  });
  await page.route("http://127.0.0.1:8188/prompt", async (route) => {
    const req = route.request().postDataJSON();
    expect(req.prompt["1"].inputs.text).toBe("a blue bird");
    await route.fulfill({ json: { prompt_id: "test-job" } });
  });
  await page.route("http://127.0.0.1:8188/history/test-job", (route) =>
    route.fulfill({ json: { "test-job": { outputs: {} } } }),
  );
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("/imagegen a blue bird");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText(
    "Image generation complete",
  );
});

test("Hugging Face catalog filters by selected task and chooses a result", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "old/model",
          hfTask: "text-generation",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      searchHuggingFace: async (_query, task) => {
        if (task !== "text-generation") throw Error("unexpected task filter");
        return [{
          id: "org/model-a",
          pipeline_tag: "text-generation",
          cardData: { license: "apache-2.0" },
          siblings: [{ rfilename: "onnx/model.onnx" }],
        }];
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#settings").click();
  await page.locator("#hfFind").click();
  await expect(page.locator("#hfResults option")).toContainText("org/model-a");
  await page.locator("#hfResults").selectOption("org/model-a");
  await page.locator("#hfResults").dispatchEvent("dblclick");
  await expect(page.locator("#modelInput")).toHaveValue("org/model-a");
});

test("/imagegen uses the local desktop inference bridge for a downloaded HF text model", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/model-a",
          localModelId: "org/model-a",
          hfTask: "text-generation",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      runHuggingFaceTask: async (_task, _model, prompt, mediaData) => {
        expect(mediaData).toBe(image.toString("base64"));
        expect(prompt).toBe("Turn this into blue watercolor");
        return { kind: "image", mime: "image/png", data: image.toString("base64") };
      },
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }),
      generate: async (model, task, prompt, id, onChunk) => {
        if (
          model !== "org/model-a" ||
          task !== "text-generation" ||
          prompt !== "say hello"
        )
          throw Error("unexpected local inference inputs");
        onChunk("hello from local model");
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("/imagegen say hello");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText(
    "hello from local model",
  );
  await expect(page.locator("#runtimeStatus")).toContainText(
    "Local model ready",
  );
});

test("local Hugging Face summarization runs without a cloud request", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/summarizer",
          localModelId: "org/summarizer",
          hfTask: "summarization",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      generate: async (model, task, prompt) => {
        if (
          model !== "org/summarizer" ||
          task !== "summarization" ||
          !prompt.includes("local-first")
        )
          throw Error("unexpected local task inputs");
        return { result: "Short local summary" };
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("/summarize local-first desktop ai suite");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("Short local summary");
});

test("attached images reach a local image-to-text model as inline image data", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/vision",
          localModelId: "org/vision",
          hfTask: "image-to-text",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      generate: async (model, task, prompt, id, onChunk, imageData) => {
        if (
          model !== "org/vision" ||
          task !== "image-to-text" ||
          !imageData.startsWith("data:image/png;base64,")
        )
          throw Error("unexpected image inference inputs");
        return { result: "A cat on a sofa" };
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page
    .locator("#file")
    .setInputFiles({
      name: "cat.png",
      mimeType: "image/png",
      buffer: Buffer.from("fake image"),
    });
  await page.locator("#prompt").fill("Describe this");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("A cat on a sofa");
});

test("a running provider request can be stopped from the composer", async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "slow-model",
          theme: "dark",
        },
      }),
    ),
  );
  await page.addInitScript(() => { window.desktopModels = { getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }), streamProviderChat: async (_request, onChunk) => new Promise((resolve) => { window.streamStarted = true; window.finishSlowRequest = () => { onChunk("Done"); resolve(); }; }), cancelProviderChat: () => { window.cancelWasCalled = true; } }; });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("wait forever");
  await page.locator("#send").click();
  await expect(page.locator("#send")).toHaveText("■");
  await expect.poll(() => page.evaluate(() => window.streamStarted)).toBe(true);
  await page.locator("#send").click();
  await expect.poll(() => page.evaluate(() => window.cancelWasCalled)).toBe(true);
  await expect(page.locator("#send")).toHaveText("■");
  await page.evaluate(() => window.finishSlowRequest?.());
  await expect(page.locator(".assistant")).toContainText("Done");
  await expect(page.locator("#send")).toHaveText("↑");
});

test("top-left mode switch opens coding agent setup and creates a sourced project", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "test",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = { pickFolder: async () => "E:\\Projects\\Demo" };
  });
  await page.goto(
    "file://" + path.resolve("index.html").replaceAll("\\\\", "/"),
  );
  await expect(page.locator("#agentModeToggle")).toContainText("ChatBot");
  await page.locator("#agentModeToggle").click();
  await expect(page.locator("#agentModeMenu")).toBeVisible();
  await page.locator('[data-agent-mode="code"]').click();
  await expect(page.locator("#agentModeToggle")).toContainText("Coding Agent");
  await expect(page.locator("#projectDialog")).toBeVisible();
  await page.locator("#projectName").fill("Demo project");
  await page.locator("#projectInstructions").fill("Prefer concise answers.");
  await page.locator("#addProjectFolder").click();
  await expect(page.locator("#projectSources")).toContainText(
    "E:\\Projects\\Demo",
  );
  await page.getByRole("button", { name: "Create project" }).click();
  await expect(page.locator("#projectList")).toContainText("Demo project");
  await expect(page.locator("#crumb")).toHaveText("Demo project");
  await expect(page.locator("#projectContextBadge")).toBeVisible();
  await page.locator("#projectList .project-item").dblclick();
  await expect(page.locator("#projectInstructions")).toHaveValue("Prefer concise answers.");
});

test("model picker scans local hosts, displays measured performance, and switches provider/model", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://localhost:11434/v1/chat/completions",
          model: "llama3.2",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      scanHosts: async () => ({
        hosts: ["Ollama", "LM Studio"],
        models: [
          {
            provider: "Ollama",
            model: "hf.co/example/Qwen-abliterated:Q4_K_M",
            endpoint: "http://127.0.0.1:11434/v1/chat/completions",
            host: "http://127.0.0.1:11434",
            local: true,
            size: 1200000000,
          },
          {
            provider: "LM Studio",
            model: "qwen-local",
            endpoint: "http://127.0.0.1:1234/v1/chat/completions",
            host: "http://127.0.0.1:1234",
            local: true,
          },
        ],
      }),
    };
  });
  await page.goto(
    "file://" + path.resolve("index.html").replaceAll("\\\\", "/"),
  );
  await page.locator("#model").click();
  await expect(page.locator("#modelPicker")).toBeVisible();
  await expect(page.locator("#modelHostStatus")).toContainText("Ollama");
  await expect(page.locator("#modelOptions")).toContainText("Qwen-abliterated");
  await page
    .locator(".model-option")
    .filter({ hasText: "Qwen-abliterated" })
    .click();
  await expect(page.locator("#model")).toContainText("Qwen-abliterated");
  await expect(page.locator("#providerLabel")).toHaveText("Ollama");
});
test("ChatGPT connection is bridged through desktop credentials and status stays in settings", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://localhost:11434/v1/chat/completions",
          model: "llama3.2",
          theme: "dark",
        },
      }),
    );
    let connected = false;
    window.desktopModels = {
      chatgptStatus: async () =>
        connected
          ? { connected: true, sharing: true, email: "local@example.test" }
          : { connected: false, sharing: false },
      chatgptSignIn: async () => {
        connected = true;
        return { connected: true, sharing: true, email: "local@example.test" };
      },
    };
  });
  await page.goto(
    "file://" + path.resolve("index.html").replaceAll("\\\\", "/"),
  );
  await page.locator("#settings").click();
  await expect(page.locator("#chatgptConnectionStatus")).toContainText(
    "Connect an eligible account",
  );
  await page.locator("#chatgptConnect").click();
  await expect(page.locator("#chatgptConnectionStatus")).toContainText(
    "local@example.test",
  );
  await expect(page.locator("#chatgptConnect")).toHaveText("Reconnect ChatGPT");
});
test("ChatGPT plan model streams through the protected desktop bridge", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [],
        settings: {
          provider: "ChatGPT plan",
          endpoint: "https://api.openai.com/v1/responses",
          model: "gpt-test",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      chatgptStatus: async () => ({
        connected: true,
        sharing: true,
        email: "local@example.test",
      }),
      chatgptStream: async (messages, model, id, onChunk) => {
        if (
          model !== "gpt-test" ||
          messages.at(-1).content !== "make a snake game"
        )
          throw Error("unexpected request");
        onChunk("Created a game plan");
      },
    };
  });
  await page.goto(
    "file://" + path.resolve("index.html").replaceAll("\\\\", "/"),
  );
  await page.locator("#prompt").fill("make a snake game");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("Created a game plan");
});
test("Codex-style rail navigates Chat, Images, Activity, and model library", async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [
          {
            id: "saved",
            title: "A saved conversation",
            messages: [{ role: "user", content: "hi" }],
          },
        ],
        projects: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://localhost:11434/v1/chat/completions",
          model: "tiny",
          theme: "dark",
        },
      }),
    ),
  );
  await page.goto(
    "file://" + path.resolve("index.html").replaceAll("\\\\", "/"),
  );
  await page.locator("#navImages").click();
  await expect(page.locator("#imagePage")).toBeVisible();
  await expect(page.locator("#crumb")).toHaveText("Images");
  await page.locator("#navActivity").click();
  await expect(page.locator("#activityPage")).toBeVisible();
  await expect(page.locator("#activityList")).toContainText(
    "A saved conversation",
  );
  await page.locator("#navLibrary").click();
  await expect(page.locator("#modelPicker")).toBeVisible();
  await page.locator("#navChats").click();
  await expect(page.locator("#imagePage")).toBeHidden();
  await expect(page.locator("#crumb")).toHaveText("New chat");
});
test("Images page prompt template enters existing image generation workflow", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "tiny",
          comfyEndpoint: "http://127.0.0.1:8188",
          imageWorkflow: JSON.stringify({
            1: { inputs: { text: "{{prompt}}" }, class_type: "Test" },
          }),
          theme: "dark",
        },
      }),
    );
  });
  await page.route("http://127.0.0.1:8188/prompt", async (route) => {
    const req = route.request().postDataJSON();
    expect(req.prompt["1"].inputs.text).toContain(
      "cinematic mountain landscape",
    );
    await route.fulfill({ json: { prompt_id: "image-page-test" } });
  });
  await page.route("http://127.0.0.1:8188/history/image-page-test", (route) =>
    route.fulfill({ json: { "image-page-test": { outputs: {} } } }),
  );
  await page.goto(
    "file://" +
      path.resolve("index.html").replaceAll(String.fromCharCode(92), "/"),
  );
  await page.locator("#navImages").click();
  await page.locator(".template-card").first().click();
  await expect(page.locator("#imagePrompt")).toHaveValue(/mountain landscape/);
  await page.locator("#createImage").click();
  await expect(page.locator(".assistant")).toContainText(
    "Image generation complete",
  );
});
test("Coding Agent reads project files and saves generated files into its selected folder", async ({
  page,
}) => {
  await page.addInitScript(() => {
    const folders = ["C:/workspace"];
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [{ id: "p1", name: "Demo", folders }],
        activeProjectId: "p1",
        agentMode: "code",
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "coder",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }),
      readProjectFiles: async () => ({
        files: [{ path: "README.md", content: "Existing project context" }],
      }),
      writeProjectFile: async (folders, relativePath, content) => {
        window.__written = { folders, relativePath, content };
        return { relativePath };
      },
    };
  });
  await page.addInitScript(() => {
    window.desktopModels.streamProviderChat = async (request, onChunk) => {
      window.codingRequest = request;
      onChunk('```file:snake.py\nprint("ready")\n```');
    };
  });
  await page.goto(
    "file://" +
      path.resolve("index.html").replaceAll(String.fromCharCode(92), "/"),
  );
  await page.locator("#prompt").fill("create snake.py");
  await page.locator("#send").click();
  const request = await page.evaluate(() => window.codingRequest);
  expect(request.messages[0].content).toContain("Existing project context");
  expect(request.messages[0].content).toContain("file:relative/path.ext");
  await expect(page.locator(".assistant")).toContainText(
    "1 file(s) saved in Demo",
  );
  expect(await page.evaluate(() => window.__written)).toMatchObject({
    relativePath: "snake.py",
  });
  expect((await page.evaluate(() => window.__written.content)).trim()).toBe(
    'print("ready")',
  );
  await expect(page.locator(".assistant")).toContainText('print("ready")');
});
test("ChatBot answers with relevant excerpts from selected local project knowledge", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        projects: [{ id: "docs", name: "Service notes", folders: ["C:/notes"] }],
        activeProjectId: "docs",
        agentMode: "chat",
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "local-chat",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      readProjectFiles: async () => ({
        files: [
          {
            path: "README.md",
            content: "To start the server, run npm run serve from the project root.",
          },
        ],
      }),
    };
  });
  await page.addInitScript(() => { window.desktopModels.getProviderSecretStatus = async () => ({ saved: false, secureStorage: false }); window.desktopModels.streamProviderChat = async (request, onChunk) => { window.knowledgeRequest = request; onChunk("Run npm run serve."); }; });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("How do I start the server?");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("Run npm run serve.");
  const payload = await page.evaluate(() => window.knowledgeRequest);
  expect(payload.messages[0].content).toContain("npm run serve");
  expect(payload.messages[0].content).toContain("README.md");
  expect(payload.messages.at(-1).content).toBe("How do I start the server?");
});
test("workspace writes only authorized relative files and reads project text", async () => {
  const dir = await fs.mkdtemp(
    path.join(os.tmpdir(), "server-rock-workspace-"),
  );
  const profile = path.join(dir, "profile"),
    project = path.join(dir, "project");
  await fs.mkdir(project, { recursive: true });
  const workspace = createWorkspace(profile);
  try {
    await workspace.approveFolder(project);
    await fs.writeFile(
      path.join(project, "README.md"),
      "Local project data",
      "utf8",
    );
    const listing = await workspace.readFiles([project]);
    expect(listing.files).toEqual([
      { path: "README.md", content: "Local project data" },
    ]);
    const result = await workspace.writeFile(
      [project],
      "src/snake.py",
      'print("snake")',
    );
    expect(result.relativePath).toBe("src/snake.py");
    await expect(
      workspace.writeFile([project], "../escape.py", "bad"),
    ).rejects.toThrow("inside the selected project folder");
    await expect(
      fs.readFile(path.join(dir, "escape.py"), "utf8"),
    ).rejects.toThrow();
  } finally {
    await fs.rm(dir, { recursive: true, force: true });
  }
});
test("conversation renders sanitized Markdown and copyable offline code blocks", async ({
  page,
}) => {
  await page.addInitScript(() => { window.desktopModels = { streamProviderChat: async (_request, onChunk) => onChunk('### Build notes\n\n```python\nprint("hello")\n```\n\n<img src=x onerror=alert(1)>'), getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }), cancelProviderChat: () => {} }; });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("show code");
  await page.locator("#send").click();
  const answer = page.locator(".assistant").last();
  await expect(answer.locator("h3")).toHaveText("Build notes");
  await expect(answer.locator("pre code")).toContainText('print("hello")');
  await expect(answer.locator(".copy-code")).toHaveText("Copy");
  await expect(answer.locator("img")).toHaveCount(1);
  expect(await answer.locator("img").getAttribute("onerror")).toBeNull();
});
test("a typed follow-up queues during generation and sends automatically when ready", async ({
  page,
}) => {
  const requests = [];
  let finishFirst;
  await page.addInitScript(() => {
    window.desktopModels = { streamProviderChat: (request, onChunk) => new Promise((resolve) => {
      window.providerRequests.push(request);
      if (window.providerRequests.length === 1) window.finishFirstProviderRequest = () => { onChunk("First reply"); resolve(); };
      else { onChunk("Second reply"); resolve(); }
    }), getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }), cancelProviderChat: () => {} };
    window.providerRequests = [];
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("first question");
  await page.locator("#send").click();
  await expect.poll(() => page.evaluate(() => window.providerRequests.length)).toBe(1);
  await page.locator("#prompt").fill("second question");
  await page.locator("#prompt").press("Enter");
  await expect(page.locator("#queueHint")).toBeVisible();
  await expect(page.locator("#queueHint")).toHaveText("1 queued");
  await page.evaluate(() => window.finishFirstProviderRequest());
  await expect(page.locator(".assistant")).toHaveCount(2);
  await expect(page.locator(".assistant").last()).toContainText("Second reply");
  const providerRequests = await page.evaluate(() => window.providerRequests);
  expect(providerRequests[1].messages.at(-1).content).toBe("second question");
  await expect(page.locator("#queueHint")).toBeHidden();
});
test("conversation response alternatives can be selected and forked without losing the first answer", async ({ page }) => {
  let reply = "First answer";
  await page.addInitScript(() => {
    localStorage.setItem("local-studio-v1", JSON.stringify({ chats: [], projects: [], settings: { provider: "Ollama", endpoint: "http://localhost:11434/v1/chat/completions", model: "test", theme: "dark" } }));
    window.desktopModels = { streamProviderChat: async (_request, onChunk) => { onChunk(window.replyForTest); }, cancelProviderChat: () => {} };
    window.replyForTest = "First answer";
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("Compare options");
  await page.locator("#send").click();
  await expect(page.locator(".assistant").last()).toContainText("First answer");
  await page.locator(".assistant .regenerate").last().click();
  await expect(page.locator(".assistant").last()).toContainText("First answer");
  await page.evaluate(() => { window.replyForTest = "Alternative answer"; });
  await page.locator(".assistant .regenerate").last().click();
  await expect(page.locator(".assistant").last()).toContainText("Alternative answer");
  await expect(page.locator("#branchBar")).toBeVisible();
  await page.locator("#branchBar button[title='Previous response']").click();
  await expect(page.locator(".assistant").last()).toContainText("First answer");
  await page.locator("#branchBar").getByRole("button", { name: "Fork from here" }).click();
  await expect(page.locator("#crumb")).toContainText("(fork)");
});
test("agent mode menu dismisses when clicking outside", async ({ page }) => {
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#agentModeToggle").click();
  await expect(page.locator("#agentModeMenu")).toBeVisible();
  await page.locator("main").click({ position: { x: 500, y: 180 } });
  await expect(page.locator("#agentModeMenu")).toBeHidden();
});
test("Hugging Face image-to-image sends the attached source image and renders the result", async ({
  page,
}) => {
  const image = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  await page.addInitScript(() =>
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/image-editor",
          hfTask: "image-to-image",
          apiKey: "hf_test",
          theme: "dark",
        },
      }),
    ),
  );
  await page.addInitScript(() => {
    window.desktopModels = {
      runHuggingFaceTask: async () => ({ kind: "image", mime: "image/png", data: "iVBORw0KGgo=" }),
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page
    .locator("#file")
    .setInputFiles({
      name: "source.png",
      mimeType: "image/png",
      buffer: image,
    });
  await page
    .locator("#prompt")
    .fill("/imagegen Turn this into blue watercolor");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText(
    "Image transformation complete",
  );
  await expect(page.locator(".assistant img.generated-image")).toHaveAttribute(
    "src",
    new RegExp("^data:image/png;base64,"),
  );
});
test("local Hugging Face image-to-image output remains visible after the turn is saved", async ({
  page,
}) => {
  const image = "data:image/png;base64,iVBORw0KGgo=";
  await page.addInitScript((image) => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/local-editor",
          localModelId: "org/local-editor",
          hfTask: "image-to-image",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: false }),
      runHuggingFaceTask: async (task, model, prompt, mediaData) => {
        if (model !== "org/local-editor" || task !== "image-to-image" || !mediaData.startsWith("data:image/png;base64,")) throw Error("unexpected local image inputs");
        return { kind: "image", mime: "image/png", data: image.split(",")[1] };
      },
      generate: async () => ({ imageData: image }),
    };
  }, image);
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page
    .locator("#file")
    .setInputFiles({
      name: "source.png",
      mimeType: "image/png",
      buffer: Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    });
  await page.locator("#prompt").fill("Turn this into an oil painting");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText(
    "Image transformation complete",
  );
  await expect(page.locator(".assistant img.generated-image")).toHaveAttribute(
    "src",
    image,
  );
});
test("the model task picker groups broad Hugging Face text, image, and audio tasks", async ({
  page,
}) => {
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#settings").click();
  await expect(page.locator("#taskSelect option")).toHaveCount(26);
  await expect(
    page.locator('#taskSelect option[value="zero-shot-audio-classification"]'),
  ).toHaveText("Zero-shot audio classification");
  await expect(page.locator("#file")).toHaveAttribute("accept", /audio/);
});
test("local Hugging Face question answering accepts structured JSON input", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/qa",
          localModelId: "org/qa",
          hfTask: "question-answering",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      generate: async (model, task, prompt) => {
        if (
          model !== "org/qa" ||
          task !== "question-answering" ||
          JSON.parse(prompt).question !== "What is the answer?"
        )
          throw Error("unexpected structured local task");
        return { result: "42" };
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page
    .locator("#prompt")
    .fill('{"question":"What is the answer?","context":"The answer is 42."}');
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("42");
});
test("local Hugging Face speech recognition receives audio attachments", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/asr",
          localModelId: "org/asr",
          hfTask: "automatic-speech-recognition",
          theme: "dark",
        },
      }),
    );
    window.desktopModels = {
      generate: async (model, task, prompt, id, onChunk, mediaData) => {
        if (
          model !== "org/asr" ||
          task !== "automatic-speech-recognition" ||
          !mediaData.startsWith("data:audio/wav;base64,")
        )
          throw Error("unexpected local audio task");
        return { result: "hello world" };
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page
    .locator("#file")
    .setInputFiles({
      name: "speech.wav",
      mimeType: "audio/wav",
      buffer: Buffer.from([82, 73, 70, 70, 0, 0, 0, 0, 87, 65, 86, 69]),
    });
  await page.locator("#prompt").fill("Transcribe this clip");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("hello world");
});
test("hosted Hugging Face classification uses the task inference endpoint", async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Hugging Face",
          endpoint: "https://router.huggingface.co/v1/chat/completions",
          model: "org/sentiment",
          hfTask: "text-classification",
          apiKey: "hf_test",
          theme: "dark",
        },
      }),
    ),
  );
  await page.addInitScript(() => {
    window.desktopModels = {
      runHuggingFaceTask: async (task, model, prompt) => {
        if (task !== "text-classification" || model !== "org/sentiment" || prompt !== "I love this app") throw Error("unexpected classification inputs");
        return { result: "positive" };
      },
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#prompt").fill("I love this app");
  await page.locator("#send").click();
  await expect(page.locator(".assistant")).toContainText("positive");
});
test("saving Hugging Face settings applies provider and model in the home view", async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "llama3.2",
          theme: "dark",
        },
        settingsByProvider: {
          "Hugging Face": {
            endpoint: "https://router.huggingface.co/v1/chat/completions",
            model: "Xenova/ms-marco-TinyBERT-L-2-v2",
            localModelId: "Xenova/ms-marco-TinyBERT-L-2-v2",
            hfTask: "text-classification",
          },
        },
      }),
    ),
  );
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#provider").click();
  await page.locator("#providerSelect").selectOption("Hugging Face");
  await page.locator("#modelInput").fill("Xenova/ms-marco-TinyBERT-L-2-v2");
  await page.locator("#localModelId").fill("Xenova/ms-marco-TinyBERT-L-2-v2");
  await page.locator("#taskSelect").selectOption("text-classification");
  await page.locator("#saveSettings").click();
  await expect(page.locator("#settingsDialog")).not.toBeVisible();
  await expect(page.locator("#providerLabel")).toHaveText("Hugging Face");
  await expect(page.locator("#model")).toContainText(
    "Xenova/ms-marco-TinyBERT-L-2-v2",
  );
});
test("model picker offers configured local Hugging Face models alongside scanned hosts", async ({
  page,
}) => {
  await page.addInitScript(() => {
    localStorage.setItem(
      "local-studio-v1",
      JSON.stringify({
        chats: [],
        settings: {
          provider: "Ollama",
          endpoint: "http://127.0.0.1:11434/v1/chat/completions",
          model: "llama3.2",
          theme: "dark",
        },
        settingsByProvider: {
          "Hugging Face": {
            endpoint: "https://router.huggingface.co/v1/chat/completions",
            model: "Xenova/ms-marco-TinyBERT-L-2-v2",
            localModelId: "Xenova/ms-marco-TinyBERT-L-2-v2",
            hfTask: "text-classification",
          },
        },
      }),
    );
    window.desktopModels = {
      scanHosts: async () => ({
        hosts: ["Ollama"],
        models: [
          {
            provider: "Ollama",
            model: "llama3.2",
            endpoint: "http://127.0.0.1:11434/v1/chat/completions",
            local: true,
          },
        ],
      }),
    };
  });
  await page.goto("file://" + path.resolve("index.html").replaceAll("\\", "/"));
  await page.locator("#model").click();
  await expect(page.locator("#modelOptions")).toContainText(
    "Xenova/ms-marco-TinyBERT-L-2-v2",
  );
  await expect(page.locator("#modelOptions")).toContainText("On this device");
});
