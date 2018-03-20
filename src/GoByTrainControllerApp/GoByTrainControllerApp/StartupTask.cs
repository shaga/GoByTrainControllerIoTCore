using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using Windows.UI.ApplicationSettings;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace GoByTrainControllerApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const int TickInterval = 25;

        private BackgroundTaskDeferral _deferral;

        private ThreadPoolTimer _timer;

        private LedController _ledController;

        private PsControllerReader _controller;

        private BcoreController _bcore;

        private int _speed = 0x80;

        private int _accel = 0;

        private int _accelCounter = 0;

        private int _brakeCounter = 0;

        private bool _doorIsOpened = false;

        private bool IsStopping => _speed == 0x80;

        private bool IsRunning => _speed < 0x80;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            _ledController = new LedController();

            _ledController.Inititalize();

            _ledController.SetStatus(ELedStatus.InitBlink);

            _controller = new PsControllerReader();

            _bcore = new BcoreController();

            await _controller.Init();

            _timer = ThreadPoolTimer.CreatePeriodicTimer(OnTimerTick, TimeSpan.FromMilliseconds(TickInterval));
        }

        private void OnTimerTick(ThreadPoolTimer t)
        {
            if (!_bcore.IsConnected && !_bcore.IsConnecting)
            {
                _ledController.SetEmergency(false);
                _ledController.SetStatus(ELedStatus.ScanningBlink);
                _bcore.Connect();
            }
            else if (_bcore.IsConnected)
            {
                _ledController.SetStatus(ELedStatus.On);
                UpdateStatus();

                if (IsStopping)
                {
                    // 停車中はドアの開閉と発車ベルを鳴らすことができる

                }
                else
                {
                }
            }

            _ledController.Tick();
        }

        private void UpdateStatus()
        {
            var speed = _speed;

            if (_controller.ReadValue())
            {
                if (_controller.IsEmergency)
                {
                    speed = 0x80;
                    _accel = 0;
                    _accelCounter = 0;
                    _brakeCounter = 0;
                    _ledController.SetEmergency(true);
                }
                else
                {
                    _ledController.SetEmergency(false);
                }

                if (_controller.BrakeValue > 0)
                {
                    _accel = 0 - _controller.BrakeValue;
                }
                else if (_controller.AccelValue > 0)
                {
                    _accel = _controller.AccelValue;
                }
                else
                {
                    _accel = 0;
                    _accelCounter = 0;
                    _brakeCounter = 0;
                }
            }


            if (_accel > 0)
            {
                if (speed == 0x80)
                {
                    speed = 0x70;
                }
                else
                {
                    _accelCounter++;

                    if ((_accelCounter >= 10 && _accel == 1) || (_accelCounter >= 8 && _accel == 2) ||
                        (_accelCounter >= 6 && _accel == 3) ||
                        (_accelCounter >= 4 && _accel == 4) || (_accelCounter >= 2 && _accel == 5))
                    {
                        _accelCounter = 0;
                        speed--;
                    }
                }
            }
            else if (_accel < 0)
            {
                _brakeCounter++;

                if ((_brakeCounter >= 10 && _accel == -1) || (_brakeCounter >= 9 && _accel == -2) ||
                    (_brakeCounter >= 8 && _accel == -3) ||
                    (_brakeCounter >= 7 && _accel == -4) || (_brakeCounter >= 6 && _accel == -5) ||
                    (_brakeCounter >= 5 && _accel == -6) ||
                    (_brakeCounter >= 4 && _accel == -7) || (_brakeCounter >= 3 && _accel == -8) ||
                    (_brakeCounter >= 2 && _accel == -9))
                {
                    speed++;
                    _brakeCounter = 0;
                }

                if (speed > 0x7a)
                {
                    speed = 0x80;
                }      
            }

            if (speed < 0) speed = 0;
            else if (speed > 0x80) speed = 0x80;

            if (_speed == 0x80 && speed != 0x80 && _doorIsOpened)
            {
                _ledController.SetEmergency(true);
                speed = 0x80;
            }

            if (speed != _speed)
            {
                _bcore.SetMotorSpeed(speed);
            }
            _speed = speed;
        }
    }
}
