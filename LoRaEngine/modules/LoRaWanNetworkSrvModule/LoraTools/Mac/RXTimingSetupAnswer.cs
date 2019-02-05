// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;

    /// <summary>
    /// RXTimingSetupAns Upstream & RXTimingSetupReq Downstream
    /// </summary>
    public class RXTimingSetupAnswer : MacCommand
    {
        public override int Length => 1;

        public RXTimingSetupAnswer()
        {
            this.Cid = CidEnum.RXTimingCmd;
        }

        public override byte[] ToBytes()
        {
            byte[] returnedBytes = new byte[this.Length];
            returnedBytes[0] = (byte)this.Cid;
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
