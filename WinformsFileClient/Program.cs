namespace WinformsFileClient;

using Net.Connection.Clients.Tcp;

internal static class Program
{
    public static Client Client { get; set; }
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static async void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        if (await Auth.TryLoadUser())
            Application.Run(new MainForm());
        //else
            //Application.
    }
}