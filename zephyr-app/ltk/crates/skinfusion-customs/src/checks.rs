// Copyright (C) 2026 stereolove33
// SPDX-License-Identifier: GPL-3.0-or-later

use std::collections::{BTreeMap, BTreeSet};

use camino::Utf8PathBuf;
use ltk_manager_core::config::Config;
use ltk_manager_core::mods::{ModArchiveFormat, ModLibrary, ModWadReport};
use ltk_manager_core::utils::game::GameDir;
use serde_json::{Value, json};
use sha2::{Digest, Sha256};

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
    if has("fonts")
        || has("font")
        || [".ttf", ".otf", ".woff", ".woff2"]
            .iter()
            .any(|ext| normalized.ends_with(ext))
    {
        resources.categories.insert("Font".into());
    } else if has("characters") {
        resources.categories.insert("Skin".into());
    } else if has("ux") || has("hud") || has("ui") || has("interface") {
        resources.categories.insert("UI".into());
    } else if has("maps") || has("levels") {
        resources.categories.insert("Map".into());
    } else if has("audio")
        || has("sounds")
        || [".wem", ".bnk", ".ogg", ".wav"]
            .iter()
            .any(|ext| normalized.ends_with(ext))
    {
        resources.categories.insert("Audio".into());
    }
}

fn read_resources(enabled: &mut ltk_overlay::EnabledMod) -> Result<Resources, String> {
    let mut resources = Resources {
        hashes: BTreeMap::new(),
        categories: BTreeSet::new(),
        readable_paths: 0,
    };
    let project = enabled
        .content
        .mod_project()
        .map_err(|error| error.to_string())?;
    let layers: Vec<_> = project
        .layers
        .iter()
        .map(|layer| layer.name.clone())
        .collect();

    for layer in layers {
        if enabled
            .enabled_layers
            .as_ref()
            .is_some_and(|active| !active.contains(&layer))
        {
            continue;
        }
        for wad in enabled
            .content
            .list_layer_wads(&layer)
            .map_err(|error| error.to_string())?
        {
            for (path, bytes) in enabled
                .content
                .read_wad_overrides(&layer, &wad)
                .map_err(|error| error.to_string())?
            {
                let hash = ltk_overlay::utils::resolve_chunk_hash(&path, &bytes)
                    .map_err(|error| error.to_string())?;
                record(
                    &mut resources,
                    path.as_str(),
                    format!("{hash:016x}"),
                    &bytes,
                );
            }
        }
    }
    for (path, bytes) in enabled
        .content
        .read_raw_overrides()
        .map_err(|error| error.to_string())?
    {
        let hash = ltk_overlay::utils::resolve_chunk_hash(&path, &bytes)
            .map_err(|error| error.to_string())?;
        record(
            &mut resources,
            path.as_str(),
            format!("{hash:016x}"),
            &bytes,
        );
    }

    Ok(resources)
}

/// Categories inferred from the package's readable resource paths.
pub(crate) fn inspect(library: &ModLibrary, config: &Config, id: &str) -> Result<Value, String> {
    let (_, mut enabled) = library
        .build_single_mod_provider(config, id)
        .map_err(|error| error.to_string())?;
    let resources = read_resources(&mut enabled)?;
    Ok(json!({
        "categories": resources.categories,
        "resourceCount": resources.hashes.len(),
        "readablePaths": resources.readable_paths,
    }))
}

/// WAD footprint using the same overlay provider as the patcher.
pub(crate) fn analyze(library: &ModLibrary, config: &Config, id: &str) -> Result<Value, String> {
    let game_dir = GameDir::resolve(config)
        .map_err(|error| error.to_string())?
        .into_path();
    let (profile_dir, mut enabled) = library
        .build_single_mod_provider(config, id)
        .map_err(|error| error.to_string())?;
    let game_dir = Utf8PathBuf::from_path_buf(game_dir)
        .map_err(|path| format!("Caminho invalido: {}", path.display()))?;
    let profile_dir = Utf8PathBuf::from_path_buf(profile_dir)
        .map_err(|path| format!("Caminho invalido: {}", path.display()))?;
    let upstream =
        ltk_overlay::OverlayBuilder::analyze_single_mod(&game_dir, &profile_dir, &mut enabled)
            .map_err(|error| error.to_string())?;
    let mut report = ModWadReport::from_upstream(upstream);
    library.apply_precise_categorization(config, game_dir.as_std_path(), &mut report);
    Ok(json!(report))
}

/// Fresh checks over the exact enabled mods and their active layers.
pub(crate) fn preflight(
    library: &ModLibrary,
    config: &Config,
    ids: Vec<String>,
) -> Result<Value, String> {
    let installed = library
        .get_installed_mods(config)
        .map_err(|error| error.to_string())?;
    let partial_ids: BTreeSet<_> = installed
        .iter()
        .filter(|item| item.format == ModArchiveFormat::Modpkg)
        .map(|item| item.id.clone())
        .collect();
    if ids.iter().any(|id| !partial_ids.contains(id)) && !library.hashtables_ready() {
        return Err(
            "Aguarde a sincronizacao das tabelas e clique em Verificar customs novamente.".into(),
        );
    }
    let (_, mut enabled) = library
        .get_enabled_mods_for_overlay(config)
        .map_err(|error| error.to_string())?;
    let actual: BTreeSet<_> = enabled.iter().map(|item| item.id.clone()).collect();
    let requested: BTreeSet<_> = ids.into_iter().collect();
    if actual != requested {
        return Err("A selecao de customs mudou. Atualize a lista e tente novamente.".into());
    }

    let mut owners: BTreeMap<String, Vec<(String, [u8; 32])>> = BTreeMap::new();
    let mut conflicts = Vec::new();
    let mut verdicts = Vec::new();
    let mut partial_mod_ids = Vec::new();
    for item in &mut enabled {
        let resources = read_resources(item)
            .map_err(|error| format!("Nao foi possivel ler {}: {error}", item.id))?;
        for (resource, digest) in resources.hashes {
            let previous = owners.entry(resource.clone()).or_default();
            for (other, other_digest) in previous.iter() {
                if *other_digest != digest && conflicts.len() < 100 {
                    conflicts.push(
                        json!({ "firstMod": other, "secondMod": item.id, "resource": resource }),
                    );
                }
            }
            previous.push((item.id.clone(), digest));
        }
        if partial_ids.contains(&item.id) {
            partial_mod_ids.push(item.id.clone());
        } else {
            verdicts.push(
                library
                    .check_mod_health(config, &item.id)
                    .map_err(|error| error.to_string())?,
            );
        }
    }

    Ok(json!({ "conflicts": conflicts, "verdicts": verdicts, "partialModIds": partial_mod_ids }))
}
