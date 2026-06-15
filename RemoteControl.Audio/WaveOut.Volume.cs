using System;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    public partial class WaveOut
    {
        #region method GetVolume

        /// <summary>
        /// Gets audio output volume.
        /// </summary>
        /// <param name="left">Left channel volume level.</param>
        /// <param name="right">Right channel volume level.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void GetVolume(ref ushort left, ref ushort right)
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException("WaveOut");
            }

            int volume = 0;
            WavMethods.waveOutGetVolume(m_pWavDevHandle, out volume);

            left = (ushort)(volume & 0x0000ffff);
            right = (ushort)(volume >> 16);
        }

        #endregion

        #region method SetVolume

        /// <summary>
        /// Sets audio output volume.
        /// </summary>
        /// <param name="left">Left channel volume level.</param>
        /// <param name="right">Right channel volume level.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void SetVolume(ushort left, ushort right)
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException("WaveOut");
            }

            WavMethods.waveOutSetVolume(m_pWavDevHandle, (right << 16 | left & 0xFFFF));
        }

        #endregion
    }
}
