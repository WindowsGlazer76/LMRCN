using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LMRCN.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;



namespace LMRCN.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private Netzwerk Nw { get; set; } = new Netzwerk();
        public ObservableCollection<Endgerät> DeviceList => Nw.DeviceList;

        [ObservableProperty]
        private Endgerät currentDevice = new Endgerät("", "", 0, "");
        [ObservableProperty]
        private int deviceIndex = 0;
        [ObservableProperty]
        private bool isNotScanning = true;
        [ObservableProperty]
        private bool showSpinner0 = false;
        [ObservableProperty]
        private bool showSpinner1 = false;

        public ICommand ScanCommand { get; set; }
        public ICommand OpenDeviceControl { get; set; }
        public ICommand DeviceCommandCommand { get; set; }

        public MainViewModel()
        {
            ScanCommand = new AsyncRelayCommand(ScanButtonClicked);
            OpenDeviceControl = new AsyncRelayCommand<Endgerät>(DeviceButtonClicked);
            DeviceCommandCommand = new AsyncRelayCommand<string>(DeviceCommand);
        }

        private async Task ScanButtonClicked()
        {
            IsNotScanning = false;
            ShowSpinner0 = true;
            await Nw.ScanNetwork();
            ShowSpinner0 = false;
            IsNotScanning = true;
        }

        private async Task DeviceButtonClicked(Endgerät device)
        {
            CurrentDevice = device;
            DeviceIndex = DeviceList.IndexOf(device);
            var page = App.Current.Handler.MauiContext.Services.GetService<DeviceControl>();
            await Application.Current.MainPage.Navigation.PushAsync(page);
        }

        private async Task DeviceCommand(string command)
        {
            ShowSpinner1 = true;
            bool success = await Nw.SendCommand(CurrentDevice, command);
            Nw.DeviceList[DeviceIndex] = CurrentDevice;
            ShowSpinner1 = false;
        }
    }
}
