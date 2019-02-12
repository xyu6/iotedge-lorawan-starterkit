// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SensorDecoderModule.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class ResponseHelper
    {
        public static string BuildResponse(object decodedMessage, DeviceMessage deviceMessage)
        {
            JObject jobject = new JObject();
            jobject["decoded_message"] = (JObject)JToken.FromObject(decodedMessage);
            jobject["device_message"] = (JObject)JToken.FromObject(deviceMessage);
            return JsonConvert.SerializeObject(jobject);
        }
    }

    public class DecoderResponse
    {
        public object DecodedMessage { get; set; }

        public DeviceMessage DeviceMessage { get; set; }
    }
           
    public class DeviceMessage
    {
        public string DevEUI { get; set; }

        public uint Fport { get; set; }

        public bool Confirmed { get; set; }

        public string Data { get; set; }

        public string Data_string { get; set; }

        public List<MacCommand>  MacCommands { get; set; }

        public DeviceMessage()
        {
            this.MacCommands = new List<MacCommand>();
        }
    }

    public class MacCommand
    {
        public int Cid { get; set; }
        public string Params { get; set; }
    }
}
