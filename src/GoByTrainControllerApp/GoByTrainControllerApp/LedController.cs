using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace GoByTrainControllerApp
{
    enum ELedStatus
    {
        Off,
        InitBlink,
        ScanningBlink,
        On,
    }

    class LedController
    {
        private const int EmergencyLedPinNum = 4;

        private const int StatusLedPinNum = 2;

        private const int InitBlinCount = 20;

        private const int ScanningBlinkCount = 8;

        private bool _isOn;

        private int _counter;

        private GpioPin _statusPin;

        private GpioPin _emergencyPin;

        public ELedStatus Status { get; private set; }

        public void Inititalize()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                throw new Exception("GPIO is not found.");
            }

            _statusPin = gpio.OpenPin(StatusLedPinNum);
            _statusPin.Write(GpioPinValue.Low);
            _statusPin.SetDriveMode(GpioPinDriveMode.Output);

            _emergencyPin = gpio.OpenPin(EmergencyLedPinNum);
            _emergencyPin.Write(GpioPinValue.Low);
            _emergencyPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void Tick()
        {
            switch (Status)
            {
                case ELedStatus.Off:
                    _isOn = false;
                    break;
                case ELedStatus.InitBlink:
                    _counter++;
                    if (_counter == InitBlinCount)
                    {
                        _isOn = !_isOn;
                        _counter = 0;
                    }
                    break;
                case ELedStatus.ScanningBlink:
                    _counter++;
                    if (_counter == ScanningBlinkCount)
                    {
                        _isOn = !_isOn;
                        _counter = 0;
                    }
                    break;
                case ELedStatus.On:
                    _isOn = true;
                    break;
            }

            var output = _isOn ? GpioPinValue.High : GpioPinValue.Low;

            _statusPin.Write(output);
        }

        public void SetStatus(ELedStatus status)
        {
            if (status == Status) return;

            _counter = 0;
            Status = status;
        }

        public void SetEmergency(bool isEmergency)
        {
            var value = isEmergency ? GpioPinValue.High : GpioPinValue.Low;

            _emergencyPin.Write(value);
        }
    }
}
