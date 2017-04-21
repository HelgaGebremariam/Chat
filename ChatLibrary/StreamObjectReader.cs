using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace ChatLibrary
{
    public class StreamObjectReader
    {
        private Stream ioStream;

        public StreamObjectReader(Stream ioStream)
        {
            this.ioStream = ioStream;
        }

        private byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }

        public T ReadMessage<T>() where T : class
        {
            if (ioStream.CanRead == false)
                return null;
            int len = 0;
            len = ioStream.ReadByte() * 256;
            if (len < 0)
                return null;
            len += ioStream.ReadByte();
            if (len <= 0)
                return null;
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return ByteArrayToObject(inBuffer) as T;
        }

        public int WriteMessage<T>(T outputMessage)
        {
            if (outputMessage == null)
                return 0;
            byte[] outBuffer = ObjectToByteArray(outputMessage);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
