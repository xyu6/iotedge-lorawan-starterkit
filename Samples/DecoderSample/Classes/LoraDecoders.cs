// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SensorDecoderModule.Classes
{
    using System.Collections.Generic;
    using System.Text;
    using Newtonsoft.Json;

    internal static class LoraDecoders
    {
        private static string DecoderValueSensor(byte[] payload, uint fport)
        {
            // EITHER: Convert a payload containing a string back to string format for further processing
            var result = Encoding.UTF8.GetString(payload);

            // OR: Convert a payload containing binary data to HEX string for further processing
            var result_binary = ConversionHelper.ByteArrayToString(payload);

            // Write code that decodes the payload here.

            // Return a JSON string containing the decoded data
            return JsonConvert.SerializeObject(new ValueSensorResponse { value = result });
        }

        private static string DecoderValueSensorWithDeviceMessage(byte[] payload, uint fport)
        {
            // Decode payload
            var decodedMessage = new ValueSensorResponse
            {
                value = Encoding.UTF8.GetString(payload)
            };

            // Create Decoder 2 Device message
            var deviceMessage = new DeviceMessage
            {
                devEUI = null,
                fport = fport,
                confirmed = false,
                data = ConversionHelper.Base64Encode("message to device"),
                data_string = "message to device",
            };
            deviceMessage.macCommands.Add(new MacCommand { cid = 1 });

            // Return a JSON string containing the decoded data
            return ResponseHelper.BuildResponse(decodedMessage, deviceMessage);
        }
    }

    public class ValueSensorResponse
    {
        public string value { get; set; }
    }
}
