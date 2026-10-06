/* Copyright (C) 2026 stereolove33 | SPDX-License-Identifier: GPL-3.0-or-later */
import { invoke, listen, open } from "./bridge.mjs";
import { needsCustomStart, officialLabel, unwrap, stopAllLoaders, toggleLoaders } from "./model.mjs";

import { automaticLabels, preflightIssues, healthLabel } from "./custom-analysis.mjs";

import { createUpdateController } from "./updates.mjs";
import { detectFirstLeaguePath } from "./auto-path.mjs";
import { createStartupTransition } from "./startup.mjs";
import { applyCustomOrder, moveCustomOrder } from "./custom-order.mjs";

const analyses = new Map();
const el = (id) => document.getElementById(id);
const api = async (command, args) => unwrap(await invoke(command, args));
let mods = [];
let busy = false;
let customsEnabled = true;
let settings;
let closingGame = false;
let customOrder = [];
let draggedCustom = null;
try { customOrder = JSON.parse(localStorage.getItem("zephyr.custom-order.v1") || "[]"); } catch {}
const customSection = el("custom-section");
try { customSection.open = localStorage.getItem("zephyr.custom-section.v1") !== "closed"; } catch {}
customSection.addEventListener("toggle", () => {
  try { localStorage.setItem("zephyr.custom-section.v1", customSection.open ? "open" : "closed"); } catch {}
});
let gameCloseMessage = "Closes the match only. Keep Zephyr running and click Reconnect in the League client.";
const startup = createStartupTransition({
  reveal() {
    requestAnimationFrame(() => {
      el("loading-screen").classList.add("is-ready");
      setTimeout(() => document.documentElement.classList.remove("is-loading"), 450);
      document.querySelector("main").inert = false;
    });
  },
});

