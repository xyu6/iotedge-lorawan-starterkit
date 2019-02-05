// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;

    /// <summary>
    ///  DutyCycleReq Downstream
    /// </summary>
    public class DutyCycleRequest : MacCommand
    {
        private readonly byte dutyCyclePL;

        public override int Length => 2;

        // Downstream message
        public DutyCycleRequest(byte dutyCyclePL)
        {
            this.Cid = CidEnum.DutyCycleCmd;
            this.dutyCyclePL = (byte)(dutyCyclePL & 0b00001111);
        }

        public override byte[] ToBytes()
        {
            byte[] returnedBytes = new byte[this.Length];
            returnedBytes[0] = (byte)this.Cid;
            returnedBytes[1] = (byte)this.dutyCyclePL;
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
