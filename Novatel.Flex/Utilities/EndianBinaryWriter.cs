using System;
using System.IO;
using System.Text;

namespace Novatel.Flex.Utilities
{
    internal class EndianBinaryWriter : IDisposable
    {
        // Fields
        private readonly byte[] m_buffer;
        private readonly char[] m_charBuffer;
        private bool m_disposed;
        // Methods
        public EndianBinaryWriter(EndianBitConverter bitConverter, Stream stream)
            : this(bitConverter, stream, Encoding.UTF8)
        {
        }

        public EndianBinaryWriter(EndianBitConverter bitConverter, Stream stream, Encoding encoding)
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
            if (!stream.CanWrite)
            {
                throw new ArgumentException("Stream isn't writable", "stream");
            }
            BaseStream = stream;
            BitConverter = bitConverter;
            Encoding = encoding;
        }

        // Properties
        public Stream BaseStream { get; private set; }
        public EndianBitConverter BitConverter { get; private set; }
        public Encoding Encoding { get; private set; }

        public void Dispose()
        {
            if (!m_disposed)
            {
                Flush();
                m_disposed = true;
                BaseStream.Dispose();
            }
        }

        private void CheckDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("EndianBinaryWriter");
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void Flush()
        {
            CheckDisposed();
            BaseStream.Flush();
        }

        public void Seek(int offset, SeekOrigin origin)
        {
            CheckDisposed();
            BaseStream.Seek(offset, origin);
        }

        public void Write(bool value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 1);
        }

        public void Write(byte value)
        {
            m_buffer[0] = value;
            WriteInternal(m_buffer, 1);
        }

        public void Write(char value)
        {
            m_charBuffer[0] = value;
            Write(m_charBuffer);
        }

        public void Write(decimal value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 0x10);
        }

        public void Write(double value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 8);
        }

        public void Write(short value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 2);
        }

        public void Write(int value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 4);
        }

        public void Write(long value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 8);
        }

        public void Write(sbyte value)
        {
            m_buffer[0] = (byte) value;
            WriteInternal(m_buffer, 1);
        }

        public void Write(float value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 4);
        }

        public void Write(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            CheckDisposed();
            var bytes = Encoding.GetBytes(value);
            Write7BitEncodedInt(bytes.Length);
            WriteInternal(bytes, bytes.Length);
        }

        public void Write(ushort value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 2);
        }

        public void Write(uint value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 4);
        }

        public void Write(ulong value)
        {
            BitConverter.CopyBytes(value, m_buffer, 0);
            WriteInternal(m_buffer, 8);
        }

        public void Write(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            WriteInternal(value, value.Length);
        }

        public void Write(char[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            CheckDisposed();
            var bytes = Encoding.GetBytes(value, 0, value.Length);
            WriteInternal(bytes, bytes.Length);
        }

        public void Write(byte[] value, int offset, int count)
        {
            CheckDisposed();
            BaseStream.Write(value, offset, count);
        }

        public void Write7BitEncodedInt(int value)
        {
            CheckDisposed();
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", "Value must be greater than or equal to 0.");
            }
            var count = 0;
            while (value >= 0x80)
            {
                m_buffer[count++] = (byte) ((value & 0x7f) | 0x80);
                value = value >> 7;
                count++;
            }
            m_buffer[count++] = (byte) value;
            BaseStream.Write(m_buffer, 0, count);
        }

        private void WriteInternal(byte[] bytes, int length)
        {
            CheckDisposed();
            BaseStream.Write(bytes, 0, length);
        }
    }
}