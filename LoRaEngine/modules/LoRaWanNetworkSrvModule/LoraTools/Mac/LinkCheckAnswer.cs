// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// LinkCheckAns Downstream
    /// </summary>
    public class LinkCheckAnswer : MacCommand
    {
        uint Margin { get; set; }

        uint GwCnt { get; set; }

        public override int Length => 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkCheckAnswer"/> class.
        /// Upstream Constructor
        /// </summary>
        public LinkCheckAnswer(uint margin, uint gwCnt)
        {
            this.Margin = margin;
            this.GwCnt = gwCnt;
            this.Cid = CidEnum.LinkCheckCmd;
        }

        public override IEnumerable<byte> ToBytes()
        {
            List<byte> returnedBytes = new List<byte>();
            returnedBytes[0] = (byte)this.Cid;
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