let language = "en";
try { if (localStorage.getItem("zephyr.language") === "pt-BR") language = "pt-BR"; } catch {}
const translations = {"Official and custom skins": "Skins oficiais e personalizadas", "OFFICIAL SKINS": "SKINS OFICIAIS", "Custom skins": "Customs", "Stopped": "Parado", "Start": "Iniciar", "Start waits for a match and loads the in-game menu.": "Iniciar aguarda uma partida e carrega o menu no jogo.", "+ Add custom skin": "+ Adicionar custom", "Enable custom skins": "Ativar customs", "Select custom skins before entering a match.": "Selecione as customs antes de entrar em uma partida.", "Settings": "Configurações", "Language": "Idioma", "Start with Windows": "Iniciar com o Windows", "Close to system tray": "Fechar para a bandeja", "Use the tray menu to quit the app completely.": "Use o menu da bandeja para encerrar o aplicativo.", "League folder": "Pasta do League", "Installation folder": "Pasta de instalação", "Browse": "Procurar", "Auto-detect": "Detectar automaticamente", "Save path": "Salvar caminho", "Stop loaders": "Parar carregadores", "1.0.1 · Experimental": "1.0.1 · Experimental", "Add a .modpkg or .fantome file.": "Adicione um arquivo .modpkg ou .fantome.", "Remove": "Remover", "Champion": "Campeão", "Type": "Tipo", "Skin": "Skin", "Font": "Fonte", "UI": "Interface", "Map": "Mapa", "Audio": "Áudio", "Other": "Outro", "Champion name (optional for fonts, UI and other mods)": "Nome do campeão (opcional para fontes, interface e outras customs)", "Stop the loaders before changing custom skins.": "Pare os carregadores antes de alterar as customs.", "Select at least one custom skin.": "Selecione pelo menos uma custom.", "Custom skins": "Customs", "Loaders stopped. Exit the match to unload the DLLs.": "Carregadores parados. Saia da partida para descarregar as DLLs.", "Path saved.": "Caminho salvo.", "Preparing": "Preparando", "Active": "Ativo", "Preparing custom skin files...": "Preparando os arquivos das customs...", "Loader active. Check the skin in the game.": "Carregador ativo. Confira a skin no jogo.", "Injection failed": "Falha na injeção", "DLL loaded": "DLL carregada", "Loading": "Carregando", "Waiting for game": "Aguardando o jogo", "League installation not found. Select the folder with Browse.": "Instalação do League não encontrada. Selecione a pasta com Procurar.", "League folder detected. Click Save path to confirm.": "Pasta do League detectada. Clique em Salvar caminho para confirmar.", "Setting saved.": "Configuração salva."};
Object.assign(translations, {"Stop":"Parar", "Mixed": "Misto", "Unknown": "Não identificado", "Check customs": "Verificar customs", "Not checked": "Não verificado", "Errors found": "Erros encontrados", "Warnings found": "Avisos encontrados", "No issues detected": "Nenhum problema detectado", "Check unavailable": "Verificação indisponível", "Labels can be edited; safety checks always use package content.": "Os rótulos podem ser editados; as verificações usam o conteúdo do pacote.", "Checking customs...": "Verificando customs...", "Wait for game hashtable synchronization, then check again.": "Aguarde a sincronização das tabelas do jogo e verifique novamente.", "Checks completed. Unknown labels can be edited manually.": "Verificações concluídas. Rótulos não identificados podem ser editados manualmente.", "Verification incomplete. Customs were not activated.": "Verificação incompleta. As customs não foram ativadas.", "Custom activation blocked.": "Ativação das customs bloqueada.", "Conflicting resources:": "Recursos em conflito:", "Disable the conflicting or broken custom and try again.": "Desative a custom em conflito ou com erro e tente novamente."});
Object.assign(translations, {
  "Stop custom skins": "Parar customs",
  "Custom skins stopped. Exit the match to unload active custom assets.": "Customs paradas. Saia da partida para descarregar os recursos ativos.",
  "Partial check: package readable; full Mod Health unavailable for .modpkg.": "Verificação parcial: pacote legível; análise completa indisponível para .modpkg.",
  "Partial check for .modpkg: package reading and conflicts checked; full Mod Health unavailable.": "Verificação parcial de .modpkg: leitura e conflitos verificados; análise completa indisponível."
});
Object.assign(translations, {"Zephyr loaded.":"Zephyr carregado.","Zephyr found the game. Waiting to load.":"Zephyr encontrou o jogo. Aguardando o carregamento.","Zephyr is waiting for a match.":"Zephyr está aguardando uma partida.","Zephyr stopped.":"Zephyr parado.","Custom skins stopped.":"Customs paradas.","Internal custom skins component error.":"Falha interna no componente de customs.","Select at least one custom skin before starting.":"Selecione pelo menos uma custom antes de iniciar.","Stop custom skins before changing the library or game folder.":"Pare as customs antes de alterar a biblioteca ou a pasta do jogo."});
Object.assign(translations, {"If the in-game menu does not appear, use Close game to reconnect, then click Reconnect in the League client. Keep Zephyr running.":"Se o menu no jogo não aparecer, use Fechar jogo para reconectar e depois clique em Reconectar no cliente do League. Mantenha o Zephyr aberto.","Troubleshooting tip":"Dica para resolver problemas"});
Object.assign(translations, {"Update available":"Atualização disponível","A new version of Zephyr is available:":"Uma nova versão do Zephyr está disponível:","View update":"Ver atualização","Check for updates":"Verificar atualizações","Checking for updates...":"Verificando atualizações...","You are using the latest version.":"Você está usando a versão mais recente.","Could not check for updates. Try again later.":"Não foi possível verificar atualizações. Tente novamente mais tarde."});
Object.assign(translations, {
  "Close game to reconnect": "Fechar jogo para reconectar",
  "Closes the match only. Keep Zephyr running and click Reconnect in the League client.": "Fecha apenas a partida. Mantenha o Zephyr aberto e clique em Reconectar no cliente do League.",
  "Closing game...": "Fechando o jogo...",
  "Game closed. Click Reconnect in the League client. Keep Zephyr running.": "Jogo fechado. Clique em Reconectar no cliente do League. Mantenha o Zephyr aberto.",
  "No running match found.": "Nenhuma partida em execução encontrada.",
  "Could not close the game. Check that Zephyr has permission to close it.": "Não foi possível fechar o jogo. Verifique se o Zephyr tem permissão para encerrá-lo.",
  "Set your League folder in Settings before closing the game.": "Configure a pasta do League em Configurações antes de fechar o jogo."
});
Object.assign(translations, {"Reorder custom skin": "Reorganizar custom", "Drag to reorder. Use Up and Down arrows when focused.": "Arraste para reorganizar. Com o controle selecionado, use as setas para cima e para baixo."});
const tr = (text) => language === "pt-BR" ? (translations[text] || text) : text;
const staticLabels = Array.from(document.querySelectorAll("h1, header p, .heading > span:first-child, button, summary:not(.custom-summary), label:not(.setting-toggle), .setting-toggle span, details .status, footer span")).map(node => [node, node.textContent]);
const updates = createUpdateController({
  check: () => api("check_updates"),
  openRelease: () => api("open_update"),
  translate: (text) => tr(text),
  render(view) {
    el("update-banner").hidden = !view.available;
    el("update-title").textContent = view.title;
    el("update-message").textContent = view.message;
    el("open-update").textContent = view.downloadLabel;
    el("check-updates").textContent = view.checkLabel;
    el("check-updates").disabled = busy || view.checking;
    el("update-status").hidden = !view.message || view.available;
    el("update-status").textContent = view.message;
  },
});
el("check-updates").addEventListener("click", () => updates.check());
el("open-update").addEventListener("click", () => action(() => updates.open()));
function translatePage() {
  document.documentElement.lang = language;
  el("startup-tip-text").textContent = tr("If the in-game menu does not appear, use Close game to reconnect, then click Reconnect in the League client. Keep Zephyr running.");
  el("startup-tip-button").setAttribute("aria-label", tr("Troubleshooting tip"));
  for (const [node, text] of staticLabels) node.textContent = tr(text);
  el("league-path").placeholder = tr("Installation folder");
  el("language").value = language;
  el("official-status").textContent = tr("Start waits for a match and loads the in-game menu.");
  el("custom-status").textContent = tr("Select custom skins before entering a match.");
  el("close-game-status").textContent = tr(gameCloseMessage);
  updates.refresh();
}

