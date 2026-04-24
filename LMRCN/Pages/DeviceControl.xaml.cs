using LMRCN.ViewModels;

namespace LMRCN
{
	public partial class DeviceControl : ContentPage
	{
        public DeviceControl(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}