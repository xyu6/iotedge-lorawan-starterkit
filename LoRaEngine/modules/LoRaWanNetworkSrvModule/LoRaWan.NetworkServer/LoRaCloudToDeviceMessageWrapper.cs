// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using LoRaTools;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    class LoRaCloudToDeviceMessageWrapper : ILoRaCloudToDeviceMessage
    {
        private readonly LoRaDevice loRaDevice;
        private readonly Message message;
        private LoRaCloudToDeviceMessage parseCloudToDeviceMessage;

        public LoRaCloudToDeviceMessageWrapper(LoRaDevice loRaDevice, Message message)
        {
            this.loRaDevice = loRaDevice;
            this.message = message;

            this.ParseMessage();
        }

        /// <summary>
        /// Tries to parse the <see cref="Message.GetBytes"/> to a json representation of <see cref="LoRaCloudToDeviceMessage"/>
        /// </summary>
        private void ParseMessage()
        {
            string json = string.Empty;
            var bytes = this.message.GetBytes();
            if (bytes?.Length > 0)
            {
                json = Encoding.UTF8.GetString(bytes);
                try
                {
                    this.parseCloudToDeviceMessage = JsonConvert.DeserializeObject<LoRaCloudToDeviceMessage>(json);
                }
                catch (JsonReaderException)
                {
                    Logger.Log(this.loRaDevice.DevEUI, $"Could not parse cloud to device message: {json}", LogLevel.Error);
                }
            }
        }

        public byte Fport
        {
            get
            {
                if (this.parseCloudToDeviceMessage != null)
                    return this.parseCloudToDeviceMessage.Fport;

                return 0;
            }
        }

        public bool Confirmed
        {
            get
            {
                if (this.parseCloudToDeviceMessage != null)
                    return this.parseCloudToDeviceMessage.Confirmed;

                return false;
            }
        }

        public string MessageId => this.parseCloudToDeviceMessage?.MessageId ?? this.message.MessageId;

        public string DevEUI => this.loRaDevice.DevEUI;

        public byte[] GetPayload()
        {
            if (this.parseCloudToDeviceMessage != null)
                return this.parseCloudToDeviceMessage.GetPayload();

            return new byte[0];
        }

        public IList<GenericMACCommand> MACCommands
        {
            get
            {
                if (this.parseCloudToDeviceMessage != null)
                {
                    return this.parseCloudToDeviceMessage.MACCommands;
                }

                return null;
            }
        }

        public Task<bool> CompleteAsync() => this.loRaDevice.CompleteCloudToDeviceMessageAsync(this.message);

        public Task<bool> AbandonAsync() => this.loRaDevice.AbandonCloudToDeviceMessageAsync(this.message);
    }
}