function report(error) {
  el("error").hidden = false;
  el("error").textContent = error instanceof Error ? error.message : JSON.stringify(error);
}

async function action(fn) {
  if (busy) return;
  busy = true;
  el("error").hidden = true;
  document.querySelectorAll("button:not(#close-game), input, select").forEach((button) => { button.disabled = true; });
  try { await fn(); } catch (error) { report(error); }
  finally {
    busy = false;
    document.querySelectorAll("button:not(#close-game), input, select").forEach((button) => { button.disabled = false; });
    updates.refresh();
  }
}

const customTypes = ["Skin", "Font", "UI", "Map", "Audio", "Other", "Mixed", "Unknown"];

function customMetadataFields(mod) {
  const key = `zephyr.custom-metadata.v1:${mod.id}`;
  let metadata = {};
  try {
    const saved = JSON.parse(localStorage.getItem(key) || "{}");
    if (saved && typeof saved === "object") metadata = saved;
  } catch (error) { report(new Error("Could not read custom labels: " + error.message)); }
  const automatic = analyses.get(mod.id)?.labels || { champion: "", type: "Unknown" };
  const champion = document.createElement("input");
  champion.className = "mod-champion";
  champion.type = "text";
  champion.placeholder = tr("Champion");
  champion.maxLength = 60;
  champion.value = typeof metadata.champion === "string" && metadata.champion ? metadata.champion : automatic.champion;
  champion.setAttribute("aria-label", `Champion for ${mod.displayName || mod.name}`);
  champion.title = tr("Champion name (optional for fonts, UI and other mods)");
  const type = document.createElement("select");
  type.className = "mod-type";
  type.setAttribute("aria-label", `Type for ${mod.displayName || mod.name}`);
  const unset = new Option(tr("Type"), "");
  type.append(unset);
  for (const category of customTypes) type.append(new Option(tr(category), category));
  type.value = customTypes.includes(metadata.type) && metadata.type !== "Unknown" ? metadata.type : automatic.type;
  function saveMetadata() {
    try {
      localStorage.setItem(key, JSON.stringify({ champion: champion.value.trim(), type: type.value }));
    } catch (error) { report(new Error("Could not save custom labels: " + error.message)); }
  }
  champion.addEventListener("change", saveMetadata);
  type.addEventListener("change", saveMetadata);
  return [champion, type];
}

async function refreshMods() {
  mods = await api("plugin:library|get_installed_mods");
  renderMods();
}

