/* Copyright (C) 2026 stereolove33 | SPDX-License-Identifier: GPL-3.0-or-later */
export function automaticLabels(footprint, inspection) {
  const derived = footprint?.derived || {};
  const categories = new Set(inspection?.categories || []);
  for (const tag of derived.tags || []) {
    if (tag === "champion-skin") categories.add("Skin");
    if (tag === "map-skin") categories.add("Map");
    if (tag === "ui" && !categories.has("Font") && !categories.has("Audio")) categories.add("UI");
  }
  const champions = Array.isArray(derived.champions) ? derived.champions : [];
  return {
    champion: champions.join(", "),
    type: categories.size > 1 ? "Mixed" : ([...categories][0] || "Unknown"),
    champions,
  };
}

export function preflightIssues(result) {
  if (!result || !Array.isArray(result.verdicts) || !Array.isArray(result.conflicts)) {
    throw new Error("Incomplete custom verification response.");
  }
  const broken = result.verdicts.filter((verdict) => (verdict.counts?.fatals || 0) + (verdict.counts?.errors || 0) > 0);
  const warnings = result.verdicts.filter((verdict) => (verdict.counts?.warnings || 0) > 0);
  return { broken, warnings, conflicts: result.conflicts, blocked: broken.length > 0 || result.conflicts.length > 0 };
}

export function healthLabel(verdict) {
  if (!verdict?.counts) return "Not checked";
  if ((verdict.counts.fatals || 0) + (verdict.counts.errors || 0) > 0) return "Errors found";
  if ((verdict.counts.warnings || 0) > 0) return "Warnings found";
  return "No issues detected";
}
