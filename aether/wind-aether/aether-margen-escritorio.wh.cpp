// ==WindhawkMod==
// @id              aether-margen-escritorio
// @name            Aether - Margen inferior del escritorio
// @description     Evita que la barra de tareas tape la última fila de íconos del escritorio cuando la barra está en auto-ocultar
// @version         1.0.0
// @author          Aether OS
// @include         explorer.exe
// @architecture    x86-64
// ==/WindhawkMod==

// Parte de Aether OS. Código publicado bajo la licencia GNU GPL v3.0.

// ==WindhawkModReadme==
/*
# Aether - Margen inferior del escritorio

Cuando la barra de tareas está en **auto-ocultar** (por ejemplo, con el mod
"Ocultar barra de tareas al maximizar"), Windows le da al escritorio toda la
pantalla y acomoda los íconos hasta el borde de abajo. Al aparecer la barra,
queda encima de la última fila y no se ven sus nombres.

Este mod reserva una franja abajo, del alto de la barra de tareas, donde el
escritorio **no pone íconos**. Las ventanas no cambian: al maximizar siguen
usando toda la pantalla.

**Funciona mejor con "Organizar iconos automáticamente" activado** (clic
derecho en el escritorio → Ver). Si lo tienes apagado, mueve a mano los íconos
que estén en la franja de abajo una sola vez.

Al desactivar el mod, el escritorio vuelve a como estaba.
*/
// ==/WindhawkModReadme==

// ==WindhawkModSettings==
/*
- extraMargin: 8
  $name: Margen extra (px)
  $description: >-
    Espacio adicional por encima de la barra de tareas. Súbelo si con la barra
    tipo dock todavía se tapa un poco el texto de los íconos.
- manualHeight: 0
  $name: Alto manual de la barra (px)
  $description: >-
    0 = detectar automáticamente el alto de la barra de tareas. Escribe un
    número solo si la detección automática no queda bien.
*/
// ==/WindhawkModSettings==

#include <commctrl.h>

#include <atomic>

struct {
    int extraMargin;
    int manualHeight;
} g_settings;

using ListViewWndProc_t = LRESULT(CALLBACK*)(HWND, UINT, WPARAM, LPARAM);
ListViewWndProc_t ListViewWndProc_Original;

constexpr int kMaxWorkAreas = LV_MAX_WORKAREAS;

std::atomic<HWND> g_listView{nullptr};
std::atomic<bool> g_hooked{false};
std::atomic<bool> g_disabled{false};
HANDLE g_thread = nullptr;
HANDLE g_stopEvent = nullptr;

// Las áreas de trabajo que Explorer quería, sin nuestro ajuste. Se usan para
// dejar todo como estaba al desactivar el mod.
CRITICAL_SECTION g_origLock;
RECT g_origRects[kMaxWorkAreas];
int g_origCount = 0;
bool g_haveOrig = false;

bool IsClass(HWND hWnd, LPCWSTR className) {
    WCHAR buffer[64];
    if (!hWnd || !GetClassNameW(hWnd, buffer, ARRAYSIZE(buffer))) {
        return false;
    }
    return _wcsicmp(buffer, className) == 0;
}

bool IsDesktopListView(HWND hWnd) {
    if (hWnd == g_listView.load()) {
        return true;
    }
    HWND defView = GetParent(hWnd);
    if (!IsClass(defView, L"SHELLDLL_DefView")) {
        return false;
    }
    HWND host = GetParent(defView);
    return IsClass(host, L"Progman") || IsClass(host, L"WorkerW");
}

HWND FindDesktopListView() {
    HWND defView = nullptr;
    if (HWND progman = FindWindowW(L"Progman", nullptr)) {
        defView = FindWindowExW(progman, nullptr, L"SHELLDLL_DefView", nullptr);
    }
    if (!defView) {
        HWND worker = nullptr;
        while ((worker = FindWindowExW(nullptr, worker, L"WorkerW", nullptr))) {
            defView = FindWindowExW(worker, nullptr, L"SHELLDLL_DefView", nullptr);
            if (defView) {
                break;
            }
        }
    }
    if (!defView) {
        return nullptr;
    }
    HWND listView = FindWindowExW(defView, nullptr, L"SysListView32", nullptr);
    if (!listView) {
        return nullptr;
    }
    DWORD pid = 0;
    GetWindowThreadProcessId(listView, &pid);
    return pid == GetCurrentProcessId() ? listView : nullptr;
}