function renderMods() {
  const ordered = applyCustomOrder(mods, customOrder);
  customOrder = ordered.map(mod => mod.id);
  try { localStorage.setItem("zephyr.custom-order.v1", JSON.stringify(customOrder)); } catch {}
  el("mods").replaceChildren();
  if (!mods.length) {
    const empty = document.createElement("p");
    empty.className = "empty";
    empty.textContent = tr("Add a .modpkg or .fantome file.");
    el("mods").append(empty);
  }
  for (const mod of ordered) {
    const row = document.createElement("div");
    row.className = "mod";
    row.dataset.modId = mod.id;
    const grip = createCustomGrip(mod, row);
    const check = document.createElement("input");
    check.type = "checkbox";
    check.checked = mod.enabled;
    check.setAttribute("aria-label", `Enable ${mod.displayName || mod.name}`);
    check.addEventListener("change", () => action(async () => {
      await requireIdleCustom();
      await api("plugin:library|toggle_mod", { modId: mod.id, enabled: check.checked });
      await refreshMods();
    }));
    const name = document.createElement("span");
    name.textContent = mod.displayName || mod.name;
    const remove = document.createElement("button");
    remove.className = "remove";
    remove.textContent = tr("Remove");
    remove.addEventListener("click", () => action(async () => {
      await requireIdleCustom();
      await api("plugin:library|uninstall_mod", { modId: mod.id });
      await refreshMods();
    }));
    row.append(grip, check, name, ...customMetadataFields(mod), remove);
    const summary = document.createElement("small");
    summary.className = "mod-summary";
    const analysis = analyses.get(mod.id);
    summary.textContent = analysis?.partial ? tr("Partial check: package readable; full Mod Health unavailable for .modpkg.") : analysis ? tr(healthLabel(analysis.verdict)) : tr("Not checked");
    if (analysis?.error) summary.textContent = tr("Check unavailable") + ": " + analysis.error;
    summary.title = analysis?.verdict?.rules?.map(rule => `${rule.rule}: ${JSON.stringify(rule.counts)}`).join("\n") || tr("Labels can be edited; safety checks always use package content.");
    row.append(summary);
    el("mods").append(row);
  }
}


function clearCustomDrag() {
  draggedCustom = null;
  el("mods").querySelectorAll(".mod").forEach(row => row.classList.remove("drop-before", "drop-after", "is-dragging"));
}

function reorderCustom(source, target, after) {
  customOrder = moveCustomOrder(customOrder, source, target, after);
  renderMods();
}

function createCustomGrip(mod, row) {
  const grip = document.createElement("button");
  grip.type = "button";
  grip.className = "custom-grip";
  grip.textContent = "⠿";
  grip.setAttribute("aria-label", tr("Reorder custom skin") + ": " + (mod.displayName || mod.name));
  grip.title = tr("Drag to reorder. Use Up and Down arrows when focused.");
  let pointerDrag = null;
  function updateDrop(event) {
    if (!pointerDrag || event.pointerId !== pointerDrag.pointer) return;
    if (!pointerDrag.moved && Math.hypot(event.clientX - pointerDrag.x, event.clientY - pointerDrag.y) < 4) return;
    pointerDrag.moved = true;
    draggedCustom = mod.id;
    el("mods").querySelectorAll(".mod").forEach(item => item.classList.remove("drop-before", "drop-after"));
    row.classList.add("is-dragging");
    grip.setAttribute("aria-grabbed", "true");
    const target = document.elementFromPoint(event.clientX, event.clientY)?.closest(".mod");
    pointerDrag.target = null;
    if (target && target.parentElement === el("mods") && target !== row) {
      const bounds = target.getBoundingClientRect();
      const after = event.clientY > bounds.top + bounds.height / 2;
      target.classList.add(after ? "drop-after" : "drop-before");
      pointerDrag.target = { id: target.dataset.modId, after };
    }
    const bounds = el("mods").getBoundingClientRect();
    if (event.clientY < bounds.top + 28) el("mods").scrollTop -= 18;
    else if (event.clientY > bounds.bottom - 28) el("mods").scrollTop += 18;
  }
  function finishDrag(event, cancelled = false) {
    if (!pointerDrag || event.pointerId !== pointerDrag.pointer) return;
    if (!cancelled) updateDrop(event);
    const target = !cancelled && pointerDrag.target;
    pointerDrag = null;
    if (grip.hasPointerCapture(event.pointerId)) grip.releasePointerCapture(event.pointerId);
    grip.removeAttribute("aria-grabbed");
    clearCustomDrag();
    if (target) reorderCustom(mod.id, target.id, target.after);
  }
  grip.addEventListener("pointerdown", event => {
    if (event.button !== 0 || draggedCustom) return;
    event.preventDefault();
    grip.focus();
    pointerDrag = { pointer: event.pointerId, x: event.clientX, y: event.clientY, moved: false, target: null };
    grip.setPointerCapture(event.pointerId);
  });
  grip.addEventListener("pointermove", updateDrop);
  grip.addEventListener("pointerup", event => finishDrag(event));
  grip.addEventListener("pointercancel", event => finishDrag(event, true));
  grip.addEventListener("keydown", event => {
    if (event.key !== "ArrowUp" && event.key !== "ArrowDown") return;
    event.preventDefault();
    const index = customOrder.indexOf(mod.id);
    const target = customOrder[index + (event.key === "ArrowUp" ? -1 : 1)];
    if (!target) return;
    reorderCustom(mod.id, target, event.key === "ArrowDown");
    const moved = Array.from(el("mods").children).find(item => item.dataset.modId === mod.id);
    moved?.querySelector(".custom-grip")?.focus();
  });
  return grip;
}

