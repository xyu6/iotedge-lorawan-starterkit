// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SensorDecoderModule.Classes
{
    using System.Collections.Generic;
    using System.Text;
    using Newtonsoft.Json;

    internal static class LoraDecoders
    {
        private static string DecoderValueSensor(byte[] payload, uint fport, string devEui)
        {
            // EITHER: Convert a payload containing a string back to string format for further processing
            var result = Encoding.UTF8.GetString(payload);

            // OR: Convert a payload containing binary data to HEX string for further processing
            var result_binary = ConversionHelper.ByteArrayToString(payload);

            // Write code that decodes the payload here.

            // Return a JSON string containing the decoded data. This is your message structure. Object sample code below.
            return JsonConvert.SerializeObject(new ValueSensorResponse { Value = result });
        }

        private static string DecoderValueSensorWithDeviceMessage(byte[] payload, uint fport, string devEui)
        {
            // Create DecoderResponse object
            DecoderResponse decoderResponse = new DecoderResponse();

            // Decode payload. This is your message structure. Object sample code below.
            var decodedMessage = new ValueSensorResponse
            {
                Value = Encoding.UTF8.GetString(payload)
            };

            decoderResponse.DecodedMessage = decodedMessage;

            // Create Decoder 2 Device message.
            // If DevEUI is empty, the response will be sent to the device that sent the upsteam message that we are decoding.
            var deviceMessage = new DeviceMessage
            {
                DevEUI = devEui,
                Fport = fport,
                Confirmed = false,
                Data = ConversionHelper.Base64Encode("message to device"),
                Data_string = "message to device",
            };

            deviceMessage.MacCommands.Add(new MacCommand { Cid = 1, Params = "Sample Params" });
            decoderResponse.DeviceMessage = deviceMessage;
                       
            // Return a JSON string containing the Decoder Response object
            return JsonConvert.SerializeObject(decoderResponse);
        }
    }

    public class ValueSensorResponse
    {
        public string Value { get; set; }
    }
}
