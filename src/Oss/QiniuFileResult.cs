
namespace EarthBackground.Oss
{
    public class QiniuFileResult
    {
        public QiniuFile[] Items { get; set; } = [];
    }

    public class QiniuFile
    {
        public string Key { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public long Fsize { get; set; }

        public string MimeType { get; set; } = string.Empty;

        public long PutTime { get; set; }

        public string Md5 { get; set; } = string.Empty;

        public int Type { get; set; }

        public int Status { get; set; }
    }
}
