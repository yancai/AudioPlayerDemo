using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace AudioPlay
{
    public class CusBufferedWaveProvider : IWaveProvider
     {
        private WaveFormat _waveFormat;
        private Queue<AudioBufferCus> _audioBufferQueue;

        internal Queue<AudioBufferCus> BufferQueue { get { return _audioBufferQueue; } } 

        //public event EventHandler PlayPositionChanged;

        public CusBufferedWaveProvider(WaveFormat format)
        {
            _waveFormat = format;
            _audioBufferQueue = new Queue<AudioBufferCus>();
            MaxQueuedBuffers = 100;
        }

        public int MaxQueuedBuffers { get; set; }

        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        
        public void AddSamples(byte[] buffer, int offset, int count, TimeSpan currentTime)
        {
            byte[] nbuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, nbuffer, 0, count);
            lock (_audioBufferQueue)
            {
                //if (_audioBufferQueue.Count >= MaxQueuedBuffers)
                //{
                //    throw new InvalidOperationException("Too many queued buffers");
                //}
                _audioBufferQueue.Enqueue(new AudioBufferCus(buffer, currentTime));
            }
        }


        public int BuffersCount { get { return _audioBufferQueue.Count; } }


        public int Read(byte[] buffer, int offset, int count)
        {
            int readCount = 0;
            while (readCount < count)
            {
                int requiredCount = count - readCount;
                AudioBufferCus audioBufferCus = null;
                lock (_audioBufferQueue)
                {
                    if (_audioBufferQueue.Count > 0)
                    {
                        //return 0;
                        audioBufferCus = _audioBufferQueue.Peek();
                    }
                    //audioBufferCus = _audioBufferQueue.Peek();
                }

                if (audioBufferCus == null)
                {
                    // 用空数据填充剩余部分
                    for (int n = 0; n < readCount; n++)
                    {
                        buffer[offset + n] = 0;
                    }
                    readCount += requiredCount;
                }
                else
                {
                    int needCount = audioBufferCus.Buffer.Count() - audioBufferCus.Position;

                    //// TODO:时间改变
                    //if (PlayPositionChanged != null)
                    //{
                    //    PlayPositionChanged(this, new BufferedPlayEventArgs(audioBufferCus.CurrentTime));
                    //}

                    if (needCount <= requiredCount)
                    {
                        Buffer.BlockCopy(audioBufferCus.Buffer, audioBufferCus.Position, buffer, offset + readCount, needCount);
                        readCount += needCount;

                        lock (_audioBufferQueue)
                        {
                            _audioBufferQueue.Dequeue();
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(audioBufferCus.Buffer, audioBufferCus.Position, buffer, offset + readCount, requiredCount);
                        audioBufferCus.Position += requiredCount;
                        readCount += requiredCount;
                    }
                }
            }
            return readCount;
        }
        
    }

    internal class AudioBufferCus
    {
        public byte[] Buffer { get; private set; }

        public int Position { get; set; }

        public TimeSpan CurrentTime { get; set; }

        public AudioBufferCus(byte[] newBuffer, TimeSpan currentTime)
        {
            Buffer = newBuffer;
            CurrentTime = currentTime;
            Position = 0;
        }
    }
}
