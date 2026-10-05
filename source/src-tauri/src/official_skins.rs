use parking_lot::Mutex;
use serde::Serialize;
use std::io::{BufRead, BufReader};
use std::process::{Child, Command, Stdio};
use std::sync::Arc;
use tauri::{AppHandle, Manager};

/// The owned official loader and its latest output.
#[derive(Default)]
pub struct OfficialSkinsState {
    child: Mutex<Option<Child>>,
    message: Arc<Mutex<String>>,
}

/// The official loader's process state and last reported result.
#[derive(Serialize, specta::Type)]
#[serde(rename_all = "camelCase")]
pub struct OfficialStatus {
    /// Whether the loader process is alive.
    pub running: bool,
    /// The loader's last output, including load failures.
    pub message: String,
}

/// State for the application's own official loader.
pub fn setup(app: &mut tauri::App) -> tauri::Result<()> {
    app.manage(OfficialSkinsState::default());
    Ok(())
}

/// Starts the own loader with the bundled official DLL.
pub fn start(app: &AppHandle) -> Result<OfficialStatus, String> {
    #[cfg(not(windows))]
    return Err("This loader requires Windows x64.".to_owned());

    #[cfg(windows)]
    {
        use std::os::windows::process::CommandExt;

        let state = app.state::<OfficialSkinsState>();
        let mut child = state.child.lock();
        if let Some(process) = child.as_mut() {
            if process.try_wait().map_err(|error| error.to_string())?.is_none() {
                return Ok(OfficialStatus {
                    running: true,
                    message: state.message.lock().clone(),
                });
            }
        }

        let resources = app.path().resource_dir().map_err(|error| error.to_string())?;
        let directory = resources.join("r3nz");
        let executable = fs_err::canonicalize(directory.join("official-loader.exe"))
            .map_err(|error| error.to_string())?;
        let dll = fs_err::canonicalize(directory.join("R3nzSkin.dll"))
            .map_err(|error| error.to_string())?;

        let mut process = Command::new(executable)
            .arg(dll)
            .current_dir(directory)
            .creation_flags(0x08000000)
            .stdout(Stdio::piped())
            .stderr(Stdio::null())
            .spawn()
            .map_err(|error| error.to_string())?;
        let output = process.stdout.take().ok_or("Loader output is unavailable")?;
        *state.message.lock() = "Waiting for the game...".to_owned();
        let message = Arc::clone(&state.message);
        std::thread::spawn(move || {
            for line in BufReader::new(output).lines() {
                match line {
                    Ok(line) => *message.lock() = line,
                    Err(error) => {
                        *message.lock() = format!("Failed to read loader output: {error}");
                        break;
                    }
                }
            }
        });
        *child = Some(process);

        let message = state.message.lock().clone();
        Ok(OfficialStatus {
            running: true,
            message,
        })
    }
}

/// The current process state, reaping an exited loader.
pub fn status(app: &AppHandle) -> Result<OfficialStatus, String> {
    let state = app.state::<OfficialSkinsState>();
    let mut child = state.child.lock();
    let running = match child.as_mut() {
        Some(process) => match process.try_wait().map_err(|error| error.to_string())? {
            None => true,
            Some(code) => {
                *state.message.lock() = format!("Loader exited: {code}");
                *child = None;
                false
            }
        },
        None => false,
    };
    let message = state.message.lock().clone();
    Ok(OfficialStatus {
        running,
        message,
    })
}

/// Stops the owned loader without unloading the game's DLL.
pub fn stop(app: &AppHandle) -> Result<(), String> {
    let state = app.state::<OfficialSkinsState>();
    let mut child = state.child.lock();
    if let Some(process) = child.as_mut() {
        if process.try_wait().map_err(|error| error.to_string())?.is_none() {
            process.kill().map_err(|error| error.to_string())?;
            process.wait().map_err(|error| error.to_string())?;
        }
        *child = None;
    }
    *state.message.lock() = "Loader stopped. Exit the game to unload the DLL.".to_owned();
    Ok(())
}

/// Stops the owned loader during application shutdown.
pub fn shutdown(app: &AppHandle) {
    if let Err(error) = stop(app) {
        tracing::warn!(%error, "Official loader shutdown failed");
    }
}
