﻿using System;
using System.Collections.Generic;
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

        private string encodedSign(byte[] data)
        {
#if WINDOWS_UWP
            var hma = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            var skBuffer = CryptographicBuffer.ConvertStringToBinary(mac.SecretKey, BinaryStringEncoding.Utf8);
            var hmacKey = hma.CreateKey(skBuffer);
            var dataBuffer = CryptographicBuffer.CreateFromByteArray(data);
            var signBuffer = CryptographicEngine.Sign(hmacKey, dataBuffer);
            byte[] digest;
            CryptographicBuffer.CopyToByteArray(signBuffer, out digest);
#else
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(mac.SecretKey));
            byte[] digest = hmac.ComputeHash(data);
#endif
            return QiniuBase64.UrlSafeBase64Encode(digest);
        }

        private string encodedSign(string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            return encodedSign(data);
        }

        /// <summary>
        /// 签名-字节数据
        /// </summary>
        /// <param name="data">待签名的数据</param>
        /// <returns></returns>
        public string Sign(byte[] data)
        {
            return string.Format("{0}:{1}", mac.AccessKey, encodedSign(data));
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
            return string.Format("{0}:{1}:{2}", mac.AccessKey, encodedSign(sstr), sstr);
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
        public string SignRequest(string url, byte[] body)
        {
            Uri u = new Uri(url);
            string pathAndQuery = u.PathAndQuery;
            byte[] pathAndQueryBytes = Encoding.UTF8.GetBytes(pathAndQuery);

            using (MemoryStream buffer = new MemoryStream())
            {
                buffer.Write(pathAndQueryBytes, 0, pathAndQueryBytes.Length);
                buffer.WriteByte((byte)'\n');
                if (body != null && body.Length > 0)
                {
                    buffer.Write(body, 0, body.Length);
                }
#if WINDOWS_UWP
                var hma = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
                var skBuffer = CryptographicBuffer.ConvertStringToBinary(mac.SecretKey, BinaryStringEncoding.Utf8);
                var hmacKey = hma.CreateKey(skBuffer);
                var dataBuffer = CryptographicBuffer.CreateFromByteArray(buffer.ToArray());
                var signBuffer = CryptographicEngine.Sign(hmacKey, dataBuffer);
                byte[] digest;
                CryptographicBuffer.CopyToByteArray(signBuffer, out digest);
#else
                HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(mac.SecretKey));
                byte[] digest = hmac.ComputeHash(buffer.ToArray());
#endif
                string digestBase64 = QiniuBase64.UrlSafeBase64Encode(digest);
                return string.Format("{0}:{1}", mac.AccessKey, digestBase64);
            }
        }

        /// <summary>
        /// HTTP请求签名
        /// </summary>
        /// <param name="url">请求目标的URL</param>
        /// <param name="body">请求的主体数据</param>
        /// <returns></returns>
        public string SignRequest(string url, string body)
        {
            byte[] data = Encoding.UTF8.GetBytes(body);
            return SignRequest(url, data);
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


    /// <summary>
    /// 字符串处理工具
    /// </summary>
    public class StringHelper
    {
        /// <summary>
        /// URL编码
        /// </summary>
        /// <param name="text">源字符串</param>
        /// <returns>URL编码字符串</returns>
        public static string UrlEncode(string text)
        {
            return Uri.EscapeDataString(text);
        }

        /// <summary>
        /// URL键值对编码
        /// </summary>
        /// <param name="values">键值对</param>
        /// <returns>URL编码的键值对数据</returns>
        public static string UrlFormEncode(Dictionary<string, string> values)
        {
            StringBuilder urlValuesBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> kvp in values)
            {
                urlValuesBuilder.AppendFormat("{0}={1}&", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value));
            }
            string encodedStr = urlValuesBuilder.ToString();
            return encodedStr.Substring(0, encodedStr.Length - 1);
        }
    }
}
