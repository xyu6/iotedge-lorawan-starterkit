// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;

    /// <summary>
    /// RXTimingSetupAns Upstream & RXTimingSetupReq Downstream
    /// </summary>
    public class RXTimingSetupRequest : MacCommand
    {
        private readonly byte settings;

        public override int Length => 2;

        public RXTimingSetupRequest(byte delay)
        {
            this.Cid = CidEnum.RXTimingCmd;
            this.settings |= delay;
        }

        public override byte[] ToBytes()
        {
            byte[] returnedBytes = new byte[this.Length];
            returnedBytes[0] = (byte)this.Cid;
            returnedBytes[1] = (byte)this.settings;
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
