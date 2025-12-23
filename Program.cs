using System;
using System.Windows.Forms;

namespace shop
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // CHẠY FORM ĐÚNG TÊN
            Application.Run(new Form1());
        }
    }
}
