// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LoRaTools;
    using LoRaTools.LoRaMessage;
    using LoRaTools.LoRaPhysical;
    using LoRaTools.Regions;
    using LoRaTools.Utils;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class DefaultClassCDevicesMessageSender : IClassCDeviceMessageSender
    {
        private readonly NetworkServerConfiguration configuration;
        private readonly Region loRaRegion;
        private readonly ILoRaDeviceRegistry loRaDeviceRegistry;
        private readonly IPacketForwarder packetForwarder;
        private readonly ILoRaDeviceFrameCounterUpdateStrategyProvider frameCounterUpdateStrategyProvider;

        public DefaultClassCDevicesMessageSender(
            NetworkServerConfiguration configuration,
            Region loRaRegion,
            ILoRaDeviceRegistry loRaDeviceRegistry,
            IPacketForwarder packetForwarder,
            ILoRaDeviceFrameCounterUpdateStrategyProvider frameCounterUpdateStrategyProvider)
        {
            this.configuration = configuration;
            this.loRaRegion = loRaRegion;
            this.loRaDeviceRegistry = loRaDeviceRegistry;
            this.packetForwarder = packetForwarder;
            this.frameCounterUpdateStrategyProvider = frameCounterUpdateStrategyProvider;
        }

        public async Task<bool> SendAsync(ILoRaCloudToDeviceMessage cloudToDeviceMessage, CancellationToken cts = default(CancellationToken))
        {
            try
            {
                if (string.IsNullOrEmpty(cloudToDeviceMessage.DevEUI))
                {
                    Logger.Log($"[C2D] DevEUI missing in payload", LogLevel.Error);
                    return false;
                }

                var loRaDevice = await this.loRaDeviceRegistry.GetDeviceByDevEUIAsync(cloudToDeviceMessage.DevEUI);
                if (loRaDevice == null)
                {
                    Logger.Log(cloudToDeviceMessage.DevEUI, $"[C2D] Device {cloudToDeviceMessage.DevEUI} not found", LogLevel.Error);
                    return false;
                }

                if (cts.IsCancellationRequested)
                {
                    Logger.Log(cloudToDeviceMessage.DevEUI, $"[C2D] Device {cloudToDeviceMessage.DevEUI} timed out, stopping", LogLevel.Error);
                    return false;
                }

                if (string.IsNullOrEmpty(loRaDevice.DevAddr))
                {
                    Logger.Log(loRaDevice.DevEUI, "Device devAddr is empty, cannot send cloud to device message", LogLevel.Information);
                    return false;
                }

                if (loRaDevice.ClassType != LoRaDeviceClassType.C)
                {
                    Logger.Log(loRaDevice.DevEUI, $"Sending cloud to device messages expects a class C device. Class type is {loRaDevice.ClassType}", LogLevel.Information);
                    return false;
                }

                var frameCounterStrategy = this.frameCounterUpdateStrategyProvider.GetStrategy(loRaDevice.GatewayID);
                if (frameCounterStrategy == null)
                {
                    Logger.Log(loRaDevice.DevEUI, $"Could not resolve frame count update strategy for device, gateway id: {loRaDevice.GatewayID}", LogLevel.Information);
                    return false;
                }

                var fcntDown = await frameCounterStrategy.NextFcntDown(loRaDevice, -1);
                if (fcntDown <= 0)
                {
                    Logger.Log(loRaDevice.DevEUI, "Could not obtain fcnt down for class C device", LogLevel.Information);
                    return false;
                }

                var downlink = this.CreateDownlinkMessage(loRaDevice, cloudToDeviceMessage, (ushort)fcntDown);
                await this.packetForwarder.SendDownstreamAsync(downlink);
                await frameCounterStrategy.SaveChangesAsync(loRaDevice);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(cloudToDeviceMessage.DevEUI, $"Error sending class C cloud to device message. {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private DownlinkPktFwdMessage CreateDownlinkMessage(
            LoRaDevice loRaDevice,
            ILoRaCloudToDeviceMessage cloudToDeviceMessage,
            ushort fcntDown)
        {
            // default fport
            byte fctrl = 0;
            byte[] macbytes = null;
            CidEnum macCommandType = CidEnum.Zero;

            byte[] rndToken = new byte[2];
            Random rnd = new Random();
            rnd.NextBytes(rndToken);

            var macCommands = cloudToDeviceMessage.MACCommands;
            if (macCommands != null && macCommands.Count > 0)
            {
                Logger.Log(loRaDevice.DevEUI, $"Cloud to device MAC command received", LogLevel.Information);
                macbytes = macCommands[0].ToBytes();
                macCommandType = macCommands[0].Cid;
            }

            if (cloudToDeviceMessage.Confirmed)
            {
                loRaDevice.LastConfirmedC2DMessageID = cloudToDeviceMessage.MessageId ?? Constants.C2D_MSG_ID_PLACEHOLDER;
            }

            var frmPayload = cloudToDeviceMessage.GetPayload();

            Logger.Log(loRaDevice.DevEUI, $"Sending a downstream message with ID {ConversionHelper.ByteArrayToString(rndToken)}", LogLevel.Debug);
            Logger.Log(loRaDevice.DevEUI, $"C2D message: {Encoding.UTF8.GetString(frmPayload)}, id: {cloudToDeviceMessage.MessageId ?? "undefined"}, fport: {cloudToDeviceMessage.Fport}, confirmed: {cloudToDeviceMessage.Confirmed}, cidType: {macCommandType}", LogLevel.Information);

            // cut to the max payload of lora for any EU datarate
            if (frmPayload.Length > 51)
                Array.Resize(ref frmPayload, 51);

            Array.Reverse(frmPayload);

            var payloadDevAddr = ConversionHelper.StringToByteArray(loRaDevice.DevAddr);
            var reversedDevAddr = new byte[payloadDevAddr.Length];
            for (int i = reversedDevAddr.Length - 1; i >= 0; --i)
            {
                reversedDevAddr[i] = payloadDevAddr[payloadDevAddr.Length - (1 + i)];
            }

            var msgType = cloudToDeviceMessage.Confirmed ? LoRaMessageType.ConfirmedDataDown : LoRaMessageType.UnconfirmedDataDown;
            var ackLoRaMessage = new LoRaPayloadData(
                msgType,
                reversedDevAddr,
                new byte[] { fctrl },
                BitConverter.GetBytes(fcntDown),
                macbytes,
                new byte[] { cloudToDeviceMessage.Fport },
                frmPayload,
                1);

            // Class C uses RX2 always
            string datr = null;
            double freq;
            var tmst = 0; // this.loRaRegion.Receive_delay2 * 1000000;

            var loRaRegionToUse = (this.loRaRegion ?? RegionFactory.CurrentRegion) ?? RegionFactory.CreateEU868Region();

            if (string.IsNullOrEmpty(this.configuration.Rx2DataRate))
            {
                Logger.Log(loRaDevice.DevEUI, $"using standard second receive windows", LogLevel.Information);
                freq = loRaRegionToUse.RX2DefaultReceiveWindows.frequency;
                datr = loRaRegionToUse.DRtoConfiguration[loRaRegionToUse.RX2DefaultReceiveWindows.dr].configuration;
            }

            // if specific twins are set, specify second channel to be as specified
            else
            {
                freq = this.configuration.Rx2DataFrequency;
                datr = this.configuration.Rx2DataRate;
                Logger.Log(loRaDevice.DevEUI, $"using custom DR second receive windows freq : {freq}, datr:{datr}", LogLevel.Information);
            }

            System.Diagnostics.Debug.WriteLine($"Msg down: appSKey: {loRaDevice.AppSKey}, nwkSKey: {loRaDevice.NwkSKey}");

            return ackLoRaMessage.Serialize(loRaDevice.AppSKey, loRaDevice.NwkSKey, datr, freq, tmst, loRaDevice.DevEUI);
        }
    }
}
