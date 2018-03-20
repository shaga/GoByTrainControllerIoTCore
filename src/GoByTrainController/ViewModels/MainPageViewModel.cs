using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using Windows.UI.Xaml;
using GoByTrainController.Models;
using Prism.Mvvm;
using Prism.Windows.Navigation;

namespace GoByTrainController.ViewModels
{
    class MainPageViewModel : BindableBase, INavigationAware
    {
        private static CoreDispatcher Dispatcher => CoreApplication.MainView.Dispatcher;

        private INavigationService _navigationService;

        private BcoreController _bcoreController;

        private int _value;

        private DispatcherTimer _timer;

        private GpioPin _statusLed;

        private GpioPin _emergencyLed;


        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public MainPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            _bcoreController = new BcoreController();
        }

        #region implement INavigationAware

        public async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Initialize);
        }

        public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
        }

        #endregion

        private async void Initialize()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                throw new Exception("hoge");
            }

            _statusLed = gpio.OpenPin(3);
            _emergencyLed = gpio.OpenPin(4);


            _statusLed.Write(GpioPinValue.Low);
            _emergencyLed.Write(GpioPinValue.Low);

            var isStatusOn = false;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (sender, o) =>
            {
                isStatusOn = !isStatusOn;
                var value = isStatusOn ? GpioPinValue.High : GpioPinValue.Low;

                _statusLed.Write(value);
            };


            var result = await _bcoreController.Initialize();
        }
    }
}
