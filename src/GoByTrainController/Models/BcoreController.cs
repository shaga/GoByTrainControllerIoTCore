using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace GoByTrainController.Models
{
    class BcoreController : IDisposable
    {
        #region const

        private const string UuidPrefix = "389CAAF";
        private const string UuidSurfix = "-843F-4D3B-959D-C954CCE14655";

        private const string BcoreTrainDeviceName = "bCore_20C38FAC46FD";

        private static readonly Guid ServiceUuid = Guid.Parse($"{UuidPrefix}0{UuidSurfix}");
        private static readonly Guid BatteryCharacteristicUuid = Guid.Parse($"{UuidPrefix}1{UuidSurfix}");
        private static readonly Guid MotorCharacteristicUuid = Guid.Parse($"{UuidPrefix}2{UuidSurfix}");

        private static readonly string BcoreServiceSelector = GattDeviceService.GetDeviceSelectorFromUuid(ServiceUuid);

        #endregion

        private static CoreDispatcher Dispatcher => CoreApplication.MainView.Dispatcher;

        private BluetoothLEDevice _device;
        private GattDeviceService _service;
        private GattCharacteristic _batteryCharacteristic;
        private GattCharacteristic _motorCharacteristic;
        private BluetoothLEAdvertisementWatcher _watcher;
        private DeviceInformationCollection _bcoreCollection;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private ushort _batteryVoltage;

        private DispatcherTimer _timerReadBattery;

        public event EventHandler ConnectionChanged;



        public BcoreController()
        {
            _watcher = new BluetoothLEAdvertisementWatcher();
            _watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(ServiceUuid);
            _watcher.Received += OnReceivedAdvertisement;

            _timerReadBattery = new DispatcherTimer();
            _timerReadBattery.Interval = TimeSpan.FromMilliseconds(500);
            _timerReadBattery.Tick += OnTickReadBattery;
        }

        public void Dispose()
        {
            if (_timerReadBattery?.IsEnabled ?? false)
            {
                _timerReadBattery.Stop();
            }

            _timerReadBattery = null;


            InitDeviceInfo();
        }

        public async Task<bool> Initialize()
        {
            _watcher.Start();

            var count = 0;

            while (count < 300 && _motorCharacteristic == null)
            {
                await Task.Delay(100);
                count++;
            }

            await _semaphore.WaitAsync();

            if (_watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started) _watcher.Stop();

            _semaphore.Release();

            return _motorCharacteristic != null;
        }

        public async void SetMotorSpeed(int speed)
        {
            if (speed < 0) speed = 0;
            else if (speed > 0x80) speed = 0x80;

            var data = new byte[] {0x00, (byte) speed};
            await _motorCharacteristic.WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithoutResponse);
        }

        private void InitDeviceInfo()
        {
            if (_device != null)
            {
                _device.ConnectionStatusChanged -= OnChanngedConnectionState;
                _device.Dispose();
            }

            _device = null;

            _service?.Dispose();
            _service = null;

            _batteryCharacteristic = null;
            _motorCharacteristic = null;
        }

        private async void OnReceivedAdvertisement(BluetoothLEAdvertisementWatcher watcher,
            BluetoothLEAdvertisementReceivedEventArgs e)
        {
            await _semaphore.WaitAsync();

            _watcher.Stop();

            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);

            if (_device == null)
            {
                _watcher.Start();
                _semaphore.Release();
                return;
            }

            _device .ConnectionStatusChanged += OnChanngedConnectionState;

            var servicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);

            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                InitDeviceInfo();
                _watcher.Start();
                _semaphore.Release();
                return;
            }

            _service = servicesResult.Services.FirstOrDefault(s => s.Uuid.Equals(ServiceUuid));

            if (_service == null)
            {
                InitDeviceInfo();
                _watcher.Start();
                _semaphore.Release();
                return;
            }

            var characteristicsResult = await _service.GetCharacteristicsAsync();

            if (characteristicsResult.Status != GattCommunicationStatus.Success)
            {
                InitDeviceInfo();
                _watcher.Start();
                _semaphore.Release();
                return;
            }

            _batteryCharacteristic =
                characteristicsResult.Characteristics.FirstOrDefault(c => c.Uuid.Equals(BatteryCharacteristicUuid));
            _motorCharacteristic =
                characteristicsResult.Characteristics.FirstOrDefault(c => c.Uuid.Equals(MotorCharacteristicUuid));

            if (_batteryCharacteristic == null || _motorCharacteristic == null)
            {
                InitDeviceInfo();
                _watcher.Start();
                _semaphore.Release();
                return;
            }

            _timerReadBattery.Start();
            _semaphore.Release();
        }

        private void OnChanngedConnectionState(BluetoothLEDevice sender, object args)
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void OnTickReadBattery(object sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (_batteryCharacteristic == null) return;

                var result = await _batteryCharacteristic.ReadValueAsync();

                var buffer = result.Value.ToArray();

                if (buffer == null || buffer.Length < 2) return;

                _batteryVoltage = (ushort)((buffer[1] << 8) | buffer[0]);
            });
        }
    }
}
