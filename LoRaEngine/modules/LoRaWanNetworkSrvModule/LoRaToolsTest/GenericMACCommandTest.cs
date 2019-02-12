// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWanTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LoRaTools;
    using Newtonsoft.Json;
    using Xunit;

    public class GenericMACCommandTest
    {
        public class Holder
        {
            // [JsonConverter(typeof(GenericMACCommand))]
            public List<GenericMACCommand> Commands { get; set; }
        }

        [Fact]
        public void When_Serializing_List_Should_Create_Correct_Items()
        {
            var input = @"[ { ""cid"": 2, ""Margin"": 12, ""GwCnt"": 10 }, { ""cid"": 6, ""Battery"": 1, ""Margin"": 10 } ]";
            var list = JsonConvert.DeserializeObject<List<GenericMACCommand>>(input);
            Assert.Equal(2, list.Count);
            Assert.IsType<LinkCheckCmd>(list[0]);
            Assert.IsType<DevStatusCmd>(list[1]);

            var linkCheckCmd = (LinkCheckCmd)list[0];
            Assert.Equal(12U, linkCheckCmd.Margin);
            Assert.Equal(10U, linkCheckCmd.GwCnt);

            var devStatus = (DevStatusCmd)list[1];
            Assert.Equal(1U, devStatus.Battery);
            Assert.Equal(10, devStatus.Margin);
        }

        [Fact]
        public void When_Serializing_Single_Object_Should_Create_Correct_Items()
        {
            var input = @"{ ""cid"": 2, ""Margin"": 12, ""GwCnt"": 10 }";
            var genericMACCommand = JsonConvert.DeserializeObject<GenericMACCommand>(input);
            Assert.NotNull(genericMACCommand);
            Assert.IsType<LinkCheckCmd>(genericMACCommand);
            var linkCheckCmd = (LinkCheckCmd)genericMACCommand;
            Assert.Equal(12U, linkCheckCmd.Margin);
            Assert.Equal(10U, linkCheckCmd.GwCnt);
        }
    }
}
