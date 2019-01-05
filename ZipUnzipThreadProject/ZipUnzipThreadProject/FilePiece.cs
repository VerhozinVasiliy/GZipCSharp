namespace ZipUnzipThreadProject
{
    /// <summary>
    /// кусочки файла
    /// </summary>
    public class FilePiece
    {
        public int Id { private set; get; }
        public byte[] Content { private set; get; }

        public FilePiece(int mId, byte[] mContent)
        {
            Id = mId;
            Content = mContent;
        }
    }
}
