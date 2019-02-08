// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// LoRa message contract
    /// </summary>
    interface ILoRaMessage
    {
        /// <summary>
        /// Complete a cloud to device message
        /// </summary>
        Task<bool> CompleteAsync(LoRaDevice loRaDevice);

        /// <summary>
        /// Abandon a cloud to device message
        /// </summary>
        Task<bool> AbandonAsync(LoRaDevice loRaDevice);
    }
}
