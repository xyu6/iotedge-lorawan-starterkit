// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;

    public class LoRaMessage : ILoRaMessage
    {
        public Message Message { get; set; }

        public string DecoderUrl { get; set; }

        public LoRaMessage()
        {
        }

        public void GetMessageFromString(string messageBody)
        {
            this.Message = new Message(Encoding.UTF8.GetBytes(messageBody));
        }

        public Task<bool> AbandonAsync(LoRaDevice loRaDevice)
        {
            return loRaDevice.AbandonCloudToDeviceMessageAsync(this.Message);
        }

        public Task<bool> CompleteAsync(LoRaDevice loRaDevice)
        {
            return loRaDevice.CompleteCloudToDeviceMessageAsync(this.Message);
        }
    }
}
