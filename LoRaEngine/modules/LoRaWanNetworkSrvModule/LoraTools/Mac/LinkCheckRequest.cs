// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// LinkCheckReq Upstream
    /// </summary>
    public class LinkCheckRequest : MacCommand
    {
        uint Margin { get; set; }

        uint GwCnt { get; set; }

        public override int Length => 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkCheckRequest"/> class.
        /// Downstream Constructor
        /// </summary>
        public LinkCheckRequest(uint margin, uint gwCnt)
        {
            this.Cid = CidEnum.LinkCheckCmd;
            this.Margin = margin;
            this.GwCnt = gwCnt;
        }

        public override IEnumerable<byte> ToBytes()
        {
            List<byte> returnedBytes = new List<byte>();
            returnedBytes[0] = (byte)this.Cid;
            returnedBytes[1] = BitConverter.GetBytes(this.Margin)[0];
            returnedBytes[2] = BitConverter.GetBytes(this.GwCnt)[0];
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
