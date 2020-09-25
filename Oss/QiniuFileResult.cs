namespace EarthBackground.Oss
{
    public class QiniuFileResult
    {
        public QiniuFile[] items { get; set; }
    }

    public class QiniuFile
    {
        public string key { get; set; }

        public string hash { get; set; }

        public long fsize { get; set; }

        public string mimeType { get; set; }

        public long putTime { get; set; }

        public string md5 { get; set; }

        public int type { get; set; }

        public int status { get; set; }
    }
}
