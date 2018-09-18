namespace S22.Xmpp.Extensions.Socks5
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class ByteBuilder
    {
        private byte[] buffer = new byte[0x400];
        private int position = 0;

        public ByteBuilder Append(params byte[] values)
        {
            if ((this.position + values.Length) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            foreach (byte num in values)
            {
                this.buffer[this.position++] = num;
            }
            return this;
        }

        public ByteBuilder Append(short value, bool bigEndian = false)
        {
            int[] numArray2;
            if ((this.position + 2) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            int[] numArray = bigEndian ? (numArray2 = new int[2]) : (numArray2 = new int[2]);
            for (int i = 0; i < 2; i++)
            {
                this.buffer[this.position++] = (byte) ((value >> (numArray[i] * 8)) & 0xff);
            }
            return this;
        }

        public ByteBuilder Append(int value, bool bigEndian = false)
        {
            if ((this.position + 4) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            int[] numArray = bigEndian ? new int[] { 3, 2, 1, 0 } : new int[] { 0, 1, 2, 3 };
            for (int i = 0; i < 4; i++)
            {
                this.buffer[this.position++] = (byte) ((value >> (numArray[i] * 8)) & 0xff);
            }
            return this;
        }

        public ByteBuilder Append(long value, bool bigEndian = false)
        {
            if ((this.position + 8) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            int[] numArray = bigEndian ? new int[] { 7, 6, 5, 4, 3, 2, 1, 0 } : new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            for (int i = 0; i < 8; i++)
            {
                this.buffer[this.position++] = (byte) ((value >> (numArray[i] * 8)) & 0xffL);
            }
            return this;
        }

        public ByteBuilder Append(string value, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.ASCII;
            }
            byte[] bytes = encoding.GetBytes(value);
            if ((this.position + bytes.Length) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            foreach (byte num in bytes)
            {
                this.buffer[this.position++] = num;
            }
            return this;
        }

        public ByteBuilder Append(ushort value, bool bigEndian = false)
        {
            int[] numArray2;
            if ((this.position + 2) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            int[] numArray = bigEndian ? (numArray2 = new int[2]) : (numArray2 = new int[2]);
            for (int i = 0; i < 2; i++)
            {
                this.buffer[this.position++] = (byte) ((value >> (numArray[i] * 8)) & 0xff);
            }
            return this;
        }

        public ByteBuilder Append(uint value, bool bigEndian = false)
        {
            if ((this.position + 4) >= this.buffer.Length)
            {
                this.Resize(0x400);
            }
            int[] numArray = bigEndian ? new int[] { 3, 2, 1, 0 } : new int[] { 0, 1, 2, 3 };
            for (int i = 0; i < 4; i++)
            {
                this.buffer[this.position++] = (byte) ((value >> (numArray[i] * 8)) & 0xff);
            }
            return this;
        }

        public ByteBuilder Append(byte[] buffer, int offset, int count)
        {
            if ((this.position + count) >= buffer.Length)
            {
                this.Resize(0x400);
            }
            for (int i = 0; i < count; i++)
            {
                this.buffer[this.position++] = buffer[offset + i];
            }
            return this;
        }

        public void Clear()
        {
            this.buffer = new byte[0x400];
            this.position = 0;
        }

        private void Resize(int amount = 0x400)
        {
            byte[] destinationArray = new byte[this.buffer.Length + amount];
            Array.Copy(this.buffer, destinationArray, this.buffer.Length);
            this.buffer = destinationArray;
        }

        public byte[] ToArray()
        {
            byte[] destinationArray = new byte[this.position];
            Array.Copy(this.buffer, destinationArray, this.position);
            return destinationArray;
        }

        public int Length
        {
            get
            {
                return this.position;
            }
        }
    }
}

