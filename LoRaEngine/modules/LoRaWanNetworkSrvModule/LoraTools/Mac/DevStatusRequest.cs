// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    /// <summary>
    /// DevStatusAns Upstream & DevStatusReq Downstream
    /// </summary>
    public class DevStatusRequest : MacCommand
    {
        public override int Length => 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevStatusRequest"/> class.
        /// Upstream Constructor
        /// </summary>
        public DevStatusRequest()
        {
            this.Cid = CidEnum.DevStatusCmd;
        }

        public override string ToString()
        {
            return string.Empty;
        }

        public override byte[] ToBytes()
        {
            byte[] returnedBytes = new byte[this.Length];
            returnedBytes[0] = (byte)this.Cid;
            return returnedBytes;
        }
    }
}
