namespace Novatel.Flex.Utilities
{
    internal sealed class LittleEndianBitConverter : EndianBitConverter
    {
        // Properties
        public override Endianness Endianness
        {
            get { return Endianness.LittleEndian; }
        }

        // Methods
        protected override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
        {
            for (var i = 0; i < bytes; i++)
            {
                buffer[i + index] = (byte) (value & 0xffL);
                value = value >> 8;
            }
        }

        protected override long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
        {
            var num = 0L;
            for (var i = 0; i < bytesToConvert; i++)
            {
                num = (num << 8) | buffer[((startIndex + bytesToConvert) - 1) - i];
            }
            return num;
        }

        public override bool IsLittleEndian()
        {
            return true;
        }
    }
}