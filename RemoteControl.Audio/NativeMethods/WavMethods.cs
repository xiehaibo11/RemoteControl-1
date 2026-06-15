using System;
using System.Runtime.InteropServices;

namespace RemoteControl.Audio.NativeMethods
{
    /// <summary>
    /// The waveOutProc function is the callback function used with the waveform-audio output device.
    /// </summary>
    /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
    /// <param name="uMsg">Waveform-audio output message.</param>
    /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
    /// <param name="dwParam1">Message parameter.</param>
    /// <param name="dwParam2">Message parameter.</param>
    internal delegate void waveOutProc(IntPtr hdrvr,int uMsg,int dwUser,int dwParam1,int dwParam2);

    /// <summary>
    /// The waveInProc function is the callback function used with the waveform-audio input device.
    /// </summary>
    /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
    /// <param name="uMsg">Waveform-audio input message.</param>
    /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
    /// <param name="dwParam1">Message parameter.</param>
    /// <param name="dwParam2">Message parameter.</param>
    internal delegate void waveInProc(IntPtr hdrvr,int uMsg,int dwUser,int dwParam1,int dwParam2);

    /// <summary>
    /// This class provides windows wav methods.
    /// </summary>
    internal partial class WavMethods
    {
    }
}
