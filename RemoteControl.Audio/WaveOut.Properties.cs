using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    public partial class WaveOut
    {
        #region Properties Implementation

        /// <summary>
        /// Gets all available output audio devices.
        /// </summary>
        public static WavOutDevice[] Devices
        {
            get{
                List<WavOutDevice> retVal = new List<WavOutDevice>();
                // Get all available output devices and their info.
                int devicesCount = WavMethods.waveOutGetNumDevs();
                for(int i=0;i<devicesCount;i++){
                    WAVEOUTCAPS pwoc = new WAVEOUTCAPS();
                    if(WavMethods.waveOutGetDevCaps((uint)i,ref pwoc,Marshal.SizeOf(pwoc)) == MMSYSERR.NOERROR){
                        retVal.Add(new WavOutDevice(i,pwoc.szPname,pwoc.wChannels));
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
        /// Gets current output device.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public WavOutDevice OutputDevice
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_pOutDevice; 
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
                    throw new ObjectDisposedException("WaveOut");
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
                    throw new ObjectDisposedException("WaveOut");
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
                    throw new ObjectDisposedException("WaveOut");
                }
                
                return m_Channels; 
            }
        }

        /// <summary>
        /// Gets one smaple block size in bytes.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public int BlockSize
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WaveOut");
                }

                return m_BlockSize; 
            }
        }

        /// <summary>
        /// Gets if wav player is currently playing something.
        /// </summary>
        /// <exception cref="">Is raised when this object is disposed and this property is accessed.</exception>
        public bool IsPlaying
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WaveOut");
                }
                
                if(m_pPlayItems.Count > 0){
                    return true;
                }
                else{
                    return false;
                }
            }
        }

        #endregion
    }
}
