Imports System.Windows.Forms

Module Program

    Private _mainForm As MainForm

    <STAThread()>
    Sub Main()
        AddHandler AppDomain.CurrentDomain.ProcessExit, Sub(s, e) _mainForm?.ForceCleanup()

        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        _mainForm = New MainForm()
        Application.Run(_mainForm)
    End Sub

End Module
