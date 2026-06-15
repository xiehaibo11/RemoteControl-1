using System;
using System.Runtime.InteropServices;

namespace RemoteControl.Audio.NativeMethods
{
    internal partial class WavMethods
    {
        /// <summary>
        /// The waveInAddBuffer function sends an input buffer to the given waveform-audio input device. When the buffer is filled, the application is notified.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the buffer.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveInAddBuffer(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        /// <summary>
        /// Closes the specified waveform input device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device. If the function succeeds, the handle is no longer valid after this call.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveInClose(IntPtr hWaveOut);

        /// <summary>
        /// Queries a specified waveform device to determine its capabilities.
        /// </summary>
        /// <param name="hwo">Identifier of the waveform-audio input device. It can be either a device identifier or a Handle to an open waveform-audio output device.</param>
        /// <param name="pwoc">Pointer to a WAVEOUTCAPS structure to be filled with information about the capabilities of the device.</param>
        /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
        public static extern uint waveInGetDevCaps(uint hwo,ref WAVEOUTCAPS pwoc,int cbwoc);

        /// <summary>
        /// Get the waveInGetNumDevs function returns the number of waveform-audio input devices present in the system.
        /// </summary>
        /// <returns>Returns the waveInGetNumDevs function returns the number of waveform-audio input devices present in the system.
        /// </returns>
        [DllImport("winmm.dll")]
        public static extern int waveInGetNumDevs();

        /// <summary>
        /// The waveInOpen function opens the given waveform-audio input device for recording.
        /// </summary>
        /// <param name="hWaveOut">Pointer to a buffer that receives a handle identifying the open waveform-audio input device.</param>
        /// <param name="uDeviceID">Identifier of the waveform-audio input device to open. It can be either a device identifier or a handle of an open waveform-audio input device. You can use the following flag instead of a device identifier.</param>
        /// <param name="lpFormat">Pointer to a WAVEFORMATEX structure that identifies the desired format for recording waveform-audio data. You can free this structure immediately after waveInOpen returns.</param>
        /// <param name="dwCallback">Pointer to a fixed callback function, an event handle, a handle to a window,
        /// or the identifier of a thread to be called during waveform-audio recording to process messages related
        /// to the progress of recording. If no callback function is required, this value can be zero.
        /// For more information on the callback function, see waveInProc.</param>
        /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
        /// <param name="dwFlags">Flags for opening the device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveInOpen(out IntPtr hWaveOut,int uDeviceID,WAVEFORMATEX lpFormat,waveInProc dwCallback,int dwInstance,int dwFlags);

        /// <summary>
        /// Prepares a waveform data block for recording.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the data block to be prepared.
        /// The buffer's base address must be aligned with the respect to the sample size.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveInPrepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        /// <summary>
        /// Stops input on a specified waveform output device and resets the current position to 0.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveInReset(IntPtr hWaveOut);

        /// <summary>
        /// Starts input on the given waveform-audio input device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveInStart(IntPtr hWaveOut);

        /// <summary>
        /// Stops input on the given waveform-audio input device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		public static extern int waveInStop(IntPtr hWaveOut);

        /// <summary>
        /// Cleans up the preparation performed by waveInPrepareHeader.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure identifying the data block to be cleaned up.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		public static extern int waveInUnprepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);
    }
}
