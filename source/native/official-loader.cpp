/* Copyright (C) 2026 stereolove33 | SPDX-License-Identifier: GPL-3.0-or-later */
#define UNICODE
#define _UNICODE
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <tlhelp32.h>
#include <cstdint>
#include <filesystem>
#include <iostream>
#include <string>
#include <unordered_set>

static_assert(sizeof(void*) == 8, "Build from the x64 Visual Studio terminal.");

struct Handle {
    HANDLE value;
    explicit Handle(HANDLE value) : value(value) {}
    ~Handle() { if (value && value != INVALID_HANDLE_VALUE) CloseHandle(value); }
    Handle(const Handle&) = delete;
    Handle& operator=(const Handle&) = delete;
    explicit operator bool() const { return value && value != INVALID_HANDLE_VALUE; }
};

static std::string error_text(const char* operation) {
    return std::string(operation) + " failed (Windows " + std::to_string(GetLastError()) + ").";
}

static uintptr_t module_base(DWORD pid, const std::wstring& name) {
    Handle snapshot(CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, pid));
    if (!snapshot) return 0;

    MODULEENTRY32W entry{};
    entry.dwSize = sizeof(entry);
    if (Module32FirstW(snapshot.value, &entry)) {
        do {
            if (_wcsicmp(entry.szModule, name.c_str()) == 0) {
                return reinterpret_cast<uintptr_t>(entry.modBaseAddr);
            }
        } while (Module32NextW(snapshot.value, &entry));
    }
    return 0;
}

static bool load_official(DWORD pid, const std::wstring& dll, std::string& message) {
    const auto dll_name = std::filesystem::path(dll).filename().wstring();
    if (module_base(pid, dll_name)) {
        message = "Official DLL already loaded. PID " + std::to_string(pid);
        return true;
    }

    const auto load_library = GetProcAddress(GetModuleHandleW(L"kernel32.dll"), "LoadLibraryW");
    HMODULE owner = nullptr;
    if (!load_library || !GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCWSTR>(load_library), &owner)) {
        message = error_text("Resolve LoadLibraryW");
        return false;
    }

    wchar_t owner_path[32768]{};
    if (!GetModuleFileNameW(owner, owner_path, 32768)) {
        message = error_text("Resolve library owner");
        return false;
    }
    const auto remote_owner = module_base(pid, std::filesystem::path(owner_path).filename().wstring());
    if (!remote_owner) {
        message = "Cannot resolve the game's loader module.";
        return false;
    }
    const auto rva = reinterpret_cast<uintptr_t>(load_library) - reinterpret_cast<uintptr_t>(owner);
    const auto remote_loader = reinterpret_cast<LPTHREAD_START_ROUTINE>(remote_owner + rva);

    Handle process(OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION |
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, FALSE, pid));
    if (!process) {
        message = error_text("OpenProcess");
        return false;
    }

    const auto bytes = (dll.size() + 1) * sizeof(wchar_t);
    const auto remote_path = VirtualAllocEx(process.value, nullptr, bytes,
        MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (!remote_path) {
        message = error_text("VirtualAllocEx");
        return false;
    }

    SIZE_T written = 0;
    if (!WriteProcessMemory(process.value, remote_path, dll.c_str(), bytes, &written) || written != bytes) {
        message = error_text("WriteProcessMemory");
        VirtualFreeEx(process.value, remote_path, 0, MEM_RELEASE);
        return false;
    }

    Handle thread(CreateRemoteThread(process.value, nullptr, 0, remote_loader, remote_path, 0, nullptr));
    if (!thread) {
        message = error_text("CreateRemoteThread");
        VirtualFreeEx(process.value, remote_path, 0, MEM_RELEASE);
        return false;
    }

    const auto wait = WaitForSingleObject(thread.value, 15000);
    if (wait != WAIT_OBJECT_0) {
        // The thread may still read its argument. Freeing it after a timeout would race it.
        message = "DLL load did not finish within 15 seconds. Stop this test and close the game.";
        return false;
    }
    VirtualFreeEx(process.value, remote_path, 0, MEM_RELEASE);

    if (!module_base(pid, dll_name)) {
        message = "DLL was not confirmed in the game. The load may have been rejected.";
        return false;
    }
    message = "Official DLL loaded. PID " + std::to_string(pid) + ". Check the in-game menu.";
    return true;
}

int wmain(int argc, wchar_t** argv) {
    if (argc != 2 || !std::filesystem::is_regular_file(argv[1])) {
        std::cout << "ERROR|Official DLL is missing." << std::endl;
        return 2;
    }
    const auto dll = std::filesystem::absolute(argv[1]).wstring();
    std::unordered_set<DWORD> attempted;
    std::cout << "WAITING|Waiting for League of Legends.exe" << std::endl;

    while (true) {
        Handle snapshot(CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0));
        if (!snapshot) {
            std::cout << "ERROR|" << error_text("Process snapshot") << std::endl;
            return 3;
        }
        std::unordered_set<DWORD> active;
        PROCESSENTRY32W entry{};
        entry.dwSize = sizeof(entry);
        if (Process32FirstW(snapshot.value, &entry)) {
            do {
                if (_wcsicmp(entry.szExeFile, L"League of Legends.exe") == 0) {
                    active.insert(entry.th32ProcessID);
                    if (attempted.insert(entry.th32ProcessID).second) {
                        std::cout << "LOADING|Game found. PID " << entry.th32ProcessID << std::endl;
                        std::string message;
                        Sleep(10000);
                        const auto success = load_official(entry.th32ProcessID, dll, message);
                        std::cout << (success ? "LOADED|" : "ERROR|") << message << std::endl;
                    }
                }
            } while (Process32NextW(snapshot.value, &entry));
        }
        for (auto it = attempted.begin(); it != attempted.end();) {
            if (!active.count(*it)) it = attempted.erase(it);
            else ++it;
        }
        Sleep(1000);
    }
}
