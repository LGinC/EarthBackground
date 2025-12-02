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
        public const string Fy4 = "fy-4b";

        /// <summary>
        /// 抓取器名称列表
        /// </summary>
        public static readonly string[] CaptorNames = [Himawari8, Fy4];

        /// <summary>
        /// 下载器名称列表
        /// </summary>
        public static readonly string[] DownloaderNames = [DirectDownload, Qiqiuyun, Cloudinary];

        public const string ImageIdPath = "ImageId.txt";
    }
}
