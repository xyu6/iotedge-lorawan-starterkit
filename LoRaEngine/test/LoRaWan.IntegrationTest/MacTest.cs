// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.IntegrationTest
{
    using System;
    using System.Threading.Tasks;
    using System.Xml;
    using LoRaWan.Test.Shared;
    using Newtonsoft.Json.Linq;
    using Xunit;

    // Tests OTAA requests
    [Collection(Constants.TestCollectionName)] // run in serial
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public sealed class MacTest : IntegrationTestBaseCi
    {
        public MacTest(IntegrationTestFixtureCi testFixture)
            : base(testFixture)
        {
        }

        // Send a LinkCheckCmd from the device and expect an answer.
        [Fact]
        public async Task Test_Mac_LinkCheckCmd_Should_work()
        {
            var device = this.TestFixtureCi.Device22_ABP;
            this.LogTestStart(device);

            await this.ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWABP);
            await this.ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, null);
            await this.ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, null);
            await this.ArduinoDevice.setPortAsync(0);

            await this.ArduinoDevice.SetupLora(this.TestFixtureCi.Configuration.LoraRegion);

            var msg = "02";
            this.Log($"{device.DeviceID}: Sending unconfirmed Mac LinkCheckCmd message");
            await this.ArduinoDevice.transferHexPacketAsync(msg, 10);

            await Task.Delay(Constants.DELAY_BETWEEN_MESSAGES);

                // After transferPacket: Expectation from serial
                // +MSG: Done
            await AssertUtils.ContainsWithRetriesAsync("+MSGHEX: Done", this.ArduinoDevice.SerialLogs);

            await this.TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"{device.DeviceID}: valid frame counter, msg:");

            await this.TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"LinkCheckCmd mac command detected in upstream payload:");

            await AssertUtils.ContainsWithRetriesAsync("MSGHEX: RXWIN", this.ArduinoDevice.SerialLogs);
            this.TestFixtureCi.ClearLogs();
            await this.ArduinoDevice.setPortAsync(1);
        }

        // [Fact]
        // public async Task Test_Mac_Unknown_Should_Fail()
        // {
        // }
    }
}