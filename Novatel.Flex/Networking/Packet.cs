using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novatel.Flex.Networking.Data;

namespace Novatel.Flex.Networking
{
    /// <summary>
    /// Thread safe packet buffer.
    /// </summary>
    /// <remarks>
    /// Based on http://www.novatel.com/assets/Documents/Manuals/om-20000129.pdf
    /// Page 26, Table 3.
    /// </remarks>
    /// todo: we should consider turning off the confirmation responses for commands
    internal class Packet
    {
        private readonly byte[] m_syncBytes = {0xAA, 0x44, 0x12};

        private PacketWriter m_writer;
        private PacketReader m_reader;
        private byte[] m_readerBytes;

        private readonly object m_lock;
        // determines is it a readonly or newly created packet
        private bool m_locked; 

        public Packet()
        {
            m_writer = new PacketWriter();
            m_reader = null;
            m_lock = new object();
            m_readerBytes = null;
            Format = PacketFormat.Binary;
        }

        public Packet(Packet rhs)
        {
            lock (rhs.m_lock)
            {
                m_lock = new object();
                m_locked = rhs.m_locked;

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
                }
            }
        }

        public Packet(byte[] bytes)
        {
            m_lock = new object();
            m_writer = new PacketWriter();
            m_writer.Write(bytes);
            m_reader = null;
            m_readerBytes = null;
            Format = PacketFormat.Binary;
        }

        public Packet(byte[] bytes, int offset, int length)
        {
            m_lock = new object();
            m_writer = new PacketWriter();
            m_writer.Write(bytes, offset, length);
            m_reader = null;
            m_readerBytes = null;
            Format = PacketFormat.Binary;
        }

        private void CheckIfLocked(string operation)
        {
            if (!m_locked)
            {
                throw new PacketException(string.Format("Cannot do '{0}' operation on an unlocked Packet.", operation));
            }
        }

        public ushort MessageId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// bit format: 1111 1111
        /// first 4 bits is the measurement source
        /// in the example on Page 40 we have: 0000 0010
        /// which means: measurement source: 0, format: ASCII, Response Type: Original
        /// </remarks>
        public sbyte MessageType { get; set; }

        public byte PortAddress { get; set; }

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
                return m_locked ? m_readerBytes : m_writer.GetBytes();
            }
        }

        public long SeekRead(long offset, SeekOrigin orgin)
        {
            lock (m_lock)
            {
                CheckIfLocked("SeekReed");
                return m_reader.BaseStream.Seek(offset, orgin);
            }
        }

        public int RemainingRead()
        {
            lock (m_lock)
            {
                CheckIfLocked("RemainingRead");
                return (int)(m_reader.BaseStream.Length - m_reader.BaseStream.Position);
            }
        }

        // todo finish here the write and read methods;;
    }
}
