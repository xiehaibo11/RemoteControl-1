using System;
using System.Runtime.InteropServices;
using System.Threading;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    public partial class WaveOut
    {
        #region method OnWaveOutProc

        /// <summary>
        /// This method is called when wav device generates some event.
        /// </summary>
        /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
        /// <param name="uMsg">Waveform-audio output message.</param>
        /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
        /// <param name="dwParam1">Message parameter.</param>
        /// <param name="dwParam2">Message parameter.</param>
        private void OnWaveOutProc(IntPtr hdrvr,int uMsg,int dwUser,int dwParam1,int dwParam2)
        {
            // NOTE: MSDN warns, we may not call any wav related methods here.

            try{
                if(uMsg == WavConstants.MM_WOM_DONE){
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCleanUpFirstBlock));
                }
            }
            catch{
            }
        }

        #endregion

        #region method OnCleanUpFirstBlock

        /// <summary>
        /// Cleans up the first data block in play queue.
        /// </summary>
        /// <param name="state">User data.</param>
        private void OnCleanUpFirstBlock(object state)
        {
            try{
                lock(m_pPlayItems){
                    PlayItem item = m_pPlayItems[0];
                    WavMethods.waveOutUnprepareHeader(m_pWavDevHandle,item.HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(item.Header));
                    m_pPlayItems.Remove(item);
                    m_BytesBuffered -= item.DataSize;
                    item.Dispose();
                }
            }
            catch{
            }
        }

        #endregion

        #region method Play

        /// <summary>
        /// Plays specified audio data bytes. If player is currently playing, data will be queued for playing.
        /// </summary>
        /// <param name="audioData">Audio data. Data boundary must n * BlockSize.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to play form the specified offset.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>audioData</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when <b>audioData</b> is with invalid length.</exception>
        public void Play(byte[] audioData,int offset,int count)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("WaveOut");
            }
            if(audioData == null){
                throw new ArgumentNullException("audioData");
            }
            if((count % m_BlockSize) != 0){
                throw new ArgumentException("Audio data is not n * BlockSize.");
            }

            //--- Queue specified audio block for play. --------------------------------------------------------
            byte[]   data       = new byte[count];
            Array.Copy(audioData,offset,data,0,count);
            GCHandle dataHandle = GCHandle.Alloc(data,GCHandleType.Pinned);
//            m_BytesBuffered += data.Length;

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
            result = WavMethods.waveOutPrepareHeader(m_pWavDevHandle,headerHandle.AddrOfPinnedObject(),Marshal.SizeOf(wavHeader));
            if(result == MMSYSERR.NOERROR){
                PlayItem item = new PlayItem(ref headerHandle,ref dataHandle,data.Length);
                m_pPlayItems.Add(item);

                // We ran out of minimum buffer, we must pause playing while min buffer filled.
                if(m_BytesBuffered < 1000){
                    if(!m_IsPaused){
                        WavMethods.waveOutPause(m_pWavDevHandle);
                        m_IsPaused = true;
                    }
                    //File.AppendAllText("aaaa.txt","Begin buffer\r\n");
                }
                // Buffering completed,we may resume playing.
                else if(m_IsPaused && m_BytesBuffered > m_MinBuffer){
                    WavMethods.waveOutRestart(m_pWavDevHandle);
                    m_IsPaused = false;
                    //File.AppendAllText("aaaa.txt","end buffer: " + m_BytesBuffered + "\r\n");
                }
                /*
                // TODO: If we ran out of minimum buffer, we must pause playing while min buffer filled.
                if(m_BytesBuffered < m_MinBuffer){
                    if(!m_IsPaused){
                        WavMethods.waveOutPause(m_pWavDevHandle);
                        m_IsPaused = true;
                    }
                }
                else if(m_IsPaused){
                    WavMethods.waveOutRestart(m_pWavDevHandle);
                }*/

                m_BytesBuffered += data.Length;

                result = WavMethods.waveOutWrite(m_pWavDevHandle,headerHandle.AddrOfPinnedObject(),Marshal.SizeOf(wavHeader));
            }
            else{
                dataHandle.Free();
                headerHandle.Free();
            }
            //--------------------------------------------------------------------------------------------------
        }

        #endregion
    }
}