function modName(id) {
  const mod = mods.find(item => item.id === id);
  return mod?.displayName || mod?.name || id;
}

async function analyzeCustoms(ids = mods.map(mod => mod.id)) {
  if (!ids.length) return;
  const readiness = await api("plugin:library|get_health_check_readiness");
  for (const id of ids) {
    el("custom-status").textContent = tr("Checking customs...") + " " + modName(id);
    const previous = analyses.get(id) || {};
    let footprint;
    let inspection;
    const errors = [];
    try { inspection = await api("plugin:library|inspect_custom_mod", { modId: id }); }
    catch (error) { errors.push(error.message); }
    try { footprint = await api("plugin:library|analyze_mod_wads", { modId: id }); }
    catch (error) { errors.push(error.message); }
    let verdict;
    const partial = mods.find(mod => mod.id === id)?.format === "modpkg";
    if (!partial && readiness === "ready") {
      try { verdict = await api("plugin:library|check_mod_health", { modId: id }); }
      catch (error) { errors.push(error.message); }
    } else if (!partial) {
      errors.push(tr("Wait for game hashtable synchronization, then check again."));
    }
    analyses.set(id, { ...previous, labels: automaticLabels(footprint, inspection), verdict, partial: partial && !!inspection, error: errors.join("; ") });
  }
  await refreshMods();
  el("custom-status").textContent = tr("Checks completed. Unknown labels can be edited manually.");
}

async function verifySelectedCustoms() {
  await refreshMods();
  const selected = mods.filter(mod => mod.enabled).map(mod => mod.id);
  el("custom-status").textContent = tr("Checking customs...");
  const result = await api("plugin:library|check_custom_selection", { modIds: selected });
  const covered = [...result.verdicts.map(verdict => verdict.modId), ...(result.partialModIds || [])];
  if (covered.length !== selected.length || selected.some(id => !covered.includes(id))) {
    throw new Error(tr("Verification incomplete. Customs were not activated."));
  }
  for (const verdict of result.verdicts) analyses.set(verdict.modId, { ...analyses.get(verdict.modId), verdict, error: "" });
  for (const id of result.partialModIds || []) analyses.set(id, { ...analyses.get(id), partial: true, error: "" });
  await refreshMods();
  const issues = preflightIssues(result);
  if (issues.blocked) {
    const lines = [tr("Custom activation blocked.")];
    const pairs = new Set();
    for (const conflict of issues.conflicts) {
      const pair = `${modName(conflict.firstMod)} + ${modName(conflict.secondMod)}`;
      if (!pairs.has(pair)) { lines.push(tr("Conflicting resources:") + " " + pair); pairs.add(pair); }
    }
    for (const verdict of issues.broken) lines.push(tr("Errors found") + ": " + modName(verdict.modId));
    lines.push(tr("Disable the conflicting or broken custom and try again."));
    throw new Error(lines.join("\n"));
  }
  const notice = el("check-notice");
  notice.hidden = !issues.warnings.length && !result.partialModIds?.length;
  notice.textContent = issues.warnings.length ? tr("Warnings found") + ": " + issues.warnings.map(verdict => modName(verdict.modId)).join(", ") : "";
  if (result.partialModIds?.length) notice.textContent += (notice.textContent ? "\n" : "") + tr("Partial check for .modpkg: package reading and conflicts checked; full Mod Health unavailable.");
}

