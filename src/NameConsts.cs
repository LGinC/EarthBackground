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
        /// 向日葵
        /// </summary>
        public const string Himawari = "Himawari";

        /// <summary>
        /// GOES
        /// </summary>
        public const string Goes = "GOES";

        /// <summary>
        /// Geo-KOMPSAT
        /// </summary>
        public const string GeoKompsat = "GeoKompsat";

        /// <summary>
        /// Meteosat
        /// </summary>
        public const string Meteosat = "Meteosat";

        /// <summary>
        /// 风云4
        /// </summary>
        public const string Fy4 = "fy-4";

        /// <summary>
        /// 当前使用的风云4卫星
        /// </summary>
        public const string Fy4CurrentSatellite = "fy-4b";

        /// <summary>
        /// 抓取器名称列表
        /// </summary>
        public static readonly string[] CaptorNames = [Himawari, Goes, GeoKompsat, Meteosat, Fy4];

        /// <summary>
        /// 下载器名称列表
        /// </summary>
        public static readonly string[] DownloaderNames = [DirectDownload, Qiqiuyun, Cloudinary];

        public static string ImageIdPath => AppPaths.ImageIdPath;
    }
}
