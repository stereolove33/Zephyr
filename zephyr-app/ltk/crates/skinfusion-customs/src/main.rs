use std::collections::HashMap;
use std::io::{self, BufRead, Write};
use std::path::PathBuf;
use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::thread::{self, JoinHandle};

use fs_err as fs;
use ltk_manager_core::config::Config;
use ltk_manager_core::diagnostics::incident::OverlayOutcome;
use ltk_manager_core::events::{BackendEvent, EventSink};
use ltk_manager_core::hashtables::{HashtableCache, WadPathResolverState};
use ltk_manager_core::mods::{ChecksumMismatchState, LinkedBinState, ModLibrary, WadReportState};
use ltk_manager_core::patcher::host::{
    HOOK_DLL_NAME, HOST_EXE_NAME, HostConfig, HostLogLevel, PatcherHost,
};
use ltk_manager_core::patcher::injector::{Injector, InjectorEvent};
use ltk_manager_core::patcher::{OverlayRefresh, should_elevate};
use ltk_manager_core::utils::game::GameDir;
use parking_lot::Mutex;
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

mod checks;

#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct Status {
    running: bool,
    phase: String,
    message: String,
    overlay_ready: bool,
    game_pid: Option<u64>,
}

impl Default for Status {
    fn default() -> Self {
        Self {
            running: false,
            phase: "idle".into(),
            message: "Customs paradas.".into(),
            overlay_ready: false,
            game_pid: None,
        }
    }
}

struct Notifications(Arc<Mutex<Status>>);

impl EventSink for Notifications {
    fn emit(&self, event: BackendEvent) {
        if let BackendEvent::OverlayProgress(progress) = event {
            let mut status = self.0.lock();
            if status.phase == "building" {
                status.message = format!(
                    "Preparando customs: {:?} ({}/{}).",
                    progress.stage, progress.current, progress.total,
                );
            }
        }
    }
}

#[derive(Deserialize)]
#[serde(tag = "op", rename_all = "snake_case")]
enum Request {
    List,
    SetLeague {
        path: Option<PathBuf>,
    },
    Import {
        paths: Vec<String>,
    },
    Toggle {
        id: String,
        enabled: bool,
    },
    Layers {
        id: String,
        states: HashMap<String, bool>,
    },
    Remove {
        id: String,
    },
    Start,
    Stop,
    Status,
    Detect,
    Readiness,
    Inspect {
        id: String,
    },
    Analyze {
        id: String,
    },
    Health {
        id: String,
    },
    Preflight {
        ids: Vec<String>,
    },
    Shutdown,
}

struct Backend {
    config: Config,
    config_file: PathBuf,
    app_dir: PathBuf,
    log_file: PathBuf,
    library: Arc<ModLibrary>,
    status: Arc<Mutex<Status>>,
    stop: Arc<AtomicBool>,
    task: Option<JoinHandle<()>>,
    resolver: Arc<WadPathResolverState>,
    syncing: Arc<AtomicBool>,
}

impl Backend {
    fn new() -> Result<Self, String> {
        let app_dir = std::env::current_exe()
            .map_err(|e| e.to_string())?
            .parent()
            .ok_or("Pasta do aplicativo indisponivel.")?
            .to_path_buf();
        let data_dir =
            PathBuf::from(std::env::var_os("LOCALAPPDATA").ok_or("LOCALAPPDATA indisponivel.")?)
                .join("SkinFusionReverse");
        fs::create_dir_all(&data_dir).map_err(|e| e.to_string())?;

        let config_file = data_dir.join("config.json");
        let config = if config_file.exists() {
            let bytes = fs::read(&config_file).map_err(|e| e.to_string())?;
            serde_json::from_slice(&bytes).map_err(|e| format!("Configuracao invalida: {e}"))?
        } else {
            Config::default()
        };
        let storage = data_dir.join("customs");
        let status = Arc::new(Mutex::new(Status::default()));
        let resolver = Arc::new(WadPathResolverState::default());
        let library = Arc::new(ModLibrary::new(
            Arc::new(Notifications(status.clone())),
            Some(storage.clone()),
            "1.26.1-skinfusion-reverse-01",
            Arc::new(LinkedBinState::default()),
            Arc::new(ChecksumMismatchState::default()),
            Arc::new(WadReportState::new(Some(&storage))),
            resolver.clone(),
        ));

        let backend = Self {
            config,
            config_file,
            app_dir,
            log_file: data_dir.join("customs.log"),
            library,
            status,
            stop: Arc::new(AtomicBool::new(false)),
            task: None,
            resolver,
            syncing: Arc::new(AtomicBool::new(false)),
        };
        backend.sync_tables();
        Ok(backend)
    }