el("check-customs").addEventListener("click", () => action(async () => {
  await requireIdleCustom();
  await analyzeCustoms();
  if (mods.some(mod => mod.enabled)) await verifySelectedCustoms();
}));

async function requireIdleCustom() {
  const status = await api("get_patcher_status");
  if (status.running) throw new Error(tr("Stop the loaders before changing custom skins."));
}

async function startCustom() {
  const status = await api("get_patcher_status");
  if (!mods.some((mod) => mod.enabled)) throw new Error(tr("Select at least one custom skin."));
  if (!status.running) {
    await verifySelectedCustoms();
    await api("start_patcher", { config: {} });
  }
}

async function stopLoaders() {
  await stopAllLoaders(api);
  el("start").textContent = tr("Start");
  el("custom-status").textContent = tr("Loaders stopped. Exit the match to unload the DLLs.");
}

el("start").addEventListener("click", () => action(async () => {
  const [officialStatus, customStatus] = await Promise.all([
    api("plugin:injector|official_status"), api("get_patcher_status"),
  ]);
  await toggleLoaders(officialStatus, customStatus, async () => {
    if (customsEnabled && needsCustomStart(mods, customStatus)) await startCustom();
    const official = await api("plugin:injector|start_official");
    el("official-status").textContent = tr(official.message.replace(/^[A-Z]+\|/, ""));
    el("start").textContent = tr("Stop");
  }, stopLoaders);
}));
el("close-game").addEventListener("click", async () => {
  if (closingGame) return;
  closingGame = true;
  el("close-game").disabled = true;
  gameCloseMessage = "Closing game...";
  el("close-game-status").textContent = tr(gameCloseMessage);
  try {
    const result = await api("close_league_game");
    gameCloseMessage = result.needsPath ? "Set your League folder in Settings before closing the game."
      : result.failed ? "Could not close the game. Check that Zephyr has permission to close it."
      : result.closed ? "Game closed. Click Reconnect in the League client. Keep Zephyr running."
      : "No running match found.";
  } catch (error) {
    gameCloseMessage = "Could not close the game. Check that Zephyr has permission to close it.";
    report(error);
  } finally {
    closingGame = false;
    el("close-game").disabled = false;
    el("close-game-status").textContent = tr(gameCloseMessage);
  }
});
el("add").addEventListener("click", () => action(async () => {
  await requireIdleCustom();
  const selected = await open({ multiple: true, filters: [{ name: tr("Custom skins"), extensions: ["modpkg", "fantome"] }] });
  if (!selected) return;
  const paths = Array.isArray(selected) ? selected : [selected];
  for (const filePath of paths) {
    const mod = await api("plugin:library|install_mod", { filePath });
    await api("plugin:library|toggle_mod", { modId: mod.id, enabled: true });
  }
  await refreshMods();
  await analyzeCustoms(paths.length ? mods.map(mod => mod.id) : []);
}));
el("apply").addEventListener("click", () => action(async () => {
  const status = await api("get_patcher_status");
  if (status.running) {
    await api("stop_patcher");
    customsEnabled = false;
    el("apply").textContent = tr("Enable custom skins");
    el("custom-status").textContent = tr("Custom skins stopped. Exit the match to unload active custom assets.");
  } else {
    await startCustom();
    customsEnabled = true;
    el("apply").textContent = tr("Stop custom skins");
  }
}));
el("browse").addEventListener("click", () => action(async () => {
  const path = await open({ directory: true, multiple: false });
  if (typeof path === "string") el("league-path").value = path;
}));
el("save-path").addEventListener("click", () => action(async () => {
  await requireIdleCustom();
  settings = await api("get_settings");
  settings.leaguePath = el("league-path").value.trim() || null;
  await api("save_settings", { settings });
  el("custom-status").textContent = tr("Path saved.");
}));


