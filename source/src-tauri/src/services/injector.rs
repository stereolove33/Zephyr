use tauri::plugin::TauriPlugin;
use tauri::{AppHandle, Wry};

use crate::error::{AppError, IpcResult};
use crate::official_skins::{self, OfficialStatus};

/// The own-injector IPC service.
pub struct Table;

/// Commands for the own official loader.
pub fn plugin() -> TauriPlugin<Wry> {
    super::plugin::<Table>().build()
}

/// Starts the own official loader.
#[tauri::command]
#[specta::specta]
pub fn start_official(app: AppHandle) -> IpcResult<OfficialStatus> {
    official_skins::start(&app).map_err(AppError::Other).into()
}

/// Stops the own official loader.
#[tauri::command]
#[specta::specta]
pub fn stop_official(app: AppHandle) -> IpcResult<()> {
    official_skins::stop(&app).map_err(AppError::Other).into()
}

/// The own loader's process state and latest output.
#[tauri::command]
#[specta::specta]
pub fn official_status(app: AppHandle) -> IpcResult<OfficialStatus> {
    official_skins::status(&app).map_err(AppError::Other).into()
}
