using System;
using System.IO;
using System.IO.Compression;

namespace GZipLibrary
{
    /// <summary>
    /// упаковка/распаковка частей
    /// </summary>
    public class ProcessPacking
    {
        public static byte[] ProcessArchive(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes.Length))
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(bytes, 0, bytes.Length);
                }
                var compressedData = memoryStream.ToArray();
                //добавить длину блока
                var compressedLength = compressedData.Length;
                var bitLength = BitConverter.GetBytes(compressedLength);
                bitLength.CopyTo(compressedData, 4);
                return compressedData;
            }
        }
    }

    public class ProcessUnPacking
    {
        public static byte[] ProcessArchive(byte[] bytes, int outSize)
        {
            using (var gZipStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            {
                using (var memStream = new MemoryStream())
                {
                    //m_ProgressList[index] = pieceId;
                    var buffer = new byte[outSize];
                    int size = gZipStream.Read(buffer, 0, buffer.Length);
                    memStream.Write(buffer, 0, size);
                    var ms = memStream.ToArray();
                    return ms;
                }
            }
        }
    }
}
