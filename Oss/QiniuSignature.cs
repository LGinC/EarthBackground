using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EarthBackground.Oss
{
    public class QiniuSignature
    {
        private readonly Mac mac;
        public QiniuSignature(Mac mac)
        {
            this.mac = mac;
        }

        private string EncodedSign(byte[] data)
        {
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(mac.SecretKey));
            byte[] digest = hmac.ComputeHash(data);
            return QiniuBase64.UrlSafeBase64Encode(digest);
        }

        private string EncodedSign(string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            return EncodedSign(data);
        }

        /// <summary>
        /// 签名-字节数据
        /// </summary>
        /// <param name="data">待签名的数据</param>
        /// <returns></returns>
        public string Sign(byte[] data)
        {
            return string.Format("{0}:{1}", mac.AccessKey, EncodedSign(data));
        }

        /// <summary>
        /// 签名-字符串数据
        /// </summary>
        /// <param name="str">待签名的数据</param>
        /// <returns></returns>
        public string Sign(string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            return Sign(data);
        }

        /// <summary>
        /// 附带数据的签名
        /// </summary>
        /// <param name="data">待签名的数据</param>
        /// <returns></returns>
        public string SignWithData(byte[] data)
        {
            string sstr = QiniuBase64.UrlSafeBase64Encode(data);
            return $"{mac.AccessKey}:{EncodedSign(sstr)}:{sstr}";
        }

        /// <summary>
        /// 附带数据的签名
        /// </summary>
        /// <param name="str">待签名的数据</param>
        /// <returns>签名结果</returns>
        public string SignWithData(string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            return SignWithData(data);
        }

        /// <summary>
        /// HTTP请求签名
        /// </summary>
        /// <param name="url">请求目标的URL</param>
        /// <param name="body">请求的主体数据</param>
        /// <returns></returns>
        public string SignRequest(string httpMethod, string url, string contentType = null, byte[] body = null)
        {
            Uri u = new Uri(url);
            string signingStr = $"{httpMethod} {u.PathAndQuery}\nHost: {u.Host}\n";
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                signingStr = $"{signingStr}Content-Type: {contentType}\n";
            }
            byte[] bytes = Encoding.UTF8.GetBytes(signingStr);
            using MemoryStream buffer = new MemoryStream();
            buffer.Write(bytes, 0, bytes.Length);
            buffer.WriteByte((byte)'\n');
            if (body != null && body.Length > 0)
            {
                buffer.Write(body, 0, body.Length);
            }
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(mac.SecretKey));
            byte[] digest = hmac.ComputeHash(buffer.ToArray());
            string digestBase64 = QiniuBase64.UrlSafeBase64Encode(digest);
            return $"{mac.AccessKey}:{digestBase64}";
        }

        /// <summary>
        /// HTTP请求签名
        /// </summary>
        /// <param name="url">请求目标的URL</param>
        /// <param name="body">请求的主体数据</param>
        /// <returns></returns>
        public string SignRequest(string httpMethod, string url, string contentType = null, string body = null)
        {
            byte[] data = body != null ? Encoding.UTF8.GetBytes(body) : null;
            return SignRequest(httpMethod, url, contentType, data);
        }
    }


    /// <summary>
    /// 账户访问控制(密钥)
    /// </summary>
    public class Mac
    {
        /// <summary>
        /// 密钥-AccessKey
        /// </summary>
        public string AccessKey { set; get; }

        /// <summary>
        /// 密钥-SecretKey
        /// </summary>
        public string SecretKey { set; get; }

        /// <summary>
        /// 初始化密钥AK/SK
        /// </summary>
        /// <param name="accessKey">AccessKey</param>
        /// <param name="secretKey">SecretKey</param>
        public Mac(string accessKey, string secretKey)
        {
            AccessKey = accessKey;
            SecretKey = secretKey;
        }
    }
}
