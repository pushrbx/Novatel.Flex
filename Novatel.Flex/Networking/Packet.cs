using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Novatel.Flex.Networking.Data;

namespace Novatel.Flex.Networking
{
    /// <summary>
    ///     Thread safe packet buffer.
    /// </summary>
    /// <remarks>
    ///     Based on http://www.novatel.com/assets/Documents/Manuals/om-20000129.pdf
    ///     Page 26, Table 3.
    ///     Also automatically builds the header.
    /// </remarks>
    /// todo: we should consider turning off the confirmation responses for commands
    internal sealed class Packet
    {
        private readonly object m_lock;

        private readonly List<string> m_parsed = new List<string>();
        private readonly byte[] m_syncBytes = {0xAA, 0x44, 0x12};
        // determines is it a readonly or newly created packet
        private bool m_locked;
        private PacketReader m_reader;
        private byte[] m_readerBytes;

        private PacketWriter m_writer;

        /// <summary>
        /// Constructor for a request packet
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="portIdentifier"></param>
        public Packet(ushort opcode, byte portIdentifier)
        {
            m_writer = new PacketWriter();
            m_reader = null;
            m_lock = new object();
            m_readerBytes = null;
            Format = PacketFormat.Binary;
            //m_writer.Write(m_syncBytes);
            MessageId = opcode;
            PortAddress = portIdentifier;
            Sequence = 0;
            MessageType = 0x2;
        }

        /// <summary>
        /// Constructor for a request packet
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="portIdentifier"></param>
        public Packet(LogType opcode, byte portIdentifier) 
            : this((ushort)opcode, portIdentifier)
        {
        }

        public Packet(Packet rhs)
        {
            lock (rhs.m_lock)
            {
                m_lock = new object();
                m_locked = rhs.m_locked;
                MessageId = rhs.MessageId;
                PortAddress = rhs.PortAddress;

                if (!m_locked)
                {
                    m_writer = new PacketWriter();
                    m_reader = null;
                    m_readerBytes = null;
                    m_writer.Write(rhs.m_writer.GetBytes());
                    // in bits: 0000 0000, meaning: measurement source: 0, format: binary, response: original
                    MessageType = 0;
                    Format = PacketFormat.Binary;
                }
                else
                {
                    m_writer = null;
                    m_readerBytes = rhs.m_readerBytes;
                    m_reader = new PacketReader(m_readerBytes);
                    IsIncoming = true;
                }
            }
        }

        public Packet(ushort opcode, byte[] bytes, byte portIdentifier)
        {
            m_lock = new object();
            m_writer = new PacketWriter();
            m_writer.Write(bytes);
            m_reader = null;
            m_readerBytes = null;
            Format = PacketFormat.Binary;
            MessageId = opcode;
            PortAddress = portIdentifier;
            Sequence = 0;
            MessageType = 0x2;
        }

        public Packet(ushort opcode, byte[] bytes, int offset, int length, byte portIdentifier)
        {
            m_lock = new object();
            m_writer = new PacketWriter();
            m_writer.Write(bytes, offset, length);
            m_reader = null;
            m_readerBytes = null;
            Format = PacketFormat.Binary;
            MessageId = opcode;
            PortAddress = portIdentifier;
            Sequence = 0;
            MessageType = 0x2;
        }

        public ushort MessageId { get; private set; }

        /// <summary>
        /// </summary>
        /// <remarks>
        ///     bit format: 1111 1111
        ///     first 4 bits is the measurement source
        ///     in the example on Page 40 we have: 0000 0010
        ///     which means: measurement source: 0, format: ASCII, Response Type: Original
        /// </remarks>
        public sbyte MessageType { get; set; }

        public byte PortAddress { get; private set; }

        public ushort Sequence { get; set; }

        private byte IdleTime { get; set; }

        public bool IsIncoming { get; set; }

        public PacketFormat Format { get; private set; }

        internal PortIndentifier PortType
        {
            get
            {
                try
                {
                    var p = (PortIndentifier) PortAddress;
                    return p;
                }
                catch (Exception)
                {
                    return PortIndentifier.NoPorts;
                }
            }
        }

        public List<string> ParsedBytes
        {
            get { return m_parsed; }
        }

        private void CheckIfUnlocked(string operation)
        {
            if (!m_locked)
            {
                throw new PacketException(string.Format("Cannot do '{0}' operation on an unlocked Packet.", operation));
            }
        }

        private void CheckIfLocked(string operation)
        {
            if (m_locked)
            {
                throw new PacketException(string.Format("Cannot do '{0}' operation on a locked Packet.", operation));
            }
        }

