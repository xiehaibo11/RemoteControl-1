using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    /// <summary>
    /// This class implements streaming wav data player.
    /// </summary>
    public partial class WaveOut : IDisposable
    {

        private WavOutDevice    m_pOutDevice    = null;
        private int             m_SamplesPerSec = 8000;
        private int             m_BitsPerSample = 16;
        private int             m_Channels      = 1;
        private int             m_MinBuffer     = 1200;
        private IntPtr          m_pWavDevHandle = IntPtr.Zero;
        private int             m_BlockSize     = 0;
        private int             m_BytesBuffered = 0;
        private bool            m_IsPaused      = false;
        private List<PlayItem>  m_pPlayItems    = null;
        private waveOutProc     m_pWaveOutProc  = null;
        private bool            m_IsDisposed    = false;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="outputDevice">Output device.</param>
        /// <param name="samplesPerSec">Sample rate, in samples per second (hertz). For PCM common values are 
        /// 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.</param>
        /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
        /// <param name="channels">Number of channels.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>outputDevice</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the aruments has invalid value.</exception>
        public WaveOut(WavOutDevice outputDevice,int samplesPerSec,int bitsPerSample,int channels)
        {
            if(outputDevice == null){
                throw new ArgumentNullException("outputDevice");
            }
            if(samplesPerSec < 8000){
                throw new ArgumentException("Argument 'samplesPerSec' value must be >= 8000.");
            }
            if(bitsPerSample < 8){
                throw new ArgumentException("Argument 'bitsPerSample' value must be >= 8.");
            }
            if(channels < 1){
                throw new ArgumentException("Argument 'channels' value must be >= 1.");
            }

            m_pOutDevice    = outputDevice;
            m_SamplesPerSec = samplesPerSec;
            m_BitsPerSample = bitsPerSample;
            m_Channels      = channels;
            m_BlockSize     = m_Channels * (m_BitsPerSample / 8);
            m_pPlayItems    = new List<PlayItem>();
            
            // Try to open wav device.            
            WAVEFORMATEX format = new WAVEFORMATEX();
            format.wFormatTag      = WavFormat.PCM;
            format.nChannels       = (ushort)m_Channels;
            format.nSamplesPerSec  = (uint)samplesPerSec;                        
            format.nAvgBytesPerSec = (uint)(m_SamplesPerSec * m_Channels * (m_BitsPerSample / 8));
            format.nBlockAlign     = (ushort)m_BlockSize;
            format.wBitsPerSample  = (ushort)m_BitsPerSample;
            format.cbSize          = 0; 
            // We must delegate reference, otherwise GC will collect it.
            m_pWaveOutProc = new waveOutProc(this.OnWaveOutProc);
            int result = WavMethods.waveOutOpen(out m_pWavDevHandle,m_pOutDevice.Index,format,m_pWaveOutProc,0,WavConstants.CALLBACK_FUNCTION);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to open wav device, error: " + result.ToString() + ".");
            }
        }

        /// <summary>
        /// Default destructor.
        /// </summary>
        ~WaveOut()
        {
            Dispose();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            try{
                // If playing, we need to reset wav device first.
                WavMethods.waveOutReset(m_pWavDevHandle);

                // If there are unprepared wav headers, we need to unprepare these.
                foreach(PlayItem item in m_pPlayItems){
                    WavMethods.waveOutUnprepareHeader(m_pWavDevHandle,item.HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(item.Header));
                    item.Dispose();
                }
                
                // Close output device.
                WavMethods.waveOutClose(m_pWavDevHandle);

                m_pOutDevice    = null;
                m_pWavDevHandle = IntPtr.Zero;
                m_pPlayItems    = null;
                m_pWaveOutProc  = null;
            }
            catch{                
            }
        }

        #endregion


    }
}
