﻿using System;
using System.Collections.Generic;

namespace EarthBackground
{
    /// <summary>
    /// 抓取配置项
    /// </summary>
    public class CaptureOption
    {
        /// <summary>
        /// 图片id url
        /// </summary>
        public string ImageIdUrl { get; set; }

        /// <summary>
        /// 抓取者
        /// </summary>
        public string Captor { get; set; }

        /// <summary>
        /// 是否开机自启
        /// </summary>
        public bool AutoStart { get; set; }

        /// <summary>
        /// 是否设置为壁纸
        /// </summary>
        public bool SetWallpaper { get; set; } = true;

        /// <summary>
        /// 拼接壁纸保存目录 默认是images
        /// </summary>
        public string WallpaperFolder { get;  set; } = "images";

        /// <summary>
        /// 下载图片保存位置  默认是images目录
        /// </summary>
        public string SavePath { get; set; } = "images";

        /// <summary>
        /// 分辨率
        /// </summary>
        public Resolution Resolution { get; set; }

        /// <summary>
        /// 缩放比例 单位%  默认为100%
        /// </summary>
        public int Zoom { get; set; } = 100;

        /// <summary>
        /// 更新频率 单位 min  默认20
        /// </summary>
        public int Interval { get; set; } = 20;

        /// <summary>
        /// 上一张图片id
        /// </summary>
        public string LastImageId { get; set; }
        

       
    }


    [Flags]
    public enum Resolution
    {
        /// <summary>
        /// 550 * 550
        /// </summary>
        r_550 = 1,

        /// <summary>
        /// 1100 * 1100
        /// </summary>
        r_1100 = 1 << 2,

        /// <summary>
        /// 2200 * 2200
        /// </summary>
        r_2200 = 1 << 3,

        /// <summary>
        /// 4400 * 4400
        /// </summary>
        r_4400 = 1 << 4,

        /// <summary>
        /// 8800 * 8800
        /// </summary>
        r_8800 = 1 << 5,

        
    }

    public static class EnumExtension
    {
        public static string GetName(this Resolution resolution)
        {
            var r = resolution.ToString()[2..];
            return $"{r}*{r}";
        }

        public static Dictionary<string, Resolution> GetAllResolution()
        {
            Dictionary<string, Resolution> result = new Dictionary<string, Resolution>();
            foreach (Resolution item in Enum.GetValues(typeof(Resolution)))
            {
                result.Add(item.GetName(), item);
            }

            return result;
        }
    }
}