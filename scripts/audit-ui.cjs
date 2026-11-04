const path = require("node:path");
const { chromium } = require("playwright");

const appUrl = "file://" + path.resolve(__dirname, "..", "index.html").replaceAll("\\", "/");
const viewports = [
  { name: "wide", width: 1920, height: 1080 },
  { name: "desktop", width: 1440, height: 900 },
  { name: "tablet", width: 800, height: 700 },
  { name: "mobile", width: 390, height: 844 },
  { name: "small-mobile", width: 320, height: 568 },
];

async function auditViewport(page, viewport) {
  await page.setViewportSize({ width: viewport.width, height: viewport.height });
  await page.goto(appUrl);
  await page.evaluate(() => localStorage.removeItem("local-studio-v1"));
  await page.reload();
  await page.locator("#prompt").waitFor({ state: "visible" });
  const structure = await page.evaluate(() => {
    const named = (element) => Boolean(
      (element.getAttribute("aria-label") || element.getAttribute("title") || element.textContent || "").trim(),
    );
    const labels = new Map([...document.querySelectorAll("label[for]")].map((label) => [label.htmlFor, label.textContent.trim()]));
    const duplicateIds = [...document.querySelectorAll("[id]")].map((element) => element.id).filter((id, index, ids) => ids.indexOf(id) !== index);
    const unlabeledControls = [...document.querySelectorAll("input,textarea,select")]
      .filter((element) => !(element.hidden || element.closest("[hidden]")))
      .filter((element) => !element.getAttribute("aria-label") && !element.closest("label") && !labels.has(element.id))
      .map((element) => element.id || element.tagName);
    const unnamedButtons = [...document.querySelectorAll("button")]
      .filter((element) => !(element.hidden || element.closest("[hidden]")))
      .filter((element) => !named(element))
      .map((element) => element.id || element.outerHTML.slice(0, 80));
    return {
      horizontalOverflow: document.documentElement.scrollWidth > innerWidth,
      duplicateIds: [...new Set(duplicateIds)],
      unlabeledControls,
      unnamedButtons,
    };
  });
  const failures = [];
  if (structure.horizontalOverflow) failures.push("horizontal overflow");
  if (structure.duplicateIds.length) failures.push(`duplicate IDs: ${structure.duplicateIds.join(", ")}`);
  if (structure.unlabeledControls.length) failures.push(`unlabeled controls: ${structure.unlabeledControls.join(", ")}`);
  if (structure.unnamedButtons.length) failures.push(`unnamed buttons: ${structure.unnamedButtons.join(", ")}`);

  if (viewport.width <= 700) {
    await page.locator("#mobileMenu").click();
    await page.locator('.mobile-nav [data-page="activity"]').click();
    if (await page.locator("#activityPage").isHidden()) failures.push("mobile drawer could not open Activity");
    if (await page.locator(".sidebar").evaluate((element) => element.classList.contains("mobile-open"))) failures.push("mobile drawer stayed open after navigation");
    await page.locator("#settings").click();
    const modal = await page.locator("#settingsDialog").evaluate((dialog) => {
      const content = dialog.querySelector(".settings-content");
      const target = content || dialog;
      return {
        open: dialog.open,
        scrollable: target.scrollHeight > target.clientHeight && ["auto", "scroll"].includes(getComputedStyle(target).overflowY),
      };
    });
    if (!modal.open || !modal.scrollable) failures.push("settings dialog is not scrollable on mobile");
    await page.locator("#settingsDialog .close").click();
  } else {
    await page.locator("#navActivity").click();
  }
  if (await page.locator("#activityPage").isHidden()) failures.push("Activity page did not open");
  if (!(await page.locator(".terminal-panel").isVisible())) failures.push("terminal panel is not visible on Activity");
  return { viewport: viewport.name, failures, structure };
}

(async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    const results = [];
    for (const viewport of viewports) results.push(await auditViewport(page, viewport));
    for (const result of results) {
      console.log(`${result.viewport}: ${result.failures.length ? result.failures.join("; ") : "passed"}`);
    }
    const failures = results.flatMap((result) => result.failures.map((failure) => `${result.viewport}: ${failure}`));
    if (failures.length) throw new Error(`UI audit failed:\n${failures.join("\n")}`);
  } finally {
    await browser.close();
  }
})().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
