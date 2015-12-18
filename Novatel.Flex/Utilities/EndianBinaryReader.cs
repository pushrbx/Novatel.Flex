using System;
using System.IO;
using System.Text;

namespace Novatel.Flex.Utilities
{
    internal class EndianBinaryReader : IDisposable
    {
        // Fields
        private readonly byte[] m_buffer;
        private readonly char[] m_charBuffer;
        private readonly Decoder m_decoder;
        private readonly int m_minBytesPerChar;
        private bool m_disposed;

        // Methods
        public EndianBinaryReader(EndianBitConverter bitConverter, Stream stream)
            : this(bitConverter, stream, Encoding.UTF8)
        {
        }

        public EndianBinaryReader(EndianBitConverter bitConverter, Stream stream, Encoding encoding)
        {
            m_buffer = new byte[0x10];
            m_charBuffer = new char[1];
            if (bitConverter == null)
            {
                throw new ArgumentNullException("bitConverter");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream isn't writable", "stream");
            }
            BaseStream = stream;
            BitConverter = bitConverter;
            Encoding = encoding;
            m_decoder = encoding.GetDecoder();
            m_minBytesPerChar = 1;
            if (encoding is UnicodeEncoding)
            {
                m_minBytesPerChar = 2;
            }
        }

        // Properties
        public Stream BaseStream { get; private set; }
        public EndianBitConverter BitConverter { get; private set; }
        public Encoding Encoding { get; private set; }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                BaseStream.Dispose();
            }
        }

        private void CheckDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("EndianBinaryReader");
            }
        }

        public void Close()
        {
            Dispose();
        }

        public int Read()
        {
            if (Read(m_charBuffer, 0, 1) == 0)
            {
                return -1;
            }
            return m_charBuffer[0];
        }

        public int Read(byte[] buffer, int index, int count)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((count + index) > buffer.Length)
            {
                throw new ArgumentException(
                    "Not enough space in buffer for specified number of bytes starting at specified index");
            }
            var num = 0;
            while (count > 0)
            {
                var num2 = BaseStream.Read(buffer, index, count);
                if (num2 == 0)
                {
                    return num;
                }
                index += num2;
                num += num2;
                count -= num2;
            }
            return num;
        }

        public int Read(char[] data, int index, int count)
        {
            CheckDisposed();
            if (m_buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((count + index) > data.Length)
            {
                throw new ArgumentException(
                    "Not enough space in buffer for specified number of characters starting at specified index");
            }
            var num = 0;
            var flag = true;
            var buffer = m_buffer;
            if (buffer.Length < (count*m_minBytesPerChar))
            {
                buffer = new byte[0x1000];
            }
            while (num < count)
            {
                int length;
                if (flag)
                {
                    length = count*m_minBytesPerChar;
                    flag = false;
                }
                else
                {
                    length = (((count - num) - 1)*m_minBytesPerChar) + 1;
                }
                if (length > buffer.Length)
                {
                    length = buffer.Length;
                }
                var byteCount = TryReadInternal(buffer, length);
                if (byteCount == 0)
                {
                    return num;
                }
                var num4 = m_decoder.GetChars(buffer, 0, byteCount, data, index);
                num += num4;
                index += num4;
            }
            return num;
        }

        public int Read7BitEncodedInt()
        {
            CheckDisposed();
            var num = 0;
            for (var i = 0; i < 0x23; i += 7)
            {
                var num3 = BaseStream.ReadByte();
                if (num3 == -1)
                {
                    throw new EndOfStreamException();
                }
                num |= (num3 & 0x7f) << i;
                if ((num3 & 0x80) == 0)
                {
                    return num;
                }
            }
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }

        public int ReadBigEndian7BitEncodedInt()
        {
            CheckDisposed();
            var num = 0;
            for (var i = 0; i < 5; i++)
            {
                var num3 = BaseStream.ReadByte();
                if (num3 == -1)
                {
                    throw new EndOfStreamException();
                }
                num = (num << 7) | (num3 & 0x7f);
                if ((num3 & 0x80) == 0)
                {
                    return num;
                }
            }
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }

        public bool ReadBoolean()
        {
            ReadInternal(m_buffer, 1);
            return BitConverter.ToBoolean(m_buffer, 0);
        }

        public byte ReadByte()
        {
            ReadInternal(m_buffer, 1);
            return m_buffer[0];
        }

        public byte[] ReadBytes(int count)
        {
            int num2;
            CheckDisposed();
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            var buffer = new byte[count];
            for (var i = 0; i < count; i += num2)
            {
                num2 = BaseStream.Read(buffer, i, count - i);
                if (num2 == 0)
                {
                    var dst = new byte[i];
                    Buffer.BlockCopy(buffer, 0, dst, 0, i);
                    return dst;
                }
            }
            return buffer;
        }

        public byte[] ReadBytesOrThrow(int count)
        {
            var data = new byte[count];
            ReadInternal(data, count);
            return data;
        }

        public decimal ReadDecimal()
        {
            ReadInternal(m_buffer, 0x10);
            return BitConverter.ToDecimal(m_buffer, 0);
        }

        public double ReadDouble()
        {
            ReadInternal(m_buffer, 8);
            return BitConverter.ToDouble(m_buffer, 0);
        }

        public short ReadInt16()
        {
            ReadInternal(m_buffer, 2);
            return BitConverter.ToInt16(m_buffer, 0);
        }

        public int ReadInt32()
        {
            ReadInternal(m_buffer, 4);
            return BitConverter.ToInt32(m_buffer, 0);
        }

        public long ReadInt64()
        {
            ReadInternal(m_buffer, 8);
            return BitConverter.ToInt64(m_buffer, 0);
        }

        private void ReadInternal(byte[] data, int size)
        {
            int num2;
            CheckDisposed();
            for (var i = 0; i < size; i += num2)
            {
                num2 = BaseStream.Read(data, i, size - i);
                if (num2 == 0)
                {
                    throw new EndOfStreamException(string.Format(
                        "End of stream reached with {0} byte{1} left to read.", size - i, ((size - i) == 1) ? "s" : ""));
                }
            }
        }

        public sbyte ReadSByte()
        {
            ReadInternal(m_buffer, 1);
            return (sbyte) m_buffer[0];
        }

        public float ReadSingle()
        {
            ReadInternal(m_buffer, 4);
            return BitConverter.ToSingle(m_buffer, 0);
        }

        public string ReadString()
        {
            var size = Read7BitEncodedInt();
            var data = new byte[size];
            ReadInternal(data, size);
            return Encoding.GetString(data, 0, data.Length);
        }

        public ushort ReadUInt16()
        {
            ReadInternal(m_buffer, 2);
            return BitConverter.ToUInt16(m_buffer, 0);
        }

        public uint ReadUInt32()
        {
            ReadInternal(m_buffer, 4);
            return BitConverter.ToUInt32(m_buffer, 0);
        }

        public ulong ReadUInt64()
        {
            ReadInternal(m_buffer, 8);
            return BitConverter.ToUInt64(m_buffer, 0);
        }

        public void Seek(int offset, SeekOrigin origin)
        {
            CheckDisposed();
            BaseStream.Seek(offset, origin);
        }

        private int TryReadInternal(byte[] data, int size)
        {
            CheckDisposed();
            var offset = 0;
            while (offset < size)
            {
                var num2 = BaseStream.Read(data, offset, size - offset);
                if (num2 == 0)
                {
                    return offset;
                }
                offset += num2;
            }
            return offset;
        }
    }
}