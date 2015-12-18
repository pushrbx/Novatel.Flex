namespace Novatel.Flex.Utilities
{
    internal sealed class BigEndianBitConverter : EndianBitConverter
    {
        // Properties
        public override Endianness Endianness
        {
            get { return Endianness.BigEndian; }
        }

        // Methods
        protected override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
        {
            var num = (index + bytes) - 1;
            for (var i = 0; i < bytes; i++)
            {
                buffer[num - i] = (byte) (value & 0xffL);
                value = value >> 8;
            }
        }

        protected override long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
        {
            var num = 0L;
            for (var i = 0; i < bytesToConvert; i++)
            {
                num = (num << 8) | buffer[startIndex + i];
            }
            return num;
        }

        public override bool IsLittleEndian()
        {
            return false;
        }
    }
}