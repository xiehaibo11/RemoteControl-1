using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    public partial class WaveIn
    {
        #region Properties Implementation

        /// <summary>
        /// Gets all available input audio devices.
        /// </summary>
        public static WavInDevice[] Devices
        {
            get{
                List<WavInDevice> retVal = new List<WavInDevice>();
                // Get all available output devices and their info.                
                int devicesCount = WavMethods.waveInGetNumDevs();
                for(int i=0;i<devicesCount;i++){
                    WAVEOUTCAPS pwoc = new WAVEOUTCAPS();
                    if(WavMethods.waveInGetDevCaps((uint)i,ref pwoc,Marshal.SizeOf(pwoc)) == MMSYSERR.NOERROR){
                        retVal.Add(new WavInDevice(i,pwoc.szPname,pwoc.wChannels));
                    }
                }

                return retVal.ToArray();
            }
        }


        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets current input device.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public WavInDevice InputDevice
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_pInDevice; 
            }
        }

        /// <summary>
        /// Gets number of samples per second.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int SamplesPerSec
        {
            get{                 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_SamplesPerSec; 
            }
        }

        /// <summary>
        /// Gets number of buts per sample.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BitsPerSample
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }
                
                return m_BitsPerSample; 
            }
        }

        /// <summary>
        /// Gets number of channels.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int Channels
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }
                
                return m_Channels; 
            }
        }

        /// <summary>
        /// Gets recording buffer size.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BufferSize
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }
                
                return m_BufferSize; 
            }
        }

        // <summary>
        /// Gets one smaple block size in bytes.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BlockSize
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_BlockSize; 
            }
        }

        #endregion
        
        #region Events Implementation

        /// <summary>
        /// This event is raised when record buffer is full and application should process it.
        /// </summary>
        public event BufferFullHandler BufferFull = null;

        /// <summary>
        /// This method raises event <b>BufferFull</b> event.
        /// </summary>
        /// <param name="buffer">Receive buffer.</param>
        private void OnBufferFull(byte[] buffer)
        {
            if(this.BufferFull != null){
                this.BufferFull(buffer);
            }
        }

        #endregion
    }
}
