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

    public class DeviceMessage
    {
        public string devEUI { get; set; }

        public uint fport { get; set; }

        public bool confirmed { get; set; }

        public string data { get; set; }

        public string data_string { get; set; }

        public List<MacCommand>  macCommands { get; set; }

        public DeviceMessage()
        {
            this.macCommands = new List<MacCommand>();
        }
    }

    public class MacCommand
    {
        public int cid { get; set; }
    }
}
