﻿using System;
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
        private readonly Stream _ioStream;

        public StreamObjectReader(Stream ioStream)
        {
            this._ioStream = ioStream;
        }

        private static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static object ByteArrayToObject(byte[] arrBytes)
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
            if (_ioStream.CanRead == false)
                return null;
            var len = 0;
            len = _ioStream.ReadByte() * 256;
            if (len < 0)
                return null;
            len += _ioStream.ReadByte();
            if (len <= 0)
                return null;
            byte[] inBuffer = new byte[len];
            _ioStream.Read(inBuffer, 0, len);

            return ByteArrayToObject(inBuffer) as T;
        }

        public int WriteMessage<T>(T outputMessage)
        {
            if (outputMessage == null)
                return 0;
            var outBuffer = ObjectToByteArray(outputMessage);
            var len = outBuffer.Length;
            if (len > ushort.MaxValue)
            {
                len = (int)ushort.MaxValue;
            }
            _ioStream.WriteByte((byte)(len / 256));
            _ioStream.WriteByte((byte)(len & 255));
            _ioStream.Write(outBuffer, 0, len);
            _ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
