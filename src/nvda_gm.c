/*
 * nvda_gm.dll  -  tiny bridge so a 32-bit GameMaker game (Undertale) can speak
 * through NVDA.  GameMaker's external_call hands us a plain (UTF-8/char*) string;
 * NVDA's nvdaController_speakText wants a wide (UTF-16 wchar_t*) string.  This
 * just translates and forwards.  nvdaControllerClient32.dll is loaded at runtime
 * (it sits in the game folder), so we need no import library at build time.
 *
 * All exports use the C calling convention (cdecl) and return a double, so they
 * line up with GameMaker external_define(..., dll_cdecl, ty_real, ...).
 */
#include <windows.h>
#include <stdlib.h>

/* NVDA's exported functions are __stdcall on 32-bit Windows. */
typedef unsigned long (__stdcall *speak_fn)(const wchar_t *);
typedef unsigned long (__stdcall *cancel_fn)(void);
typedef unsigned long (__stdcall *running_fn)(void);
typedef unsigned long (__stdcall *braille_fn)(const wchar_t *);

static HMODULE  g_dll    = NULL;
static speak_fn   p_speak   = NULL;
static cancel_fn  p_cancel  = NULL;
static running_fn p_running = NULL;
static braille_fn p_braille = NULL;   /* optional: shows text on a braille display */

/* Load nvdaControllerClient32.dll and resolve the functions we need.
 * Pass the full dll path, or "" to just load by name from the game folder.
 * Returns 1 on success, negative on failure. */
__declspec(dllexport) double gmnvda_init(const char *dllpath)
{
    if (g_dll == NULL)
    {
        if (dllpath != NULL && dllpath[0] != '\0')
            g_dll = LoadLibraryA(dllpath);
        if (g_dll == NULL)
            g_dll = LoadLibraryA("nvdaControllerClient32.dll");
    }
    if (g_dll == NULL)
        return -1.0;

    p_speak   = (speak_fn)  GetProcAddress(g_dll, "nvdaController_speakText");
    p_cancel  = (cancel_fn) GetProcAddress(g_dll, "nvdaController_cancelSpeech");
    p_running = (running_fn)GetProcAddress(g_dll, "nvdaController_testIfRunning");
    p_braille = (braille_fn)GetProcAddress(g_dll, "nvdaController_brailleMessage");

    if (p_speak == NULL || p_cancel == NULL)
        return -2.0;
    return 1.0;
}

/* Speak a UTF-8 string through NVDA (interrupts current speech via cancel first). */
__declspec(dllexport) double gmnvda_speak(const char *utf8)
{
    int wlen;
    wchar_t  stackbuf[2048];
    wchar_t *w = stackbuf;
    int heap = 0;
    unsigned long r;

    if (p_speak == NULL)
        return -1.0;
    if (utf8 == NULL)
        return -2.0;

    wlen = MultiByteToWideChar(CP_UTF8, 0, utf8, -1, NULL, 0);
    if (wlen <= 0)
        return -3.0;
    if (wlen > 2048)
    {
        w = (wchar_t *)malloc((size_t)wlen * sizeof(wchar_t));
        if (w == NULL)
            return -4.0;
        heap = 1;
    }
    MultiByteToWideChar(CP_UTF8, 0, utf8, -1, w, wlen);

    if (p_cancel != NULL)
        p_cancel();           /* interrupt: new line replaces the old */
    if (p_braille != NULL)
        p_braille(w);         /* mirror the same line to a braille display */
    r = p_speak(w);

    if (heap)
        free(w);
    return (double)r;         /* 0 == success from NVDA */
}

/* Speak without interrupting (queue-ish: no cancel first). */
__declspec(dllexport) double gmnvda_speak_queue(const char *utf8)
{
    int wlen;
    wchar_t  stackbuf[2048];
    wchar_t *w = stackbuf;
    int heap = 0;
    unsigned long r;

    if (p_speak == NULL || utf8 == NULL)
        return -1.0;
    wlen = MultiByteToWideChar(CP_UTF8, 0, utf8, -1, NULL, 0);
    if (wlen <= 0)
        return -3.0;
    if (wlen > 2048)
    {
        w = (wchar_t *)malloc((size_t)wlen * sizeof(wchar_t));
        if (w == NULL)
            return -4.0;
        heap = 1;
    }
    MultiByteToWideChar(CP_UTF8, 0, utf8, -1, w, wlen);
    if (p_braille != NULL)
        p_braille(w);         /* mirror the same line to a braille display */
    r = p_speak(w);
    if (heap)
        free(w);
    return (double)r;
}

/* Send a UTF-8 string to a braille display only (no speech). Standalone helper;
 * the speak functions already braille automatically, so this is for future use. */
__declspec(dllexport) double gmnvda_braille(const char *utf8)
{
    int wlen;
    wchar_t  stackbuf[2048];
    wchar_t *w = stackbuf;
    int heap = 0;
    unsigned long r;

    if (p_braille == NULL)
        return -1.0;
    if (utf8 == NULL)
        return -2.0;
    wlen = MultiByteToWideChar(CP_UTF8, 0, utf8, -1, NULL, 0);
    if (wlen <= 0)
        return -3.0;
    if (wlen > 2048)
    {
        w = (wchar_t *)malloc((size_t)wlen * sizeof(wchar_t));
        if (w == NULL)
            return -4.0;
        heap = 1;
    }
    MultiByteToWideChar(CP_UTF8, 0, utf8, -1, w, wlen);
    r = p_braille(w);
    if (heap)
        free(w);
    return (double)r;
}

/* Stop current speech. */
__declspec(dllexport) double gmnvda_cancel(void)
{
    if (p_cancel == NULL)
        return -1.0;
    return (double)p_cancel();
}

/* 1 if NVDA is running, 0 if not, -1 if unavailable. */
__declspec(dllexport) double gmnvda_running(void)
{
    if (p_running == NULL)
        return -1.0;
    return (p_running() == 0) ? 1.0 : 0.0;
}