    fn sync_tables(&self) {
        if self.library.hashtables_ready() || self.syncing.swap(true, Ordering::SeqCst) {
            return;
        }

        let syncing = self.syncing.clone();
        let resolver = self.resolver.clone();
        let status = self.status.clone();
        thread::spawn(move || {
            let result = HashtableCache::shared().and_then(|cache| {
                cache.sync(
                    false,
                    "ltk-manager/1.26.1-skinfusion-zephyr",
                    &Notifications(status),
                )
            });
            match result {
                Ok(_) => resolver.invalidate(),
                Err(error) => eprintln!("Tabelas para verificar customs: {error}"),
            }
            syncing.store(false, Ordering::SeqCst);
        });
    }

    fn require_stopped(&self) -> Result<(), String> {
        if self.status.lock().running {
            return Err("Pare as customs antes de alterar a biblioteca ou a pasta do jogo.".into());
        }

        Ok(())
    }

    fn list(&self) -> Result<Value, String> {
        let mods = self
            .library
            .get_installed_mods(&self.config)
            .map_err(|e| e.to_string())?;
        Ok(json!({ "mods": mods, "leaguePath": self.config.league_path }))
    }

    fn handle(&mut self, request: Request) -> Result<Value, String> {
        match request {
            Request::List => self.list(),
            Request::Status => Ok(json!(*self.status.lock())),
            Request::Detect => Ok(json!(
                ltk_manager_core::launcher::detect_league_install_root()
                    .or_else(|| self.config.league_path.clone())
            )),
            Request::Readiness => {
                self.sync_tables();
                Ok(json!(if self.library.hashtables_ready() {
                    "ready"
                } else if self.syncing.load(Ordering::SeqCst) {
                    "syncing"
                } else {
                    "unsynced"
                }))
            }
            Request::Inspect { id } => {
                self.require_stopped()?;
                checks::inspect(&self.library, &self.config, &id)
            }
            Request::Analyze { id } => {
                self.require_stopped()?;
                checks::analyze(&self.library, &self.config, &id)
            }
            Request::Health { id } => {
                self.require_stopped()?;
                self.library
                    .check_mod_health(&self.config, &id)
                    .map(|verdict| json!(verdict))
                    .map_err(|error| error.to_string())
            }
            Request::Preflight { ids } => {
                self.require_stopped()?;
                checks::preflight(&self.library, &self.config, ids)
            }
            Request::SetLeague { path } => {
                self.require_stopped()?;
                let mut config = self.config.clone();
                config.league_path = path;
                if config.league_path.is_some() {
                    GameDir::resolve(&config).map_err(|e| e.to_string())?;
                }
                let bytes = serde_json::to_vec_pretty(&config).map_err(|e| e.to_string())?;
                let staged = self.config_file.with_extension("json.tmp");
                fs::write(&staged, bytes).map_err(|e| e.to_string())?;
                fs::rename(&staged, &self.config_file).map_err(|e| e.to_string())?;
                self.config = config;
                self.list()
            }
            Request::Import { paths } => {
                self.require_stopped()?;
                let result = self
                    .library
                    .install_mods_from_packages(&self.config, &paths)
                    .map_err(|e| e.to_string())?;
                Ok(json!(result))
            }
            Request::Toggle { id, enabled } => {
                self.require_stopped()?;
                self.library
                    .toggle_mod_enabled(&self.config, &id, enabled)
                    .map_err(|e| e.to_string())?;
                Ok(Value::Null)
            }
            Request::Layers { id, states } => {
                self.require_stopped()?;
                self.library
                    .set_mod_layers(&self.config, &id, states)
                    .map_err(|e| e.to_string())?;
                Ok(Value::Null)
            }
            Request::Remove { id } => {
                self.require_stopped()?;
                self.library
                    .uninstall_mod_by_id(&self.config, &id)
                    .map_err(|e| e.to_string())?;
                Ok(Value::Null)
            }
            Request::Start => {
                self.start()?;
                Ok(json!(*self.status.lock()))
            }
            Request::Stop | Request::Shutdown => {
                self.stop.store(true, Ordering::SeqCst);
                if self.status.lock().running {
                    let mut status = self.status.lock();
                    status.phase = "stopping".into();
                    status.message = "Parando customs...".into();
                }
                Ok(Value::Null)
            }
        }
    }

