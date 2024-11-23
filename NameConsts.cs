namespace EarthBackground
{
    public class NameConsts
    {
        public const string DirectDownload = "DirectDownload";

        public const string Cloudinary = "Cloudinary";

        /// <summary>
        /// 七牛云
        /// </summary>
        public const string Qiqiuyun = "Qiniuyun";

        /// <summary>
        /// 向日葵8号
        /// </summary>
        public const string Himawari8 = "Himawari8";

        /// <summary>
        /// 风云4A
        /// </summary>
        public const string FY4 = "FY4A";

        /// <summary>
        /// 抓取器名称列表
        /// </summary>
        public readonly static string[] CaptorNames = [Himawari8, FY4];

        /// <summary>
        /// 下载器名称列表
        /// </summary>
        public readonly static string[] DownloaderNames = [DirectDownload, Qiqiuyun, Cloudinary];
    }
}
