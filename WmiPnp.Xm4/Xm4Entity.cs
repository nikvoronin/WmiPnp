﻿using FluentResults;
using System.Management;

namespace WmiPnp.Xm4
{
    public class Xm4Entity
    {
        private readonly PnpEntity _xm4;
        private readonly PnpEntity _handsFree;

        private Xm4Entity( PnpEntity handsFree, PnpEntity xm4 )
        {
            _handsFree = handsFree;
            _xm4 = xm4;
        }

        public static Result<Xm4Entity> Create()
            => CreateBy(
                HandsFree_PnpEntity_FriendlyNameGeneral,
                Headphones_PnpEntity_FriendlyNameGeneral );

        public static Result<Xm4Entity> CreateBy(
            string handsfreeName,
            string headphonesName )
        {
            handsfreeName ??= HandsFree_PnpEntity_FriendlyNameGeneral;
            headphonesName ??= Headphones_PnpEntity_FriendlyNameGeneral;

            var handsfree =
                PnpEntity
                .ByFriendlyName( handsfreeName );

            if ( handsfree.IsFailed )
                return Result.Fail(
                    $"Can not create {handsfreeName} entity" );

            var xm4headphones =
                PnpEntity
                .ByFriendlyName( headphonesName );

            if ( xm4headphones.IsFailed )
                return Result.Fail(
                    $"Can not create {headphonesName} entity" );

            return
                new Xm4Entity(
                    handsfree.Value
                    , xm4headphones.Value );
        }

        public static Result<Xm4Entity> CreateUnsafe(
            PnpEntity batteryEntity,
            PnpEntity stateEntity )
            => new Xm4Entity(
                batteryEntity,
                stateEntity );

        public int BatteryLevel {
            get {
                var batteryLevel =
                    _handsFree.GetDeviceProperty(
                        PnpEntity.DeviceProperty_BatteryLevel )
                    .ValueOrDefault;

                return (byte)( batteryLevel.Data ?? 0 );
            }
        }

        public bool IsConnected {
            get {
                var connected =
                    _xm4.GetDeviceProperty(
                        PnpEntity.DeviceProperty_IsConnected )
                    .Value;
                return (bool)( connected.Data ?? false );
            }
        }

        public Result<DateTime> LastConnectedTime {
            get {
                var dtResult =
                    _xm4.GetDeviceProperty(
                        PnpEntity.DeviceProperty_LastConnectedTime );

                return
                    dtResult.IsSuccess
                    ? ManagementDateTimeConverter
                        .ToDateTime( dtResult.Value.Data as string )
                        .ToUniversalTime()
                    : Result.Fail(
                        "Can not find `LastConnectedTime` property. It is possible the device is still connected." );
            }
        }

        /// <summary>
        /// Find all bluetooth sub-devices related to headphones
        /// </summary>
        private IEnumerable<PnpEntity> BluetoothDevices
            => PnpEntity.LikeFriendlyNameForClass(
                name: Headphones_PnpEntity_FriendlyNameGeneral,
                className: Bluetooth_PnpClassName );

        /// <summary>
        /// Try connect already paired bluetooth headphones.
        /// Application have to be runned under the Administrative rights
        /// </summary>
        public void TryConnect()
        {
            foreach (var bt in BluetoothDevices) {
                bt.Disable();
                bt.Enable();
            }
        }

        /// <summary>
        /// Try disconnect already paired bluetooth headphones.
        /// Application have to be runned under the Administrative rights
        /// </summary>
        public void TryDisconnect()
        {
            foreach (var bt in BluetoothDevices.Reverse()) { // TODO: Is reverse meaningful?
                bt.Disable();
                Thread.Sleep( 100 ); // TODO: Is pause meaningful?
                bt.Disable();
            }
        }

        //// TODO: candidate to remove, WH-1000XM4 only
        //public const string HandsFree_PnpEntity_FriendlyName
        //    = "WH-1000XM4 Hands-Free AG"; // Battery level related
        //public const string Headphones_PnpEntity_FriendlyName
        //    = "WH-1000XM4"; // Headphones state related
        public const string HandsFree_PnpEntity_FriendlyNameGeneral
            = "W_-1000XM_ Hands-Free AG"; // Battery level related
        public const string Headphones_PnpEntity_FriendlyNameGeneral
            = "W_-1000XM_"; // Headphones state related

        public const string Bluetooth_PnpClassName = "Bluetooth";
    }
}