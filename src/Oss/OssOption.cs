namespace EarthBackground.Oss
{
    /// <summary>
    /// oss 配置项
    /// </summary>
    public class OssOption
    {
        /// <summary>
        /// 云厂商名称
        /// </summary>
        public string CloudName { get; set; } = string.Empty;

        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// api key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// api密钥
        /// </summary>
        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// 区域
        /// </summary>
        public string Zone { get; set; } = string.Empty;

        /// <summary>
        /// 桶
        /// </summary>
        public string Bucket { get; set; } = string.Empty;

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; }
    }
}