        public void Lock()
        {
            lock (m_lock)
            {
                if (!m_locked)
                {
                    m_readerBytes = m_writer.GetBytes();
                    m_reader = new PacketReader(m_readerBytes);
                    m_writer.Close();
                    m_writer = null;
                    m_locked = true;
                }
            }
        }

        public byte[] GetBytes()
        {
            lock (m_lock)
            {
                var bytes = m_locked ? m_readerBytes : m_writer.GetBytes();
                if (!m_locked)
                {
                    m_writer.Seek(0, SeekOrigin.Begin);
                    var headerBytes = GetHeaderBytes(8192);
                    m_writer.Write(m_syncBytes);
                    m_writer.Write((byte)headerBytes.Length);
                    m_writer.Write(headerBytes);
                    m_writer.Write(bytes);
                    bytes = m_writer.GetBytes();
                }
                return bytes;
            }
        }

        private byte[] GetHeaderBytes(ushort messageLength)
        {
            using (var header = new PacketWriter())
            {
                header.Write(MessageId);
                // message type
                header.Write(MessageType);
                // port address
                header.Write(PortAddress);
                header.Write(messageLength);
                header.Write(Sequence);
                header.Write(IdleTime);
                // TimeStatus
                header.Write((byte)0); // use the ondevicetime
                // Week
                header.Write((ushort)0); // use the ondevice time.
                // Miliseconds
                header.Write((uint)0); // use the ondevice time.
                // Receiver status (gets ignored)
                header.Write((uint)0);
                // Reserved
                header.Write((ushort)21842);
                // ReceiverVersion
                header.Write((ushort)0);

                return header.GetBytes();
            }
        }