el("language").addEventListener("change", () => action(async () => {
  const selected = el("language").value;
  localStorage.setItem("zephyr.language", selected);
  language = selected;
  translatePage();
  await refreshMods();
}));
for (const [id, key] of [["auto-run", "autoRun"], ["close-to-tray", "minimizeToTray"]]) {
  el(id).addEventListener("change", () => action(async () => {
    const current = await api("get_settings");
    const previous = current[key];
    current[key] = el(id).checked;
    try { await api("save_settings", { settings: current }); }
    catch (error) { el(id).checked = previous; throw error; }
    settings = current;
    el("custom-status").textContent = tr("Setting saved.");
  }));
}
el("detect-path").addEventListener("click", () => action(async () => {
  const detected = await api("auto_detect_league_path");
  if (!detected) throw new Error(tr("League installation not found. Select the folder with Browse."));
  el("league-path").value = detected;
  el("custom-status").textContent = tr("League folder detected. Click Save path to confirm.");
}));

async function poll() {
  try {
    const [official, custom] = await Promise.all([
      api("plugin:injector|official_status"), api("get_patcher_status"),
    ]);
    el("apply").textContent = tr(custom.running ? "Stop custom skins" : "Enable custom skins");
    el("start").textContent = tr(official.running ? "Stop" : "Start");
    el("official-badge").textContent = tr(officialLabel(official.message, official.running));
    if (official.message) el("official-status").textContent = tr(official.message.replace(/^[A-Z]+\|/, ""));
    el("custom-badge").textContent = custom.running ? (custom.phase === "building" ? tr("Preparing") : tr("Active")) : tr("Stopped");
    if (custom.running) el("custom-status").textContent = custom.phase === "building"
      ? tr("Preparing custom skin files...")
      : tr("Loader active. Check the skin in the game.");
  } catch (error) { report(error); }
  finally { setTimeout(poll, 1000); }
}

translatePage();
updates.check();
setInterval(() => { if (!document.hidden) updates.check(); }, 4 * 60 * 60 * 1000);

try {
  try { await el("loading-screen").querySelector("img").decode(); } catch {}
  const handover = await api("startup_ready");
  const elapsed = Math.max(0, handover?.elapsedMilliseconds || 0);
  el("loading-progress-fill").style.animationDelay = "-" + elapsed + "ms";
  el("loading-progress-fill").style.animationPlayState = "running";
  startup.handedOver(elapsed);
  await api("show_main_window");
  await listen("patcher-error", (event) => report(event.payload));
  settings = await api("get_settings");
  let attempted = false;
  try { attempted = localStorage.getItem("zephyr.auto-path.v1") === "attempted"; } catch {}
  const detectedPath = await detectFirstLeaguePath(settings, {
    attempted,
    markAttempted: () => localStorage.setItem("zephyr.auto-path.v1", "attempted"),
    detect: () => api("auto_detect_league_path"),
    save: (updated) => api("save_settings", { settings: updated }),
  });
  settings = detectedPath.settings;
  el("league-path").value = settings.leaguePath || "";
  el("auto-run").checked = settings.autoRun === true;
  el("close-to-tray").checked = settings.minimizeToTray !== false;
  translatePage();
  await refreshMods();
  poll();
  finishLoading();
  await action(async () => { await analyzeCustoms(); });
} catch (error) { report(error); }
finally { finishLoading(); }

function finishLoading() { startup.finish(); }

const tipButton = el("startup-tip-button");
tipButton.addEventListener("click", () => tipButton.parentElement.classList.toggle("is-open"));
document.addEventListener("pointerdown", (event) => { if (!tipButton.parentElement.contains(event.target)) tipButton.parentElement.classList.remove("is-open"); });
document.addEventListener("keydown", (event) => { if (event.key === "Escape") { tipButton.parentElement.classList.remove("is-open"); if (document.activeElement === tipButton) tipButton.blur(); } });

// Open the community invite in the default browser.
el("discord-invite").addEventListener("click", (event) => {
  event.preventDefault();
  api("open_discord").catch(report);
});
