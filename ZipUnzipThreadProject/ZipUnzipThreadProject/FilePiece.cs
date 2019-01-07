namespace ZipUnzipThreadProject
{
    /// <summary>
    /// кусочки файла
    /// </summary>
    public class FilePiece
    {
        public int Id { private set; get; }
        public byte[] Content { private set; get; }
        public int OutSize { private set; get; }

        public FilePiece(int mId, byte[] mContent, int outSize)
        {
            Id = mId;
            Content = mContent;
            OutSize = outSize;
        }
    }
}