    fn start(&mut self) -> Result<(), String> {
        self.require_stopped()?;
        if let Some(task) = self.task.take() {
            let _ = task.join();
        }

        GameDir::resolve(&self.config).map_err(|e| e.to_string())?;
        let mods = self
            .library
            .get_installed_mods(&self.config)
            .map_err(|e| e.to_string())?;
        if !mods.iter().any(|m| m.enabled) {
            return Err("Marque pelo menos uma custom skin antes de iniciar.".into());
        }
        let host_path = self.app_dir.join(HOST_EXE_NAME);
        for path in [&host_path, &self.app_dir.join(HOOK_DLL_NAME)] {
            if !path.is_file() {
                return Err(format!("Arquivo necessario ausente: {}", path.display()));
            }
        }

        self.stop.store(false, Ordering::SeqCst);
        *self.status.lock() = Status {
            running: true,
            phase: "building".into(),
            message: "Preparando customs...".into(),
            ..Status::default()
        };
        let library = self.library.clone();
        let config = self.config.clone();
        let stop = self.stop.clone();
        let status = self.status.clone();
        let log_file = self.log_file.clone();
        self.task = Some(thread::spawn(move || {
            let result = std::panic::catch_unwind(std::panic::AssertUnwindSafe(|| {
                run_customs(
                    library,
                    config,
                    host_path,
                    stop.clone(),
                    status.clone(),
                    log_file,
                )
            }));
            let mut current = status.lock();
            current.running = false;
            current.overlay_ready = false;
            current.game_pid = None;
            match result {
                Ok(Ok(())) if current.phase == "failed" && !stop.load(Ordering::SeqCst) => {}
                Ok(Ok(())) => {
                    current.phase = "idle".into();
                    current.message = "Customs paradas.".into();
                }
                Ok(Err(_)) if stop.load(Ordering::SeqCst) => {
                    current.phase = "idle".into();
                    current.message = "Customs paradas.".into();
                }
                Ok(Err(error)) => {
                    current.phase = "failed".into();
                    current.message = error;
                }
                Err(_) => {
                    current.phase = "failed".into();
                    current.message = "Falha interna no nucleo de customs.".into();
                }
            }
        }));

        Ok(())
    }

    fn shutdown(&mut self) {
        self.stop.store(true, Ordering::SeqCst);
        if let Some(task) = self.task.take() {
            let _ = task.join();
        }
    }
}

