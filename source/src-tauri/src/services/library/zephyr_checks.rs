// Copyright (C) 2026 stereolove33
// SPDX-License-Identifier: GPL-3.0-or-later

use crate::commands::off_thread;
use crate::error::{AppError, AppResult, IpcResult};
use crate::mods::ModLibraryState;
use crate::patcher::PatcherState;
use crate::state::SettingsState;
use ltk_manager_core::mods::{HealthCheckReadiness, ModHealthVerdict, ModArchiveFormat};
use serde::Serialize;
use sha2::{Digest, Sha256};
use std::collections::{BTreeMap, BTreeSet};
use tauri::{AppHandle, Manager};

/// Content categories visible in a mod's readable resource paths.
#[derive(Serialize, specta::Type)]
#[serde(rename_all = "camelCase")]
pub struct CustomInspection {
    pub categories: Vec<String>,
    pub resource_count: u32,
    pub readable_paths: u32,
}

/// Differing contents written by two enabled mods to the same resource.
#[derive(Serialize, specta::Type)]
#[serde(rename_all = "camelCase")]
pub struct CustomConflict {
    pub first_mod: String,
    pub second_mod: String,
    pub resource: String,
}

/// Fresh checks over the exact enabled mod set.
#[derive(Serialize, specta::Type)]
#[serde(rename_all = "camelCase")]
pub struct CustomPreflight {
    pub conflicts: Vec<CustomConflict>,
    pub verdicts: Vec<ModHealthVerdict>,
    pub partial_mod_ids: Vec<String>,
}

struct Resources {
    hashes: BTreeMap<String, [u8; 32]>,
    categories: BTreeSet<String>,
    readable_paths: u32,
}

fn record(resources: &mut Resources, path: &str, hash: String, bytes: &[u8]) {
    resources.hashes.insert(hash, Sha256::digest(bytes).into());
    let normalized = path.replace('\\', "/").to_ascii_lowercase();
    let parts: Vec<_> = normalized.split('/').collect();
    let file = parts.last().copied().unwrap_or("");
    let stem = file.split('.').next().unwrap_or("");
    if !stem.is_empty() && stem.len() == 16 && stem.chars().all(|c| c.is_ascii_hexdigit()) {
        return;
    }

    resources.readable_paths += 1;
    let has = |word: &str| parts.contains(&word);
    if has("fonts") || has("font") || [".ttf", ".otf", ".woff", ".woff2"].iter().any(|ext| normalized.ends_with(ext)) {
        resources.categories.insert("Font".into());
    } else if has("characters") {
        resources.categories.insert("Skin".into());
    } else if has("ux") || has("hud") || has("ui") || has("interface") {
        resources.categories.insert("UI".into());
    } else if has("maps") || has("levels") {
        resources.categories.insert("Map".into());
    } else if has("audio") || has("sounds") || [".wem", ".bnk", ".ogg", ".wav"].iter().any(|ext| normalized.ends_with(ext)) {
        resources.categories.insert("Audio".into());
    }
}

fn read_resources(enabled: &mut ltk_overlay::EnabledMod) -> AppResult<Resources> {
    let mut resources = Resources { hashes: BTreeMap::new(), categories: BTreeSet::new(), readable_paths: 0 };
    let project = enabled.content.mod_project().map_err(AppError::Overlay)?;
    let layers: Vec<_> = project.layers.iter().map(|layer| layer.name.clone()).collect();

    for layer in layers {
        if enabled.enabled_layers.as_ref().is_some_and(|active| !active.contains(&layer)) {
            continue;
        }
        for wad in enabled.content.list_layer_wads(&layer).map_err(AppError::Overlay)? {
            for (path, bytes) in enabled.content.read_wad_overrides(&layer, &wad).map_err(AppError::Overlay)? {
                let hash = ltk_overlay::utils::resolve_chunk_hash(&path, &bytes)?;
                record(&mut resources, path.as_str(), format!("{hash:016x}"), &bytes);
            }
        }
    }
    for (path, bytes) in enabled.content.read_raw_overrides().map_err(AppError::Overlay)? {
        let hash = ltk_overlay::utils::resolve_chunk_hash(&path, &bytes)?;
        record(&mut resources, path.as_str(), format!("{hash:016x}"), &bytes);
    }

    Ok(resources)
}

/// Inspect actual package resources without using user-entered labels.
#[tauri::command]
#[specta::specta]
pub async fn inspect_custom_mod(mod_id: String, app_handle: AppHandle) -> IpcResult<CustomInspection> {
    let config = app_handle.state::<SettingsState>().config();
    let library = app_handle.state::<ModLibraryState>().0.clone();
    off_thread(move || {
        let (_, mut enabled) = library.build_single_mod_provider(&config, &mod_id)?;
        let resources = read_resources(&mut enabled)?;
        Ok(CustomInspection {
            categories: resources.categories.into_iter().collect(),
            resource_count: resources.hashes.len() as u32,
            readable_paths: resources.readable_paths,
        })
    }).await
}

/// Validate enabled layers, resource overlaps and Mod Health before activation.
#[tauri::command]
#[specta::specta]
pub async fn check_custom_selection(mod_ids: Vec<String>, app_handle: AppHandle) -> IpcResult<CustomPreflight> {
    if let Err(error) = app_handle.state::<PatcherState>().reject_if_running() {
        return IpcResult::from(Err::<CustomPreflight, _>(error));
    }
    let config = app_handle.state::<SettingsState>().config();
    let library = app_handle.state::<ModLibraryState>().0.clone();
    off_thread(move || {
        let installed = library.get_installed_mods(&config)?;
        let partial_ids: BTreeSet<_> = installed.iter()
            .filter(|item| item.format == ModArchiveFormat::Modpkg)
            .map(|item| item.id.clone()).collect();
        if mod_ids.iter().any(|id| !partial_ids.contains(id))
            && !matches!(library.health_check_readiness(), HealthCheckReadiness::Ready) {
            return Err(AppError::Other("Mod Health is unavailable: wait for game hashtable synchronization, then check again.".into()));
        }
        let (_, mut enabled) = library.get_enabled_mods_for_overlay(&config)?;
        let actual: BTreeSet<_> = enabled.iter().map(|item| item.id.clone()).collect();
        let requested: BTreeSet<_> = mod_ids.into_iter().collect();
        if actual != requested {
            return Err(AppError::Other("The enabled custom list changed. Refresh and try again.".into()));
        }

        let mut owners: BTreeMap<String, Vec<(String, [u8; 32])>> = BTreeMap::new();
        let mut conflicts = Vec::new();
        let mut verdicts = Vec::new();
        let mut partial_mod_ids = Vec::new();
        for item in &mut enabled {
            let resources = read_resources(item).map_err(|error| AppError::Other(format!("Cannot read custom {}: {error}", item.id)))?;
            for (resource, digest) in resources.hashes {
                let previous = owners.entry(resource.clone()).or_default();
                for (other, other_digest) in previous.iter() {
                    if *other_digest != digest && conflicts.len() < 100 {
                        conflicts.push(CustomConflict { first_mod: other.clone(), second_mod: item.id.clone(), resource: resource.clone() });
                    }
                }
                previous.push((item.id.clone(), digest));
            }
            if partial_ids.contains(&item.id) {
                partial_mod_ids.push(item.id.clone());
            } else {
                verdicts.push(library.check_mod_health(&config, &item.id)?);
            }
        }

        Ok(CustomPreflight { conflicts, verdicts, partial_mod_ids })
    }).await
}
