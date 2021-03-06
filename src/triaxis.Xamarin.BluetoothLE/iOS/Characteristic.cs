using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class Characteristic : ICharacteristic
    {
        Service _service;
        CBCharacteristic _characteristic;
        readonly CharacteristicUuid _uuid;

        public Characteristic(Service service, CBCharacteristic characteristic)
        {
            _service = service;
            _characteristic = characteristic;
            _uuid = characteristic.UUID.ToUuid();
        }

        public ref readonly CharacteristicUuid Uuid => ref _uuid;
        public CharacteristicProperties Properties => (CharacteristicProperties)_characteristic.Properties;
        public CBCharacteristic CBCharacteristic => _characteristic;

        public IObservable<byte[]> Notifications() => Observable.Create<byte[]>(async (observer) =>
        {
            bool connected = true;

            void HandleData(CBCharacteristic ch, byte[] val)
            {
                if (ch == _characteristic)
                    observer.OnNext(val);
            }

            void HandleClosed(object sender, Exception err)
            {
                connected = false;

                if (err == null)
                    observer.OnCompleted();
                else
                    observer.OnError(err);
            }

            _service.Connection.CharacteristicChanged += HandleData;
            _service.Connection.Closed += HandleClosed;

            await _service.Connection.SetCharacteristicNotificationsAsync(this, true);

            return async () =>
            {
                _service.Connection.Closed -= HandleClosed;

                try
                {
                    if (connected)
                    {
                        await _service.Connection.SetCharacteristicNotificationsAsync(this, false);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to disable characteristic notifications: {e}");
                }
                finally
                {
                    _service.Connection.CharacteristicChanged -= HandleData;
                }
            };
        });

        public Task<byte[]> ReadAsync()
           => _service.Connection.ReadCharacteristicAsync(this);

        public Task WriteAsync(byte[] data)
            => _service.Connection.WriteCharacteristicAsync(this, data, false);

        public Task WriteWithoutResponseAsync(byte[] data)
            => _service.Connection.WriteCharacteristicAsync(this, data, true);
    }
}
