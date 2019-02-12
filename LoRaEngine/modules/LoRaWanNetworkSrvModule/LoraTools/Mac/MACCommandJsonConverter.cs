// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Defines a <see cref="JsonConverter"/> capable of converting a JSON list of elements to concrete <see cref="GenericMACCommand"/> objects
    /// </summary>
    public class MACCommandJsonConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return typeof(GenericMACCommand).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            switch (item["cid"].Value<int>())
            {
                case (int)CidEnum.LinkCheckCmd:
                {
                    var t = new LinkCheckCmd();
                    serializer.Populate(item.CreateReader(), t);
                    return t;
                }

                case (int)CidEnum.DevStatusCmd:
                {
                    var t = new DevStatusCmd();
                    serializer.Populate(item.CreateReader(), t);
                    return t;
                }

                case (int)CidEnum.DutyCycleCmd:
                {
                    var t = new DutyCycleCmd();
                    serializer.Populate(item.CreateReader(), t);
                    return t;
                }

                case (int)CidEnum.NewChannelCmd:
                {
                    var t = new NewChannelReq();
                    serializer.Populate(item.CreateReader(), t);
                    return t;
                }
            }

            return null;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
