﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;

    /// <summary>
    /// Both ways
    /// </summary>
    public class NewChannelRequest : MacCommand
    {
        private readonly byte chIndex;

        private readonly byte[] freq;

        private readonly byte drRange;

        public override int Length => 6;

        public NewChannelRequest(byte chIndex, byte[] freq, byte maxDr, byte minDr)
        {
            this.chIndex = chIndex;
            this.freq = freq;
            this.drRange = (byte)((byte)(maxDr << 4) | (minDr & 0b00001111));
            this.Cid = CidEnum.NewChannelCmd;
        }

        public override byte[] ToBytes()
        {
            byte[] returnedBytes = new byte[this.Length];
            returnedBytes[0] = (byte)this.Cid;
            returnedBytes[1] = (byte)this.chIndex;
            returnedBytes[2] = (byte)this.freq[0];
            returnedBytes[3] = (byte)this.freq[1];
            returnedBytes[4] = (byte)this.freq[2];
            returnedBytes[5] = (byte)this.drRange;
            return returnedBytes;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
