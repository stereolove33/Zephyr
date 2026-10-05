#include <Windows.h>
// Loader compatibility: preserve the exported hook name of the functional DLL.
// Game initialization remains in the existing DllMain.
extern "C" __declspec(dllexport) LRESULT CALLBACK NextHook(int code, WPARAM wParam, LPARAM lParam)
{
    return CallNextHookEx(nullptr, code, wParam, lParam);
}
