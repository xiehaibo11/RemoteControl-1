using System;
using System.Runtime.InteropServices;
using RemoteControl.Audio.NativeMethods;

namespace RemoteControl.Audio
{
    public partial class WaveIn
    {
        #region class BufferItem

        /// <summary>
        /// This class holds queued recording buffer.
        /// </summary>
        private class BufferItem
        {
            private GCHandle m_HeaderHandle;
            private GCHandle m_DataHandle;
            private int      m_DataSize = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="headerHandle">Header handle.</param>
            /// <param name="header">Wav header.</param>
            /// <param name="dataHandle">Wav header data handle.</param>
            /// <param name="dataSize">Data size in bytes.</param>
            public BufferItem(ref GCHandle headerHandle,ref GCHandle dataHandle,int dataSize)
            {
                m_HeaderHandle = headerHandle;
                m_DataHandle   = dataHandle;
                m_DataSize     = dataSize;
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                m_HeaderHandle.Free();
                m_DataHandle.Free();
            }

            #endregion


            #region Properties Implementation

            /// <summary>
            /// Gets header handle.
            /// </summary>
            public GCHandle HeaderHandle
            {
                get{ return m_HeaderHandle; }
            }

            /// <summary>
            /// Gets header.
            /// </summary>
            public WAVEHDR Header
            {
                get{ return (WAVEHDR)m_HeaderHandle.Target; }
            }

            /// <summary>
            /// Gets wav header data pointer handle.
            /// </summary>
            public GCHandle DataHandle
            {
                get{ return m_DataHandle; }
            }

            /// <summary>
            /// Gets wav header data.
            /// </summary>
            public byte[] Data
            {
                get{ return (byte[])m_DataHandle.Target; }
            }

            /// <summary>
            /// Gets wav header data size in bytes.
            /// </summary>
            public int DataSize
            {
                get{ return m_DataSize; }
            }

            #endregion

        }

        #endregion
    }
}
