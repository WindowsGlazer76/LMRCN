using LMRCN.Models;
namespace LMRCN
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App()
        {
            InitializeComponent();

#if WINDOWS
            Netzwerk nw = new Netzwerk();
            _ = nw.StartServer();
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}