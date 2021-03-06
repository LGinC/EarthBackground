﻿namespace EarthBackground.Oss
{
    /// <summary>
    /// oss 配置项
    /// </summary>
    public class OssOption
    {
        /// <summary>
        /// 云厂商名称
        /// </summary>
        public string CloudName { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// api key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// api密钥
        /// </summary>
        public string ApiSecret { get; set; }

        /// <summary>
        /// 区域
        /// </summary>
        public string  Zone { get; set; }

        /// <summary>
        /// 桶
        /// </summary>
        public string Bucket { get; set; }

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; }
    }
}