// Alto de la barra de tareas que está abajo en el monitor indicado (0 si no hay).
int GetBottomTaskbarHeight(HMONITOR monitor, const RECT& monitorRect) {
    if (g_settings.manualHeight > 0) {
        return g_settings.manualHeight;
    }

    HWND taskbars[16];
    int count = 0;
    if (HWND primary = FindWindowW(L"Shell_TrayWnd", nullptr)) {
        taskbars[count++] = primary;
    }
    HWND secondary = nullptr;
    while (count < ARRAYSIZE(taskbars) &&
           (secondary = FindWindowExW(nullptr, secondary,
                                      L"Shell_SecondaryTrayWnd", nullptr))) {
        taskbars[count++] = secondary;
    }

    for (int i = 0; i < count; i++) {
        if (MonitorFromWindow(taskbars[i], MONITOR_DEFAULTTONEAREST) !=
            monitor) {
            continue;
        }
        RECT rc;
        if (!GetWindowRect(taskbars[i], &rc)) {
            continue;
        }
        int width = rc.right - rc.left;
        int height = rc.bottom - rc.top;
        int monitorCenterY = (monitorRect.top + monitorRect.bottom) / 2;
        bool horizontal = width >= height;
        bool atBottom = (rc.top + rc.bottom) / 2 > monitorCenterY;
        // Con auto-ocultar, la ventana conserva su alto aunque esté escondida.
        if (horizontal && atBottom && height > 0 && height < 400) {
            return height;
        }
    }
    return 0;
}

void AdjustRects(HWND listView, RECT* rects, int count) {
    for (int i = 0; i < count; i++) {
        RECT r = rects[i];
        MapWindowPoints(listView, HWND_DESKTOP, reinterpret_cast<POINT*>(&r), 2);

        HMONITOR monitor = MonitorFromRect(&r, MONITOR_DEFAULTTONEAREST);
        MONITORINFO mi{sizeof(mi)};
        if (!GetMonitorInfoW(monitor, &mi)) {
            continue;
        }

        int taskbarHeight = GetBottomTaskbarHeight(monitor, mi.rcMonitor);
        if (taskbarHeight <= 0) {
            continue;
        }

        LONG limit = mi.rcMonitor.bottom - taskbarHeight - g_settings.extraMargin;
        // Solo recorta si hace falta, y nunca deja un área ridículamente chica.
        if (r.bottom > limit && limit - r.top > 200) {
            r.bottom = limit;
            MapWindowPoints(HWND_DESKTOP, listView, reinterpret_cast<POINT*>(&r),
                            2);
            rects[i].bottom = r.bottom;
        }
    }
}

LRESULT CALLBACK ListViewWndProc_Hook(HWND hWnd,
                                      UINT msg,
                                      WPARAM wParam,
                                      LPARAM lParam) {
    if (msg != LVM_SETWORKAREAS || !IsDesktopListView(hWnd)) {
        return ListViewWndProc_Original(hWnd, msg, wParam, lParam);
    }

    int count = static_cast<int>(wParam);
    const RECT* source = reinterpret_cast<const RECT*>(lParam);
    if (count < 0 || !source) {
        count = 0;
    }
    if (count > kMaxWorkAreas) {
        count = kMaxWorkAreas;
    }

    EnterCriticalSection(&g_origLock);
    g_origCount = count;
    if (count > 0) {
        memcpy(g_origRects, source, count * sizeof(RECT));
    }
    g_haveOrig = true;
    LeaveCriticalSection(&g_origLock);

    if (g_disabled) {
        return ListViewWndProc_Original(hWnd, msg, wParam, lParam);
    }

    RECT rects[kMaxWorkAreas];
    if (count > 0) {
        memcpy(rects, source, count * sizeof(RECT));
    } else {
        // Sin áreas definidas, el escritorio usa toda la ventana.
        GetClientRect(hWnd, &rects[0]);
        count = 1;
    }

    AdjustRects(hWnd, rects, count);

    Wh_Log(L"Áreas de trabajo ajustadas: %d", count);
    return ListViewWndProc_Original(hWnd, msg, count,
                                    reinterpret_cast<LPARAM>(rects));
}

void ArrangeIfAuto(HWND listView) {
    if (GetWindowLongPtrW(listView, GWL_STYLE) & LVS_AUTOARRANGE) {
        SendMessageW(listView, LVM_ARRANGE, LVA_DEFAULT, 0);
    }
}

// Vuelve a mandar las áreas de Explorer para que pasen por el ajuste.
void Reapply(HWND listView) {
    RECT rects[kMaxWorkAreas];
    int count = 0;

    EnterCriticalSection(&g_origLock);
    bool haveOrig = g_haveOrig;
    if (haveOrig) {
        count = g_origCount;
        memcpy(rects, g_origRects, sizeof(rects));
    }
    LeaveCriticalSection(&g_origLock);

    if (!haveOrig) {
        UINT current = 0;
        SendMessageW(listView, LVM_GETNUMBEROFWORKAREAS, 0,
                     reinterpret_cast<LPARAM>(&current));
        count = static_cast<int>(current > kMaxWorkAreas ? kMaxWorkAreas
                                                         : current);
        if (count > 0) {
            SendMessageW(listView, LVM_GETWORKAREAS, count,
                         reinterpret_cast<LPARAM>(rects));
        }
    }

    SendMessageW(listView, LVM_SETWORKAREAS, count,
                 count > 0 ? reinterpret_cast<LPARAM>(rects) : 0);
    ArrangeIfAuto(listView);
}

