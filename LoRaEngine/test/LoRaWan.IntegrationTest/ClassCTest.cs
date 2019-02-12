// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.IntegrationTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using LoRaWan.Test.Shared;
    using Microsoft.Azure.Devices;
    using Newtonsoft.Json;
    using Xunit;

    // Tests Cloud to Device messages
    [Collection(Constants.TestCollectionName)] // run in serial
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public sealed class ClassCTest : IntegrationTestBaseCi
    {
        public ClassCTest(IntegrationTestFixtureCi testFixture)
            : base(testFixture)
        {
        }

        // Ensures that class C devices can receive messages from a direct method call
        // Uses Device22_ABP
        [Fact]
        public async Task Test_ClassC_Send_Message_From_Direct_Method_Should_Be_Received()
        {
            var device = this.TestFixtureCi.Device22_ABP;
            this.LogTestStart(device);

            await this.ArduinoDevice.setDeviceResetAsync();
            await this.ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWABP);
            await this.ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, null);
            await this.ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, null);
            await this.ArduinoDevice.SetupLora(this.TestFixtureCi.Configuration.LoraRegion);
            await this.ArduinoDevice.setClassTypeAsync(LoRaArduinoSerial._class_type_t.CLASS_C);

            var c2d = new TestLoRaCloudToDeviceMessage()
            {
                DevEUI = device.DeviceID,
                MessageId = Guid.NewGuid().ToString(),
                Fport = 23,
                Payload = "44",
            };

            var moduleClient = await this.TestFixtureCi.GetModuleClientAsync();
            if (moduleClient != null)
            {
                TestLogger.Log("[INFO] Using module client to call direct method");
                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(c2d));
                var method = new Microsoft.Azure.Devices.Client.MethodRequest("cloudtodevicemessage", data, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                await moduleClient.InvokeMethodAsync(this.TestFixture.Configuration.LeafDeviceGatewayID, this.TestFixture.Configuration.NetworkServerModuleID, method);
            }
            else
            {
                TestLogger.Log("[INFO] Using service client to call direct method");
                await this.TestFixtureCi.InvokeModuleDirectMethodAsync(this.TestFixture.Configuration.LeafDeviceGatewayID, this.TestFixture.Configuration.NetworkServerModuleID, "cloudtodevicemessage", c2d);
            }

            await Task.Delay(Constants.DELAY_BETWEEN_MESSAGES);

            Assert.Contains(this.ArduinoDevice.SerialLogs, (l) => l.StartsWith("+MSG: PORT: 23; RX: "));
            Assert.Contains(this.ArduinoDevice.SerialLogs, (l) => l.StartsWith("+MSG: RXWIN0, RSSI"));
            await AssertUtils.ContainsWithRetriesAsync("+MSG: Done", this.ArduinoDevice.SerialLogs);
        }
    }
}