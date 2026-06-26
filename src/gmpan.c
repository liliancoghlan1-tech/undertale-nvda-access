/*
 * gmpan.dll  -  tiny stereo-beep add-on so a 32-bit GameMaker game (Undertale)
 * can play a beep that is panned LEFT/RIGHT in the player's ears and pitched
 * HIGH/LOW.  Undertale's own audio engine has panning disabled (caster_set_panning
 * is an empty stub in this PC port), so we synthesize a short stereo sine wave
 * ourselves and play it through the Windows waveOut API.  This lets the dodge
 * assist point a blind player toward the safe spot: pan = sideways, pitch = up/down.
 *
 * Playback is NON-BLOCKING: a beep is queued and returns immediately, so it is
 * safe to call every frame from the game loop.  A small pool of wave buffers is
 * recycled as the OS finishes each beep.
 *
 * All exports use the C calling convention (cdecl) and return a double, so they
 * line up with GameMaker external_define(..., dll_cdecl, ty_real, ...).
 */
#include <windows.h>
#include <math.h>
#include <string.h>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

#define SR        44100                 /* sample rate (Hz)                  */
#define MAXMS     400                   /* longest single beep we allow (ms) */
#define MAXFRAMES (SR * MAXMS / 1000)
#define NBUF      8                      /* how many beeps can overlap        */

static HWAVEOUT g_wo = NULL;
static WAVEHDR  g_hdr[NBUF];
static short   *g_buf[NBUF] = { 0 };     /* one stereo PCM buffer per slot    */
static int      g_ready = 0;

/* Open the audio device.  Returns 1 on success, negative on failure. */
__declspec(dllexport) double gmpan_init(void)
{
    WAVEFORMATEX wf;
    int i;

    if (g_ready)
        return 1.0;

    memset(&wf, 0, sizeof(wf));
    wf.wFormatTag      = WAVE_FORMAT_PCM;
    wf.nChannels       = 2;                      /* stereo = we can pan       */
    wf.nSamplesPerSec  = SR;
    wf.wBitsPerSample  = 16;
    wf.nBlockAlign     = (wf.nChannels * wf.wBitsPerSample) / 8;   /* = 4      */
    wf.nAvgBytesPerSec = wf.nSamplesPerSec * wf.nBlockAlign;
    wf.cbSize          = 0;

    if (waveOutOpen(&g_wo, WAVE_MAPPER, &wf, 0, 0, CALLBACK_NULL) != MMSYSERR_NOERROR)
        return -1.0;

    for (i = 0; i < NBUF; i++)
    {
        g_buf[i] = (short *)HeapAlloc(GetProcessHeap(), 0,
                                      (size_t)MAXFRAMES * 2 * sizeof(short));
        if (g_buf[i] == NULL)
            return -2.0;
        memset(&g_hdr[i], 0, sizeof(WAVEHDR));   /* dwFlags 0 = slot is fresh */
    }
    g_ready = 1;
    return 1.0;
}

/* Find a reusable buffer slot, or -1 if every slot is still playing. */
static int find_free_slot(void)
{
    int i;
    for (i = 0; i < NBUF; i++)
    {
        DWORD f = g_hdr[i].dwFlags;
        if (f == 0)                              /* never used yet            */
            return i;
        if (f & WHDR_DONE)                        /* OS finished this beep     */
        {
            waveOutUnprepareHeader(g_wo, &g_hdr[i], sizeof(WAVEHDR));
            memset(&g_hdr[i], 0, sizeof(WAVEHDR));
            return i;
        }
    }
    return -1;
}

/*
 * Play one beep.
 *   pan  : -1.0 = full left  ..  0 = centre  ..  +1.0 = full right
 *   freq : tone frequency in Hz (e.g. 220 low, 880 high)
 *   ms   : length in milliseconds (clamped to MAXMS)
 *   gain : 0.0 .. 1.0 loudness
 * Returns 0 on success, negative if not ready / no free slot.
 */
__declspec(dllexport) double gmpan_beep(double pan, double freq, double ms, double gain)
{
    int slot, frames, fade, n;
    double lg, rg, ang;
    short *buf;

    if (!g_ready)
        return -1.0;

    slot = find_free_slot();
    if (slot < 0)
        return -2.0;                             /* all slots busy: drop it   */

    if (pan < -1.0) pan = -1.0;
    if (pan >  1.0) pan =  1.0;
    if (gain < 0.0) gain = 0.0;
    if (gain > 1.0) gain = 1.0;
    if (freq < 50.0)  freq = 50.0;
    if (ms < 10.0)  ms = 10.0;
    if (ms > MAXMS) ms = MAXMS;

    frames = (int)(SR * ms / 1000.0);
    if (frames > MAXFRAMES) frames = MAXFRAMES;
    fade = frames / 8;                           /* short fade = no click     */
    if (fade < 1) fade = 1;

    /* equal-power pan: centre stays as loud as the edges */
    ang = (pan + 1.0) * (M_PI / 4.0);            /* 0..PI/2                    */
    lg  = cos(ang) * gain;
    rg  = sin(ang) * gain;

    buf = g_buf[slot];
    for (n = 0; n < frames; n++)
    {
        double s   = sin(2.0 * M_PI * freq * (double)n / (double)SR);
        double env = 1.0;
        if (n < fade)                 env = (double)n / (double)fade;
        else if (n > frames - fade)   env = (double)(frames - n) / (double)fade;
        buf[2 * n]     = (short)(s * lg * env * 30000.0);   /* left  */
        buf[2 * n + 1] = (short)(s * rg * env * 30000.0);   /* right */
    }

    g_hdr[slot].lpData         = (LPSTR)buf;
    g_hdr[slot].dwBufferLength = (DWORD)(frames * 2 * sizeof(short));
    g_hdr[slot].dwFlags        = 0;
    if (waveOutPrepareHeader(g_wo, &g_hdr[slot], sizeof(WAVEHDR)) != MMSYSERR_NOERROR)
        return -3.0;
    if (waveOutWrite(g_wo, &g_hdr[slot], sizeof(WAVEHDR)) != MMSYSERR_NOERROR)
        return -4.0;
    return 0.0;
}

/* Close the audio device and free buffers. */
__declspec(dllexport) double gmpan_close(void)
{
    int i;
    if (!g_ready)
        return 0.0;
    waveOutReset(g_wo);
    for (i = 0; i < NBUF; i++)
    {
        if (g_hdr[i].dwFlags & WHDR_PREPARED)
            waveOutUnprepareHeader(g_wo, &g_hdr[i], sizeof(WAVEHDR));
        if (g_buf[i])
            HeapFree(GetProcessHeap(), 0, g_buf[i]);
        g_buf[i] = NULL;
    }
    waveOutClose(g_wo);
    g_wo = NULL;
    g_ready = 0;
    return 1.0;
}
