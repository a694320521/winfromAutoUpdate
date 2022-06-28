using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace update
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] islogin)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (islogin.Length > 0)
                Application.Run(new IwinUpdate(true));
            else
                Application.Run(new IwinUpdate(false));
        }
    }
}