bool HookListView(HWND listView, bool applyNow) {
    if (g_hooked) {
        return true;
    }
    auto wndProc = reinterpret_cast<ListViewWndProc_t>(
        GetClassLongPtrW(listView, GCLP_WNDPROC));
    if (!wndProc) {
        Wh_Log(L"No se encontró el procedimiento de la lista del escritorio");
        return false;
    }
    if (!Wh_SetFunctionHook(reinterpret_cast<void*>(wndProc),
                            reinterpret_cast<void*>(ListViewWndProc_Hook),
                            reinterpret_cast<void**>(&ListViewWndProc_Original))) {
        Wh_Log(L"No se pudo instalar el hook");
        return false;
    }
    if (applyNow) {
        Wh_ApplyHookOperations();
    }
    g_hooked = true;
    return true;
}

// Firma simple del estado de las barras de tareas para detectar cambios de alto.
LONG TaskbarSignature() {
    LONG signature = g_settings.extraMargin * 7919 + g_settings.manualHeight;
    if (HWND tray = FindWindowW(L"Shell_TrayWnd", nullptr)) {
        RECT rc;
        if (GetWindowRect(tray, &rc)) {
            signature = signature * 31 + (rc.bottom - rc.top);
            signature = signature * 31 + (rc.right - rc.left);
        }
    }
    return signature;
}

DWORD WINAPI WatcherThread(LPVOID) {
    LONG lastSignature = 0;
    for (;;) {
        HWND listView = FindDesktopListView();
        if (listView) {
            bool isNew = listView != g_listView.load();
            if (isNew) {
                EnterCriticalSection(&g_origLock);
                g_haveOrig = false;
                LeaveCriticalSection(&g_origLock);
                g_listView = listView;
            }
            if (HookListView(listView, true)) {
                LONG signature = TaskbarSignature();
                if (isNew || signature != lastSignature) {
                    lastSignature = signature;
                    Reapply(listView);
                }
            }
        }
        if (WaitForSingleObject(g_stopEvent, 2000) == WAIT_OBJECT_0) {
            break;
        }
    }
    return 0;
}

void LoadSettings() {
    g_settings.extraMargin = Wh_GetIntSetting(L"extraMargin");
    g_settings.manualHeight = Wh_GetIntSetting(L"manualHeight");
    if (g_settings.extraMargin < 0) {
        g_settings.extraMargin = 0;
    }
    if (g_settings.extraMargin > 300) {
        g_settings.extraMargin = 300;
    }
    if (g_settings.manualHeight < 0) {
        g_settings.manualHeight = 0;
    }
}

BOOL Wh_ModInit() {
    Wh_Log(L">");
    InitializeCriticalSection(&g_origLock);
    LoadSettings();

    if (HWND listView = FindDesktopListView()) {
        g_listView = listView;
        HookListView(listView, false);
    }
    return TRUE;
}

void Wh_ModAfterInit() {
    Wh_Log(L">");
    g_stopEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    g_thread = CreateThread(nullptr, 0, WatcherThread, nullptr, 0, nullptr);
}

void Wh_ModBeforeUninit() {
    Wh_Log(L">");
    if (g_thread) {
        SetEvent(g_stopEvent);
        WaitForSingleObject(g_thread, INFINITE);
        CloseHandle(g_thread);
        g_thread = nullptr;
    }
    if (g_stopEvent) {
        CloseHandle(g_stopEvent);
        g_stopEvent = nullptr;
    }

    // Deja el escritorio exactamente como lo quería Explorer.
    g_disabled = true;
    HWND listView = g_listView.load();
    if (listView && IsWindow(listView) && g_hooked) {
        RECT rects[kMaxWorkAreas];
        int count = 0;
        EnterCriticalSection(&g_origLock);
        bool haveOrig = g_haveOrig;
        count = g_origCount;
        memcpy(rects, g_origRects, sizeof(rects));
        LeaveCriticalSection(&g_origLock);
        if (haveOrig) {
            SendMessageW(listView, LVM_SETWORKAREAS, count,
                         count > 0 ? reinterpret_cast<LPARAM>(rects) : 0);
            ArrangeIfAuto(listView);
        }
    }
}

void Wh_ModUninit() {
    Wh_Log(L">");
    DeleteCriticalSection(&g_origLock);
}

void Wh_ModSettingsChanged() {
    Wh_Log(L">");
    LoadSettings();
    HWND listView = g_listView.load();
    if (listView && IsWindow(listView) && g_hooked) {
        Reapply(listView);
    }
}
