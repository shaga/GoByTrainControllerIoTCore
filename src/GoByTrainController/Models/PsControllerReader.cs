using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;

namespace GoByTrainController.Models
{
    class PsControllerReader : IDisposable
    {
        private const int SpiClockFrequency = 100000;
        private const byte CmdRetSuccess = 0x5a;
        private static readonly byte[] CmdReadValue = {0x80, 0x42, 0x00, 0x00, 0x00};

        private SpiDevice _device;
        private readonly byte[] _recvBuffer = new byte[5];

        private bool _isEmergency;

        public bool IsInitialized => _device != null;

        public byte AccelValue { get; private set; }

        public byte BrakeValue { get; private set; }

        public PsControllerReader()
        {
            Init();
        }

        public void Dispose()
        {
            
        }

        private async void Init()
        {
            var settings = new SpiConnectionSettings(0)
            {
                ClockFrequency = SpiClockFrequency,
                Mode = SpiMode.Mode3
            };

            var aqs = SpiDevice.GetDeviceSelector("SPI0");
            var info = await DeviceInformation.FindAllAsync(aqs);

            if ((info?.FirstOrDefault()) == null)
            {
                throw new Exception("SPI device not found");
            }

            _device = await SpiDevice.FromIdAsync(info.FirstOrDefault().Id, settings);

            if (_device == null)
            {
                throw new Exception("SPI device is not found");
            }
        }

        public void ReadValue()
        {
            _device.TransferFullDuplex(CmdReadValue, _recvBuffer);

            if (UpdateControllerValue())
            {
                
            }
        }

        private bool UpdateControllerValue()
        {
            var isChanged = false;
            byte accel = 0;
            byte brake = 0;

            var v = (byte)(((~_recvBuffer[3] & 0x0f) << 1) | ((~_recvBuffer[4] & 0x08) >> 3));

            switch (v)
            {
                case 0x1e:
                    accel = 0;
                    break;
                case 0x1d:
                    accel = 1;
                    break;
                case 0x1c:
                    accel = 2;
                    break;
                case 0x17:
                    accel = 3;
                    break;
                case 0x16:
                    accel = 4;
                    break;
                case 0x15:
                    accel = 5;
                    break;
                default:
                    accel = AccelValue;
                    break;
            }

            v = (byte)((~_recvBuffer[4] & 0xf0) >> 4);
            switch (v)
            {
                case 0x0d:
                    brake = 0;
                    break;
                case 0x07:
                    brake = 1;
                    break;
                case 0x05:
                    brake = 2;
                    break;
                case 0x0e:
                    brake = 3;
                    break;
                case 0x0c:
                    brake = 4;
                    break;
                case 0x06:
                    brake = 5;
                    break;
                case 0x04:
                    brake = 6;
                    break;
                case 0x0b:
                    brake = 7;
                    break;
                case 0x09:
                    brake = 8;
                    break;
                default:
                    brake = 9;
                    break;
            }

            if (!_isEmergency && brake == 9)
            {
                // 非常ブレーキ
                AccelValue = 0;
                isChanged = true;
                _isEmergency = true;
            }
            else if (_isEmergency)
            {
                // 非常停止中はアクセル、ブレーキ両方オフに操作できない
                _isEmergency = accel > 0 || brake > 0;
                isChanged = !_isEmergency;
            }
            else if (AccelValue == 0 && BrakeValue == 0)
            {
                // 加減速ない場合はブレーキを優先して変化
                if (brake > 0)
                {
                    BrakeValue = brake;
                    isChanged = true;
                }
                else if (accel > 0)
                {
                    AccelValue = accel;
                    isChanged = true;
                }
            }
            else if (BrakeValue > 0)
            {
                // ブレーキ有効中
                if (brake > 0)
                {
                    // ブレーキ制御中
                    isChanged = BrakeValue != brake;
                    BrakeValue = brake;
                }
                else
                {
                    // ブレーキオフになったのでアクセルを有効に
                    isChanged = true;
                    AccelValue = accel;
                    BrakeValue = 0;
                }
            }
            else if (AccelValue > 0)
            {
                // アクセル有効中
                if (accel > 0)
                {
                    // アクセル制御中
                    isChanged = AccelValue != accel;
                    AccelValue = accel;
                }
                else
                {
                    // アクセルオフになったのでブレーキを有効に
                    isChanged = true;
                    AccelValue = 0;
                    BrakeValue = brake;
                }
            }

            return isChanged;
        }
    }
}
