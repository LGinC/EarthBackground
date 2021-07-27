using System.Runtime.InteropServices;

namespace EarthBackground.Background
{
    static class Wallpaper
    {
        public const int COLOR_DESKTOP = 1;
        public const int SPIF_UPDATEINIFILE = 0x01;
        public const int SPIF_SENDWININICHANGE = 0x02;
        private static int SPI_SETDESKWALLPAPER = 20;
        

        public static void Set(string filePath)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filePath, SPIF_UPDATEINIFILE); //filename为图片地址，最后一个参数需要为1   0的话在重启后就变回原来的了
        }


        /// <summary>
        /// 调用电脑底层的接口
        /// <para> uAction参数    意义和使用方法    </para>
        /// <para> 6    设置视窗的大小，SystemParametersInfo(6, 放大缩小值, P, 0)，lpvParam为long型    </para>
        /// <para> 17    开关屏保程序，SystemParametersInfo(17, False, P, 1)，uParam为布尔型          </para>
        /// <para> 13，24    改变桌面图标水平和垂直间距，uParam为间距值(像素)，lpvParam为long型           </para>
        /// <para> 15    设置屏保等待时间，SystemParametersInfo(15, 秒数, P, 1)，lpvParam为long型     </para>
        /// <para> 20    设置桌面背景墙纸，SystemParametersInfo(20, True, 图片路径, 1)                </para>
        /// <para> 93    开关鼠标轨迹，SystemParametersInfo(93, 数值, P, 1)，uParam为False则关闭      </para>
        /// <para> 97    开关Ctrl+Alt+Del窗口，SystemParametersInfo(97, False, A, 0)，uParam为布尔型 </para> 
        /// </summary>
        /// <param name="uAction">指定要设置的参数。参考uAction常数表</param>
        /// <param name="uParam">Any，按引用调用的Integer、Long和数据结构</param>
        /// <param name="lpvParam">图片的路径</param>
        /// <param name="fuWinIni">设置系统参数时是否应更新用户设置参数</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", CharSet = CharSet.Unicode)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    }
}
