﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer.Test
{
    using System;
    using System.Threading.Tasks;
    using LoRaTools.LoRaMessage;
    using LoRaTools.Utils;
    using LoRaWan.NetworkServer;
    using LoRaWan.Test.Shared;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Caching.Memory;
    using Moq;
    using Xunit;

    // End to end tests without external dependencies (IoT Hub, Service Facade Function)
    public class MessageProcessor_End2End_NoDep_Join_Slow_Get_Twin_Tests : MessageProcessorTestBase
    {
        [Theory]
        [InlineData(ServerGatewayID)]
        [InlineData(null)]
        public async Task When_Join_Fails_Due_To_Timeout_Second_Try_Should_Reuse_Cached_Device_Twin(string deviceGatewayID)
        {
            var simulatedDevice = new SimulatedDevice(TestDeviceInfo.CreateOTAADevice(1, gatewayID: deviceGatewayID));
            var joinRequest1 = simulatedDevice.CreateJoinRequest();

            // Create Rxpk
            var joinRequestRxpk1 = joinRequest1.SerializeUplink(simulatedDevice.LoRaDevice.AppKey).Rxpk[0];

            var joinRequestDevNonce1 = ConversionHelper.ByteArrayToString(joinRequest1.DevNonce);
            var devAddr = string.Empty;
            var devEUI = simulatedDevice.LoRaDevice.DeviceID;
            var appEUI = simulatedDevice.LoRaDevice.AppEUI;

            var loRaDeviceClient = new Mock<ILoRaDeviceClient>(MockBehavior.Strict);

            // Device twin will be queried twice, 1st time will take 7 seconds, 2nd time 0.1 second
            var twin = new Twin();
            twin.Properties.Desired[TwinProperty.DevEUI] = devEUI;
            twin.Properties.Desired[TwinProperty.AppEUI] = simulatedDevice.LoRaDevice.AppEUI;
            twin.Properties.Desired[TwinProperty.AppKey] = simulatedDevice.LoRaDevice.AppKey;
            twin.Properties.Desired[TwinProperty.GatewayID] = deviceGatewayID;
            twin.Properties.Desired[TwinProperty.SensorDecoder] = simulatedDevice.LoRaDevice.SensorDecoder;
            loRaDeviceClient.Setup(x => x.GetTwinAsync())
                .Returns(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(7));
                    return twin;
                });

            // Device twin will be updated
            string afterJoinAppSKey = null;
            string afterJoinNwkSKey = null;
            string afterJoinDevAddr = null;
            loRaDeviceClient.Setup(x => x.UpdateReportedPropertiesAsync(It.IsNotNull<TwinCollection>()))
                .Callback<TwinCollection>((updatedTwin) =>
                {
                    afterJoinAppSKey = updatedTwin[TwinProperty.AppSKey];
                    afterJoinNwkSKey = updatedTwin[TwinProperty.NwkSKey];
                    afterJoinDevAddr = updatedTwin[TwinProperty.DevAddr];
                })
                .ReturnsAsync(true);

            // Lora device api will be search by devices with matching deveui,
            var loRaDeviceApi = new Mock<LoRaDeviceAPIServiceBase>(MockBehavior.Strict);
            loRaDeviceApi.Setup(x => x.SearchAndLockForJoinAsync(this.ServerConfiguration.GatewayID, devEUI, appEUI, joinRequestDevNonce1))
                .ReturnsAsync(new SearchDevicesResult(new IoTHubDeviceInfo(devAddr, devEUI, "aabb").AsList()));

            // using factory to create mock of
            var loRaDeviceFactory = new TestLoRaDeviceFactory(loRaDeviceClient.Object);

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var deviceRegistry = new LoRaDeviceRegistry(this.ServerConfiguration, memoryCache, loRaDeviceApi.Object, loRaDeviceFactory);

            var frameCounterUpdateStrategyFactory = new LoRaDeviceFrameCounterUpdateStrategyFactory(this.ServerConfiguration.GatewayID, loRaDeviceApi.Object);

            var messageProcessor = new MessageProcessor(
                this.ServerConfiguration,
                deviceRegistry,
                frameCounterUpdateStrategyFactory,
                new LoRaPayloadDecoder());

            // 1st join request
            // Should fail
            var joinRequestDownlinkMessage1 = await messageProcessor.ProcessMessageAsync(joinRequestRxpk1);
            Assert.Null(joinRequestDownlinkMessage1);

            // 2nd attempt
            var joinRequest2 = simulatedDevice.CreateJoinRequest();
            var joinRequestRxpk2 = joinRequest2.SerializeUplink(simulatedDevice.LoRaDevice.AppKey).Rxpk[0];
            var joinRequestDevNonce2 = LoRaTools.Utils.ConversionHelper.ByteArrayToString(joinRequest2.DevNonce);

            // setup response to this device search
            loRaDeviceApi.Setup(x => x.SearchAndLockForJoinAsync(this.ServerConfiguration.GatewayID, devEUI, appEUI, joinRequestDevNonce2))
                .ReturnsAsync(new SearchDevicesResult(new IoTHubDeviceInfo(devAddr, devEUI, "aabb").AsList()));

            var joinRequestDownlinkMessage2 = await messageProcessor.ProcessMessageAsync(joinRequestRxpk2);
            Assert.NotNull(joinRequestDownlinkMessage2);
            var joinAccept = new LoRaPayloadJoinAccept(Convert.FromBase64String(joinRequestDownlinkMessage2.Txpk.Data), simulatedDevice.LoRaDevice.AppKey);
            Assert.Equal(joinAccept.DevAddr.ToArray(), ConversionHelper.StringToByteArray(afterJoinDevAddr));

            var devicesForDevAddr = deviceRegistry.InternalGetCachedDevicesForDevAddr(afterJoinDevAddr);
            Assert.Single(devicesForDevAddr); // should have the single device
            Assert.True(devicesForDevAddr.TryGetValue(devEUI, out var loRaDevice));

            Assert.Equal(simulatedDevice.AppKey, loRaDevice.AppKey);
            Assert.Equal(simulatedDevice.AppEUI, loRaDevice.AppEUI);
            Assert.Equal(afterJoinAppSKey, loRaDevice.AppSKey);
            Assert.Equal(afterJoinNwkSKey, loRaDevice.NwkSKey);
            Assert.Equal(afterJoinDevAddr, loRaDevice.DevAddr);
            if (deviceGatewayID == null)
                Assert.Null(loRaDevice.GatewayID);
            else
                Assert.Equal(deviceGatewayID, loRaDevice.GatewayID);

            // fcnt is restarted
            Assert.Equal(0, loRaDevice.FCntUp);
            Assert.Equal(0, loRaDevice.FCntDown);
            Assert.False(loRaDevice.HasFrameCountChanges);

            // searching the device should happen twice
            loRaDeviceApi.Verify(x => x.SearchAndLockForJoinAsync(ServerGatewayID, devEUI, appEUI, It.IsNotNull<string>()), Times.Exactly(2));

            // getting the device twin should happens once
            loRaDeviceClient.Verify(x => x.GetTwinAsync(), Times.Once());

            loRaDeviceClient.VerifyAll();
            loRaDeviceApi.VerifyAll();
        }
    }
}
