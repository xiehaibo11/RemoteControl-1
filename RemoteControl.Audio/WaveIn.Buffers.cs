using System;
using System.Runtime.InteropServices;
using System.Threading;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    public partial class WaveIn
    {
        #region method OnWaveInProc

        /// <summary>
        /// This method is called when wav device generates some event.
        /// </summary>
        /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
        /// <param name="uMsg">Waveform-audio input message.</param>
        /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
        /// <param name="dwParam1">Message parameter.</param>
        /// <param name="dwParam2">Message parameter.</param>
        private void OnWaveInProc(IntPtr hdrvr,int uMsg,int dwUser,int dwParam1,int dwParam2)
        {
            // NOTE: MSDN warns, we may not call any wav related methods here.

            try{
                if(uMsg == WavConstants.MM_WIM_DATA){
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.ProcessFirstBuffer));
                }
            }
            catch{
            }
        }

        #endregion

        #region method ProcessFirstBuffer

        /// <summary>
        /// Processes first first filled buffer in queue and disposes it if done.
        /// </summary>
        /// <param name="state">User data.</param>
        private void ProcessFirstBuffer(object state)
        {
            try{
                lock(m_pBuffers){
                    BufferItem item = m_pBuffers[0];

                    // Raise BufferFull event.
                    OnBufferFull(item.Data);

                    // Clean up.
                    WavMethods.waveInUnprepareHeader(m_pWavDevHandle,item.HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(item.Header));
                    m_pBuffers.Remove(item);
                    item.Dispose();
                }

                EnsureBuffers();
            }
            catch{
            }
        }

        #endregion

        #region method EnsureBuffers

        /// <summary>
        /// Fills recording buffers.
        /// </summary>
        private void EnsureBuffers()
        {
            // We keep 3 x buffer.
            lock(m_pBuffers){
                while(m_pBuffers.Count < 3){
                    byte[]   data       = new byte[m_BufferSize];
                    GCHandle dataHandle = GCHandle.Alloc(data,GCHandleType.Pinned);

                    WAVEHDR wavHeader = new WAVEHDR();
                    wavHeader.lpData          = dataHandle.AddrOfPinnedObject();
                    wavHeader.dwBufferLength  = (uint)data.Length;
                    wavHeader.dwBytesRecorded = 0;
                    wavHeader.dwUser          = IntPtr.Zero;
                    wavHeader.dwFlags         = 0;
                    wavHeader.dwLoops         = 0;
                    wavHeader.lpNext          = IntPtr.Zero;
                    wavHeader.reserved        = 0;
                    GCHandle headerHandle = GCHandle.Alloc(wavHeader,GCHandleType.Pinned);
                    int result = 0;
                    result = WavMethods.waveInPrepareHeader(m_pWavDevHandle,headerHandle.AddrOfPinnedObject(),Marshal.SizeOf(wavHeader));
                    if(result == MMSYSERR.NOERROR){
                        m_pBuffers.Add(new BufferItem(ref headerHandle,ref dataHandle,m_BufferSize));

                        result = WavMethods.waveInAddBuffer(m_pWavDevHandle,headerHandle.AddrOfPinnedObject(),Marshal.SizeOf(wavHeader));
                        if(result != MMSYSERR.NOERROR){
                            throw new Exception("Error adding wave in buffer, error: " + result + ".");
                        }
                    }
                }
            }
        }

        #endregion
    }
}
