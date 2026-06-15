using System;
using System.Runtime.InteropServices;

namespace RemoteControl.Audio.NativeMethods
{
    internal partial class WavMethods
    {
        /// <summary>
        /// Closes the specified waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device. If the function succeeds, the handle is no longer valid after this call.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutClose(IntPtr hWaveOut);

        /// <summary>
        /// Queries a specified waveform device to determine its capabilities.
        /// </summary>
        /// <param name="hwo">Identifier of the waveform-audio output device. It can be either a device identifier or a Handle to an open waveform-audio output device.</param>
        /// <param name="pwoc">Pointer to a WAVEOUTCAPS structure to be filled with information about the capabilities of the device.</param>
        /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
        public static extern uint waveOutGetDevCaps(uint hwo,ref WAVEOUTCAPS pwoc,int cbwoc);

        /// <summary>
        /// Retrieves the number of waveform output devices present in the system.
        /// </summary>
        /// <returns>The number of devices indicates success. Zero indicates that no devices are present or that an error occurred.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutGetNumDevs();

        /// <summary>
        /// Retrieves the current playback position of the specified waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpInfo">Pointer to an MMTIME structure.</param>
        /// <param name="uSize">Size, in bytes, of the MMTIME structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutGetPosition(IntPtr hWaveOut,out int lpInfo,int uSize);

        /// <summary>
        /// Queries the current volume setting of a waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to an open waveform-audio output device. This parameter can also be a device identifier.</param>
        /// <param name="dwVolume">Pointer to a variable to be filled with the current volume setting.
        /// The low-order word of this location contains the left-channel volume setting, and the high-order
        /// word contains the right-channel setting. A value of 0xFFFF represents full volume, and a
        /// value of 0x0000 is silence.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutGetVolume(IntPtr hWaveOut,out int dwVolume);

        /// <summary>
        /// The waveOutOpen function opens the given waveform-audio output device for playback.
        /// </summary>
        /// <param name="hWaveOut">Pointer to a buffer that receives a handle identifying the open waveform-audio output device. Use the handle to identify the device when calling other waveform-audio output functions. This parameter might be NULL if the WAVE_FORMAT_QUERY flag is specified for fdwOpen.</param>
        /// <param name="uDeviceID">Identifier of the waveform-audio output device to open. It can be either a device identifier or a handle of an open waveform-audio input device.</param>
        /// <param name="lpFormat">Pointer to a WAVEFORMATEX structure that identifies the format of the waveform-audio data to be sent to the device. You can free this structure immediately after passing it to waveOutOpen.</param>
        /// <param name="dwCallback">Pointer to a fixed callback function, an event handle, a handle to a window, or the identifier of a thread to be called during waveform-audio playback to process messages related to the progress of the playback. If no callback function is required, this value can be zero.</param>
        /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
        /// <param name="dwFlags">Flags for opening the device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveOutOpen(out IntPtr hWaveOut,int uDeviceID,WAVEFORMATEX lpFormat,waveOutProc dwCallback,int dwInstance,int dwFlags);

        /// <summary>
        /// Pauses playback on a specified waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutPause(IntPtr hWaveOut);

        /// <summary>
        /// Prepares a waveform data block for playback.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the data block to be prepared. The buffer's base address must be aligned with the respect to the sample size.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveOutPrepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        /// <summary>
        /// Stops playback on a specified waveform output device and resets the current position to 0.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutReset(IntPtr hWaveOut);

        /// <summary>
        /// Restarts a paused waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutRestart(IntPtr hWaveOut);

        /// <summary>
        /// Sets the volume of a waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to an open waveform-audio output device. This parameter can also be a device identifier.</param>
        /// <param name="dwVolume">Specifies a new volume setting. The low-order word contains the left-channel
        /// volume setting, and the high-order word contains the right-channel setting. A value of 0xFFFF
        /// represents full volume, and a value of 0x0000 is silence.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveOutSetVolume(IntPtr hWaveOut,int dwVolume);

        /// <summary>
        /// Cleans up the preparation performed by waveOutPrepareHeader.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure identifying the data block to be cleaned up.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveOutUnprepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        /// <summary>
        /// Sends a data block to the specified waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure containing information about the data block.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveOutWrite(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);
    }
}
