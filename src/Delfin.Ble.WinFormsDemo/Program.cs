namespace Delfin.Ble.WinFormsDemo;

static class Program
{
    private static MainForm? _mainForm;

    [STAThread]
    static void Main()
    {
        // Ensure COM port is released even on unexpected shutdown
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _mainForm?.ForceCleanup();

        ApplicationConfiguration.Initialize();
        _mainForm = new MainForm();
        Application.Run(_mainForm);
    }
}