fn record_event(event: InjectorEvent, status: &Mutex<Status>, log: &PathBuf) {
    if let Ok(mut file) = fs::OpenOptions::new().create(true).append(true).open(log) {
        let _ = writeln!(file, "{event:?}");
    }

    let mut current = status.lock();
    match event {
        InjectorEvent::Scanning | InjectorEvent::GameExited => {
            current.phase = "waiting".into();
            current.message = "Customs prontas. Aguardando a partida.".into();
            current.game_pid = None;
        }
        InjectorEvent::GameFound => {
            current.phase = "attaching".into();
            current.message = "Partida encontrada. Carregando customs...".into();
        }
        InjectorEvent::GameAttached { pid } => {
            current.phase = "attached".into();
            current.message = "Modulo de customs carregado. Aguardando confirmacao.".into();
            current.game_pid = pid;
        }
        InjectorEvent::Overlay {
            outcome: OverlayOutcome::Live,
            ..
        }
        | InjectorEvent::WadRedirected { .. } => {
            current.phase = "active".into();
            current.message = "Customs aplicadas nesta partida.".into();
        }
        InjectorEvent::Overlay { outcome, detail } => {
            current.phase = "warning".into();
            current.message = format!("Estado das customs: {outcome:?}. {detail:?}");
        }
        InjectorEvent::WadScanFailed { failures } => {
            current.phase = "failed".into();
            current.message = format!("O LTK rejeitou arquivos da custom: {failures:?}");
        }
        _ => {}
    }
}

fn run_customs(
    library: Arc<ModLibrary>,
    config: Config,
    host_path: PathBuf,
    stop: Arc<AtomicBool>,
    status: Arc<Mutex<Status>>,
    log_file: PathBuf,
) -> Result<(), String> {
    let cancellation = stop.clone();
    let build = library
        .ensure_overlay(&config, &[], false, move || {
            cancellation.load(Ordering::SeqCst)
        })
        .map_err(|e| e.to_string())?;
    library.record_overlay_build(build.outcome);
    if stop.load(Ordering::SeqCst) {
        return Ok(());
    }

    let prefix = ltk_manager_core::patcher::session::normalize_overlay_prefix(
        &build.overlay_root.to_string_lossy(),
    );
    let elevate = should_elevate(&config);
    let mut host = PatcherHost::spawn(&host_path, elevate).map_err(|e| e.to_string())?;
    host.configure(&HostConfig {
        prefix,
        log_level: HostLogLevel::Info,
        flags: 0,
    })
    .map_err(|e| e.to_string())?;
    host.start_scan().map_err(|e| e.to_string())?;
    let events = host.take_events().ok_or("Canal do LTK indisponivel.")?;
    let host = Arc::new(Mutex::new(Some(host)));
    {
        let mut current = status.lock();
        current.overlay_ready = true;
        current.phase = "waiting".into();
        current.message = "Customs prontas. Aguardando a partida.".into();
    }

    let refresh = OverlayRefresh::default();
    let result = Injector::new()
        .with_elevate(elevate)
        .on_event(move |event| {
            record_event(event, &status, &log_file);
        })
        .run_session(events, &host, &stop, &refresh)
        .0;
    if let Some(mut host) = host.lock().take() {
        let _ = host.stop_session();
        host.shutdown();
    }

    result.map(|_| ()).map_err(|e| e.to_string())
}

fn main() {
    let mut backend = match Backend::new() {
        Ok(backend) => backend,
        Err(error) => {
            eprintln!("{error}");
            std::process::exit(1);
        }
    };
    let stdin = io::stdin();
    let mut stdout = io::stdout().lock();
    for line in stdin.lock().lines() {
        let Ok(line) = line else { break };
        let request = serde_json::from_str::<Request>(&line);
        let closing = matches!(&request, Ok(Request::Shutdown));
        let result = match request {
            Ok(request) => backend.handle(request),
            Err(error) => Err(format!("Pedido invalido: {error}")),
        };
        let response = match result {
            Ok(data) => json!({ "ok": true, "data": data }),
            Err(error) => json!({ "ok": false, "error": error }),
        };
        if writeln!(stdout, "{response}")
            .and_then(|_| stdout.flush())
            .is_err()
            || closing
        {
            break;
        }
    }
    backend.shutdown();
}
