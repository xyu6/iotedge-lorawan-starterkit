// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using LoRaWan.NetworkServer;
    using LoRaWan.Test.Shared;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Caching.Memory;
    using Moq;
    using Xunit;

    // End to end tests without external dependencies (IoT Hub, Service Facade Function)
    // Multi gateway specifc scenarios
    public class MessageProcessor_End2End_NoDep_MultiGateway_Tests : MessageProcessorTestBase
    {
        [Theory]
        [InlineData(0, 0, 1)]
        public async Task When_Fcnt_Down_Fails_Should_Stop_And_Not_Update_Device_Twin(int initialFcntDown, int initialFcntUp, int payloadFcnt)
        {
            var simulatedDevice = new SimulatedDevice(TestDeviceInfo.CreateABPDevice(1, gatewayID: null));
            simulatedDevice.FrmCntDown = initialFcntDown;
            simulatedDevice.FrmCntUp = initialFcntUp;
            var loRaDeviceClient = new Mock<ILoRaDeviceClient>(MockBehavior.Strict);

            var devEUI = simulatedDevice.LoRaDevice.DeviceID;
            var devAddr = simulatedDevice.LoRaDevice.DevAddr;

            // Lora device api
            var loRaDeviceApi = new Mock<LoRaDeviceAPIServiceBase>(MockBehavior.Strict);
            loRaDeviceApi.Setup(x => x.NextFCntDownAsync(devEUI, initialFcntDown, payloadFcnt, ServerGatewayID)).ReturnsAsync((ushort)0);

            // using factory to create mock of
            var loRaDeviceFactory = new TestLoRaDeviceFactory(loRaDeviceClient.Object);

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var device = TestUtils.CreateFromSimulatedDevice(simulatedDevice, loRaDeviceClient.Object);

            var dicForDevAddr = new DevEUIToLoRaDeviceDictionary();
            dicForDevAddr.TryAdd(devEUI, device);
            memoryCache.Set(devAddr, dicForDevAddr);
            var deviceRegistry = new LoRaDeviceRegistry(this.ServerConfiguration, memoryCache, loRaDeviceApi.Object, loRaDeviceFactory);

            var frameCounterUpdateStrategyFactory = new LoRaDeviceFrameCounterUpdateStrategyFactory(this.ServerConfiguration.GatewayID, loRaDeviceApi.Object);

            // Send to message processor
            var messageProcessor = new MessageProcessor(
                this.ServerConfiguration,
                deviceRegistry,
                frameCounterUpdateStrategyFactory,
                new LoRaPayloadDecoder());

            // sends unconfirmed message
            var unconfirmedMessagePayload = simulatedDevice.CreateConfirmedDataUpMessage("hello", fcnt: payloadFcnt);
            var rxpk = unconfirmedMessagePayload.SerializeUplink(simulatedDevice.AppSKey, simulatedDevice.NwkSKey).Rxpk[0];
            var unconfirmedMessageResult = await messageProcessor.ProcessMessageAsync(rxpk);
            Assert.Null(unconfirmedMessageResult);

            // verify that the device in device registry has correct properties and frame counters
            var devicesForDevAddr = deviceRegistry.InternalGetCachedDevicesForDevAddr(devAddr);
            Assert.Single(devicesForDevAddr);
            Assert.True(devicesForDevAddr.TryGetValue(devEUI, out var loRaDevice));
            Assert.Equal(devAddr, loRaDevice.DevAddr);
            Assert.Equal(devEUI, loRaDevice.DevEUI);
            Assert.True(loRaDevice.IsABP);
            Assert.Equal(initialFcntUp, loRaDevice.FCntUp);
            Assert.Equal(initialFcntDown, loRaDevice.FCntDown);
            Assert.False(loRaDevice.HasFrameCountChanges);

            loRaDeviceClient.VerifyAll();
            loRaDeviceApi.VerifyAll();
        }

        [Theory]
        [InlineData(null, 0, null, null)]
        [InlineData(null, 0, 1, 1)]
        [InlineData(null, 0, 100, 20)]
        [InlineData(null, 1, null, null)]
        [InlineData(null, 1, 1, 1)]
        [InlineData(null, 1, 100, 20)]
        public async Task ABP_New_Loaded_Device_With_Fcnt_1_Or_0_Should_Reset_Fcnt_And_Send_To_IotHub(
            string twinGatewayID,
            int payloadFcntUp,
            int? deviceTwinFcntUp,
            int? deviceTwinFcntDown)
        {
            var simulatedDevice = new SimulatedDevice(TestDeviceInfo.CreateABPDevice(1, gatewayID: null));
            var loRaDeviceClient = new Mock<ILoRaDeviceClient>(MockBehavior.Strict);

            var devEUI = simulatedDevice.LoRaDevice.DeviceID;
            var devAddr = simulatedDevice.LoRaDevice.DevAddr;

            // message will be sent
            LoRaDeviceTelemetry loRaDeviceTelemetry = null;
            loRaDeviceClient.Setup(x => x.SendEventAsync(It.IsNotNull<LoRaDeviceTelemetry>(), null))
                .Callback<LoRaDeviceTelemetry, Dictionary<string, string>>((t, _) => loRaDeviceTelemetry = t)
                .ReturnsAsync(true);

            // C2D message will be checked
            loRaDeviceClient.Setup(x => x.ReceiveAsync(It.IsNotNull<TimeSpan>()))
                .ReturnsAsync((Message)null);

            // twin will be loaded
            var initialTwin = new Twin();
            initialTwin.Properties.Desired[TwinProperty.DevEUI] = devEUI;
            initialTwin.Properties.Desired[TwinProperty.AppEUI] = simulatedDevice.LoRaDevice.AppEUI;
            initialTwin.Properties.Desired[TwinProperty.AppKey] = simulatedDevice.LoRaDevice.AppKey;
            initialTwin.Properties.Desired[TwinProperty.NwkSKey] = simulatedDevice.LoRaDevice.NwkSKey;
            initialTwin.Properties.Desired[TwinProperty.AppSKey] = simulatedDevice.LoRaDevice.AppSKey;
            initialTwin.Properties.Desired[TwinProperty.DevAddr] = devAddr;
            if (twinGatewayID != null)
                initialTwin.Properties.Desired[TwinProperty.GatewayID] = twinGatewayID;
            initialTwin.Properties.Desired[TwinProperty.SensorDecoder] = simulatedDevice.LoRaDevice.SensorDecoder;
            if (deviceTwinFcntDown.HasValue)
                initialTwin.Properties.Reported[TwinProperty.FCntDown] = deviceTwinFcntDown.Value;
            if (deviceTwinFcntUp.HasValue)
                initialTwin.Properties.Reported[TwinProperty.FCntUp] = deviceTwinFcntUp.Value;

            loRaDeviceClient.Setup(x => x.GetTwinAsync()).ReturnsAsync(initialTwin);

            // twin will be updated with new fcnt
            int? fcntUpSavedInTwin = null;
            int? fcntDownSavedInTwin = null;

            var shouldSaveTwin = (deviceTwinFcntDown ?? 0) != 0 || (deviceTwinFcntUp ?? 0) != 0;
            if (shouldSaveTwin)
            {
                loRaDeviceClient.Setup(x => x.UpdateReportedPropertiesAsync(It.IsNotNull<TwinCollection>()))
                    .Callback<TwinCollection>((t) =>
                    {
                        fcntUpSavedInTwin = (int)t[TwinProperty.FCntUp];
                        fcntDownSavedInTwin = (int)t[TwinProperty.FCntDown];
                    })
                    .ReturnsAsync(true);
            }

            // Lora device api
            var loRaDeviceApi = new Mock<LoRaDeviceAPIServiceBase>(MockBehavior.Strict);

            // multi gateway will reset the fcnt
            if (shouldSaveTwin)
            {
                loRaDeviceApi.Setup(x => x.ABPFcntCacheResetAsync(devEUI))
                    .ReturnsAsync(true);
            }

            // device api will be searched for payload
            loRaDeviceApi.Setup(x => x.SearchByDevAddrAsync(devAddr))
                .ReturnsAsync(new SearchDevicesResult(new IoTHubDeviceInfo(devAddr, devEUI, "abc").AsList()));

            // using factory to create mock of
            var loRaDeviceFactory = new TestLoRaDeviceFactory(loRaDeviceClient.Object);

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var deviceRegistry = new LoRaDeviceRegistry(this.ServerConfiguration, memoryCache, loRaDeviceApi.Object, loRaDeviceFactory);

            var frameCounterUpdateStrategyFactory = new LoRaDeviceFrameCounterUpdateStrategyFactory(this.ServerConfiguration.GatewayID, loRaDeviceApi.Object);

            // Send to message processor
            var messageProcessor = new MessageProcessor(
                this.ServerConfiguration,
                deviceRegistry,
                frameCounterUpdateStrategyFactory,
                new LoRaPayloadDecoder());

            // sends unconfirmed message
            var unconfirmedMessagePayload = simulatedDevice.CreateUnconfirmedDataUpMessage("hello", fcnt: payloadFcntUp);
            var rxpk = unconfirmedMessagePayload.SerializeUplink(simulatedDevice.AppSKey, simulatedDevice.NwkSKey).Rxpk[0];
            var unconfirmedMessageResult = await messageProcessor.ProcessMessageAsync(rxpk);
            Assert.Null(unconfirmedMessageResult);

            // Ensure that a telemetry was sent
            Assert.NotNull(loRaDeviceTelemetry);
            // Assert.Equal(msgPayload, loRaDeviceTelemetry.data);

            // Ensure that the device twins were saved
            if (shouldSaveTwin)
            {
                Assert.NotNull(fcntDownSavedInTwin);
                Assert.NotNull(fcntUpSavedInTwin);
                Assert.Equal(0, fcntDownSavedInTwin.Value);
                Assert.Equal(0, fcntUpSavedInTwin.Value);
            }

            // verify that the device in device registry has correct properties and frame counters
            var devicesForDevAddr = deviceRegistry.InternalGetCachedDevicesForDevAddr(devAddr);
            Assert.Single(devicesForDevAddr);
            Assert.True(devicesForDevAddr.TryGetValue(devEUI, out var loRaDevice));
            Assert.Equal(devAddr, loRaDevice.DevAddr);
            Assert.Equal(devEUI, loRaDevice.DevEUI);
            Assert.True(loRaDevice.IsABP);
            Assert.Equal(payloadFcntUp, loRaDevice.FCntUp);
            Assert.Equal(0, loRaDevice.FCntDown);
            if (payloadFcntUp == 0)
                Assert.False(loRaDevice.HasFrameCountChanges); // no changes
            else
                Assert.True(loRaDevice.HasFrameCountChanges); // should have changes!

            loRaDeviceClient.VerifyAll();
            loRaDeviceApi.VerifyAll();
        }

        /// <summary>
        /// If cannot get a fcntdown from api should drop the c2d message
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task When_Getting_C2D_Message_Fails_To_Resolve_Fcnt_Down_Should_Drop_Message_And_Return_Null()
        {
            const int initialFcntDown = 5;
            const int initialFcntUp = 21;
            const int payloadFcnt = 23;

            var simulatedDevice = new SimulatedDevice(TestDeviceInfo.CreateABPDevice(1, gatewayID: null));
            simulatedDevice.FrmCntUp = initialFcntUp;
            simulatedDevice.FrmCntDown = initialFcntDown;

            var devEUI = simulatedDevice.LoRaDevice.DeviceID;
            var devAddr = simulatedDevice.LoRaDevice.DevAddr;

            // message will be sent
            var loRaDeviceClient = new Mock<ILoRaDeviceClient>(MockBehavior.Strict);
            loRaDeviceClient.Setup(x => x.SendEventAsync(It.IsNotNull<LoRaDeviceTelemetry>(), null))
                .ReturnsAsync(true);

            var cloudToDeviceMessage = new Message();
            cloudToDeviceMessage.Properties.Add("fport", "1");
            // C2D message will be retrieved
            loRaDeviceClient.Setup(x => x.ReceiveAsync(It.IsAny<TimeSpan>()))
                .ReturnsAsync(cloudToDeviceMessage);

            // C2D message will be abandonned
            loRaDeviceClient.Setup(x => x.AbandonAsync(cloudToDeviceMessage))
                .ReturnsAsync(true);

            // Lora device api
            var loRaDeviceApi = new Mock<LoRaDeviceAPIServiceBase>(MockBehavior.Strict);

            // getting the fcnt down will return 0!
            loRaDeviceApi.Setup(x => x.NextFCntDownAsync(devEUI, initialFcntDown, payloadFcnt, this.ServerConfiguration.GatewayID))
                 .ReturnsAsync((ushort)0);

            // using factory to create mock of
            var loRaDeviceFactory = new TestLoRaDeviceFactory(loRaDeviceClient.Object);

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cachedDevice = TestUtils.CreateFromSimulatedDevice(simulatedDevice, loRaDeviceClient.Object);

            var devEUIDeviceDict = new DevEUIToLoRaDeviceDictionary();
            devEUIDeviceDict.TryAdd(devEUI, cachedDevice);
            memoryCache.Set(devAddr, devEUIDeviceDict);

            var deviceRegistry = new LoRaDeviceRegistry(this.ServerConfiguration, memoryCache, loRaDeviceApi.Object, loRaDeviceFactory);

            var frameCounterUpdateStrategyFactory = new LoRaDeviceFrameCounterUpdateStrategyFactory(this.ServerConfiguration.GatewayID, loRaDeviceApi.Object);

            // Send to message processor
            var messageProcessor = new MessageProcessor(
                this.ServerConfiguration,
                deviceRegistry,
                frameCounterUpdateStrategyFactory,
                new LoRaPayloadDecoder());

            // sends unconfirmed message
            var unconfirmedMessagePayload = simulatedDevice.CreateUnconfirmedDataUpMessage("hello", fcnt: payloadFcnt);
            var rxpk = unconfirmedMessagePayload.SerializeUplink(simulatedDevice.AppSKey, simulatedDevice.NwkSKey).Rxpk[0];
            var unconfirmedMessageResult = await messageProcessor.ProcessMessageAsync(rxpk);
            Assert.Null(unconfirmedMessageResult);

            var cachedDevices = deviceRegistry.InternalGetCachedDevicesForDevAddr(simulatedDevice.DevAddr);
            Assert.True(cachedDevices.TryGetValue(devEUI, out var loRaDevice));
            // fcnt down did not change
            Assert.Equal(initialFcntDown, loRaDevice.FCntDown);

            // fcnt up changed
            Assert.Equal(unconfirmedMessagePayload.GetFcnt(), loRaDevice.FCntUp);

            loRaDeviceClient.Verify(x => x.ReceiveAsync(It.IsAny<TimeSpan>()), Times.Once());
            loRaDeviceClient.Verify(x => x.AbandonAsync(It.IsAny<Message>()), Times.Once());

            loRaDeviceClient.VerifyAll();
            loRaDeviceApi.VerifyAll();
        }
    }
}
