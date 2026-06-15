using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    #region Delegates Implementation

    /// <summary>
    /// Represents the method that will handle the <b>WavRecorder.BufferFull</b> event.
    /// </summary>
    /// <param name="buffer">Recorded data.</param>
    public delegate void BufferFullHandler(byte[] buffer);

    #endregion

    /// <summary>
    /// This class implements streaming microphone wav data receiver.
    /// </summary>
    public partial class WaveIn
    {

        private WavInDevice      m_pInDevice     = null;
        private int              m_SamplesPerSec = 8000;
        private int              m_BitsPerSample = 8;
        private int              m_Channels      = 1;
        private int              m_BufferSize    = 400;
        private IntPtr           m_pWavDevHandle = IntPtr.Zero;
        private int              m_BlockSize     = 0;
        private List<BufferItem> m_pBuffers      = null;
        private waveInProc       m_pWaveInProc   = null;
        private bool             m_IsRecording   = false;
        private bool             m_IsDisposed    = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="outputDevice">Input device.</param>
        /// <param name="samplesPerSec">Sample rate, in samples per second (hertz). For PCM common values are 
        /// 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.</param>
        /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
        /// <param name="channels">Number of channels.</param>
        /// <param name="bufferSize">Specifies recording buffer size.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>outputDevice</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the aruments has invalid value.</exception>
        public WaveIn(WavInDevice device,int samplesPerSec,int bitsPerSample,int channels,int bufferSize)
        {
            if(device == null){
                throw new ArgumentNullException("device");
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

            m_pInDevice     = device;
            m_SamplesPerSec = samplesPerSec;
            m_BitsPerSample = bitsPerSample;
            m_Channels      = channels;
            m_BufferSize    = bufferSize;
            m_BlockSize     = m_Channels * (m_BitsPerSample / 8);
            m_pBuffers      = new List<BufferItem>();

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
            m_pWaveInProc = new waveInProc(this.OnWaveInProc);
            int result = WavMethods.waveInOpen(out m_pWavDevHandle,m_pInDevice.Index,format,m_pWaveInProc,0,WavConstants.CALLBACK_FUNCTION);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to open wav device, error: " + result.ToString() + ".");
            }

            EnsureBuffers();
        }
        
        /// <summary>
        /// Default destructor.
        /// </summary>
        ~WaveIn()
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

            // Release events.
            this.BufferFull = null;

            try{
                // If recording, we need to reset wav device first.
                WavMethods.waveInReset(m_pWavDevHandle);
                
                // If there are unprepared wav headers, we need to unprepare these.
                foreach(BufferItem item in m_pBuffers){
                    WavMethods.waveInUnprepareHeader(m_pWavDevHandle,item.HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(item.Header));
                    item.Dispose();
                }
                
                // Close input device.
                WavMethods.waveInClose(m_pWavDevHandle);

                m_pInDevice     = null;
                m_pWavDevHandle = IntPtr.Zero;
            }
            catch{                
            }
        }

        #endregion


        #region method Start

        /// <summary>
        /// Starts recording.
        /// </summary>
        public void Start()
        {
            if(m_IsRecording){
                return;
            }
            m_IsRecording = true;

            int result = WavMethods.waveInStart(m_pWavDevHandle);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to start wav device, error: " + result + ".");
            }
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stops recording.
        /// </summary>
        public void Stop()
        {
            if(!m_IsRecording){
                return;
            }
            m_IsRecording = false;
            
            int result = WavMethods.waveInStop(m_pWavDevHandle);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to stop wav device, error: " + result + ".");
            }
        }

        #endregion


    }
}
