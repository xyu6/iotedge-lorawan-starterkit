// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;

    public class NewChannelAnswer : MacCommand
    {
        private readonly byte status;

        public NewChannelAnswer(bool drRangeOk, bool chanFreqOk)
        {
            this.status |= (byte)((drRangeOk ? 1 : 0) << 2);
            this.status |= (byte)(chanFreqOk ? 1 : 0);
            this.Cid = CidEnum.NewChannelCmd;
        }

        public NewChannelAnswer(ReadOnlySpan<byte> readOnlySpan)
        {
            if (readOnlySpan.Length < this.Length)
            {
                throw new Exception("NewChannelAnswer detected but the byte format is not correct");
            }
            else
            {
                this.status = readOnlySpan[1];
                this.Cid = (CidEnum)readOnlySpan[0];
            }
        }

        public override int Length => 2;

        public override byte[] ToBytes()
        {
            byte[] returnedBytes = new byte[this.Length];
            returnedBytes[0] = (byte)this.Cid;
            returnedBytes[1] = (byte)this.status;
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