        public long SeekRead(long offset, SeekOrigin orgin)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("SeekReed");
                return m_reader.BaseStream.Seek(offset, orgin);
            }
        }

        public int RemainingRead()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("RemainingRead");
                return (int) (m_reader.BaseStream.Length - m_reader.BaseStream.Position);
            }
        }

        // todo finish here the write and read methods;;
        public byte ReadUInt8()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var b = m_reader.ReadByte();
                m_parsed.Add(b.ToString("X2"));
                return b;
            }
        }

        public sbyte ReadInt8()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var b = m_reader.ReadSByte();
                m_parsed.Add(b.ToString("X2"));
                return b;
            }
        }

        public ushort ReadUInt16()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadUInt16();
                m_parsed.Add(r.ToString("X4"));
                return r;
            }
        }

        public short ReadInt16()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadInt16();
                m_parsed.Add(r.ToString("X4"));
                return r;
            }
        }

        public uint ReadUInt32()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadUInt32();
                m_parsed.Add(r.ToString("X8"));
                return r;
            }
        }

        public int ReadInt32()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadInt32();
                m_parsed.Add(r.ToString("X8"));
                return r;
            }
        }

        public ulong ReadUInt64()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadUInt64();
                m_parsed.Add(r.ToString("X16"));
                return r;
            }
        }

        public long ReadInt64()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadInt64();
                m_parsed.Add(r.ToString("X16"));
                return r;
            }
        }

        public float ReadSingle()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadSingle();
                var b = BitConverter.GetBytes(r);
                var sb = new StringBuilder();
                foreach (var by in b)
                {
                    sb.Append(by.ToString("X"));
                }
                m_parsed.Add(sb.ToString());
                return r;
            }
        }

        public double ReadDouble()
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");
                var r = m_reader.ReadDouble();
                m_parsed.Add(r.ToString("X8"));
                return r;
            }
        }

        public string ReadAscii()
        {
            return ReadAscii(1252);
        }

        public string ReadAscii(int codepage)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var length = m_reader.ReadUInt16();
                var bytes = m_reader.ReadBytes(length);
                var s = Encoding.GetEncoding(codepage).GetString(bytes);
                m_parsed.Add(s);
                return s;
            }
        }

        public byte[] ReadUInt8Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new byte[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadByte();
                }
                for (var i = 0; i < count; i++)
                {
                    m_parsed.Add(values[i].ToString("X2"));
                }
                return values;
            }
        }

        public sbyte[] ReadInt8Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new sbyte[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadSByte();
                }

                return values;
            }
        }

        public ushort[] ReadUInt16Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new ushort[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadUInt16();
                }
                return values;
            }
        }

        public short[] ReadInt16Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new short[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadInt16();
                }
                return values;
            }
        }

        public uint[] ReadUInt32Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new uint[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadUInt32();
                }
                return values;
            }
        }

        public int[] ReadInt32Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new int[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadInt32();
                }
                return values;
            }
        }

        public ulong[] ReadUInt64Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new ulong[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadUInt64();
                }
                return values;
            }
        }

        public long[] ReadInt64Array(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new long[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadInt64();
                }
                return values;
            }
        }

        public float[] ReadSingleArray(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new float[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadSingle();
                }
                return values;
            }
        }

        public double[] ReadDoubleArray(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new double[count];
                for (var x = 0; x < count; ++x)
                {
                    values[x] = m_reader.ReadDouble();
                }
                return values;
            }
        }

        public string[] ReadAsciiArray(int count)
        {
            return ReadAsciiArray(1252, count);
        }

        public string[] ReadAsciiArray(int codepage, int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new string[count];
                for (var x = 0; x < count; ++x)
                {
                    var length = m_reader.ReadUInt16();
                    var bytes = m_reader.ReadBytes(length);
                    values[x] = Encoding.UTF7.GetString(bytes);
                }
                return values;
            }
        }

        public string[] ReadUnicodeArray(int count)
        {
            lock (m_lock)
            {
                CheckIfUnlocked("Read");

                var values = new string[count];
                for (var x = 0; x < count; ++x)
                {
                    var length = m_reader.ReadUInt16();
                    var bytes = m_reader.ReadBytes(length*2);
                    values[x] = Encoding.Unicode.GetString(bytes);
                }
                return values;
            }
        }

        public long SeekWrite(long offset, SeekOrigin orgin)
        {
            lock (m_lock)
            {
                CheckIfLocked("SeekWrite");
                return m_writer.BaseStream.Seek(offset, orgin);
            }
        }

        public void WriteUInt8(byte value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteInt8(sbyte value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteUInt16(ushort value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteInt16(short value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteUInt32(uint value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteInt32(int value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteUInt64(ulong value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteInt64(long value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteSingle(float value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteDouble(double value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(value);
            }
        }

        public void WriteAscii(string value)
        {
            WriteAscii(value, 1252);
        }

        public void WriteAscii(string value, int codePage)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");

                var codepageBytes = Encoding.GetEncoding(codePage).GetBytes(value);
                var utf7Value = Encoding.UTF7.GetString(codepageBytes);
                var bytes = Encoding.Default.GetBytes(utf7Value);

                //m_writer.Write((ushort)bytes.Length);
                m_writer.Write(bytes);
            }
        }

        public void WriteUInt8(object value)
        {
            lock (m_lock)
            {
                if (m_locked)
                {
                    throw new Exception("Cannot Write to a locked Packet.");
                }
                m_writer.Write((byte) (Convert.ToUInt64(value) & 0xFF));
            }
        }

        public void WriteInt8(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write((sbyte) (Convert.ToInt64(value) & 0xFF));
            }
        }

        public void WriteUInt16(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write((ushort) (Convert.ToUInt64(value) & 0xFFFF));
            }
        }

        public void WriteInt16(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write((ushort) (Convert.ToInt64(value) & 0xFFFF));
            }
        }

        public void WriteUInt32(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write((uint) (Convert.ToUInt64(value) & 0xFFFFFFFF));
            }
        }

        public void WriteInt32(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write((int) (Convert.ToInt64(value) & 0xFFFFFFFF));
            }
        }

        public void WriteUInt64(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(Convert.ToUInt64(value));
            }
        }

        public void WriteInt64(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(Convert.ToInt64(value));
            }
        }

        public void WriteSingle(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(Convert.ToSingle(value));
            }
        }

        public void WriteDouble(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                m_writer.Write(Convert.ToDouble(value));
            }
        }

        public void WriteAscii(object value)
        {
            WriteAscii(value, 1252);
        }

        public void WriteAscii(object value, int codePage)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");

                var codepage_bytes = Encoding.GetEncoding(codePage).GetBytes(value.ToString());
                var utf7_value = Encoding.UTF7.GetString(codepage_bytes);
                var bytes = Encoding.Default.GetBytes(utf7_value);

                m_writer.Write((ushort) bytes.Length);
                m_writer.Write(bytes);
            }
        }

        public void WriteUnicode(object value)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");

                var bytes = Encoding.Unicode.GetBytes(value.ToString());

                m_writer.Write((ushort) value.ToString().Length);
                m_writer.Write(bytes);
            }
        }

        public void WriteUInt8Array(byte[] values)
        {
            CheckIfLocked("Write");
            m_writer.Write(values);
        }

        public void WriteUInt8Array(byte[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteUInt16Array(ushort[] values)
        {
            WriteUInt16Array(values, 0, values.Length);
        }

        public void WriteUInt16Array(ushort[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteInt16Array(short[] values)
        {
            WriteInt16Array(values, 0, values.Length);
        }

        public void WriteInt16Array(short[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteUInt32Array(uint[] values)
        {
            WriteUInt32Array(values, 0, values.Length);
        }

        public void WriteUInt32Array(uint[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteInt32Array(int[] values)
        {
            WriteInt32Array(values, 0, values.Length);
        }

        public void WriteInt32Array(int[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteUInt64Array(ulong[] values)
        {
            WriteUInt64Array(values, 0, values.Length);
        }

        public void WriteUInt64Array(ulong[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteInt64Array(long[] values)
        {
            WriteInt64Array(values, 0, values.Length);
        }

        public void WriteInt64Array(long[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteSingleArray(float[] values)
        {
            WriteSingleArray(values, 0, values.Length);
        }

        public void WriteSingleArray(float[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteDoubleArray(double[] values)
        {
            WriteDoubleArray(values, 0, values.Length);
        }

        public void WriteDoubleArray(double[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    m_writer.Write(values[x]);
                }
            }
        }

        public void WriteAsciiArray(string[] values, int codepage)
        {
            WriteAsciiArray(values, 0, values.Length, codepage);
        }

        public void WriteAsciiArray(string[] values, int index, int count, int codepage)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteAscii(values[x], codepage);
                }
            }
        }

        public void WriteAsciiArray(string[] values)
        {
            WriteAsciiArray(values, 0, values.Length, 1252);
        }

        public void WriteAsciiArray(string[] values, int index, int count)
        {
            WriteAsciiArray(values, index, count, 1252);
        }

        public void WriteUnicodeArray(string[] values)
        {
            WriteUnicodeArray(values, 0, values.Length);
        }

        public void WriteUnicodeArray(string[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteUnicode(values[x]);
                }
            }
        }

        public void WriteUInt8Array(object[] values)
        {
            WriteUInt8Array(values, 0, values.Length);
        }

        public void WriteUInt8Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteUInt8(values[x]);
                }
            }
        }

        public void WriteInt8Array(object[] values)
        {
            WriteInt8Array(values, 0, values.Length);
        }

        public void WriteInt8Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteInt8(values[x]);
                }
            }
        }

        public void WriteUInt16Array(object[] values)
        {
            WriteUInt16Array(values, 0, values.Length);
        }

        public void WriteUInt16Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteUInt16(values[x]);
                }
            }
        }

        public void WriteInt16Array(object[] values)
        {
            WriteInt16Array(values, 0, values.Length);
        }

        public void WriteInt16Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteInt16(values[x]);
                }
            }
        }

        public void WriteUInt32Array(object[] values)
        {
            WriteUInt32Array(values, 0, values.Length);
        }

        public void WriteUInt32Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteUInt32(values[x]);
                }
            }
        }

        public void WriteInt32Array(object[] values)
        {
            WriteInt32Array(values, 0, values.Length);
        }

        public void WriteInt32Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteInt32(values[x]);
                }
            }
        }

        public void WriteUInt64Array(object[] values)
        {
            WriteUInt64Array(values, 0, values.Length);
        }

        public void WriteUInt64Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteUInt64(values[x]);
                }
            }
        }

        public void WriteInt64Array(object[] values)
        {
            WriteInt64Array(values, 0, values.Length);
        }

        public void WriteInt64Array(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteInt64(values[x]);
                }
            }
        }

        public void WriteSingleArray(object[] values)
        {
            WriteSingleArray(values, 0, values.Length);
        }

        public void WriteSingleArray(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteSingle(values[x]);
                }
            }
        }

        public void WriteDoubleArray(object[] values)
        {
            WriteDoubleArray(values, 0, values.Length);
        }

        public void WriteDoubleArray(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteDouble(values[x]);
                }
            }
        }

        public void WriteAsciiArray(object[] values, int codepage)
        {
            WriteAsciiArray(values, 0, values.Length, codepage);
        }

        public void WriteAsciiArray(object[] values, int index, int count, int codepage)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteAscii(values[x].ToString(), codepage);
                }
            }
        }

        public void WriteAsciiArray(object[] values)
        {
            WriteAsciiArray(values, 0, values.Length, 1252);
        }

        public void WriteAsciiArray(object[] values, int index, int count)
        {
            WriteAsciiArray(values, index, count, 1252);
        }

        public void WriteUnicodeArray(object[] values)
        {
            WriteUnicodeArray(values, 0, values.Length);
        }

        public void WriteUnicodeArray(object[] values, int index, int count)
        {
            lock (m_lock)
            {
                CheckIfLocked("Write");
                for (var x = index; x < index + count; ++x)
                {
                    WriteUnicode(values[x].ToString());
                }
            }
        }
    }
}