namespace TransWriter
{
    internal static class Program
    {
        private static Mutex mutex = new Mutex(true, "{B1A2F3C4-D5E6-7890-1234-56789ABCDEF0}");

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new TransForm());
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("程序已经在运行中。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}