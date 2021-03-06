﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools.Regions
{
    using System.Collections.Generic;

    public class RegionLimits
    {
        public (double min, double max) FrequencyRange { get; set; }

        public List<string> DatarateRange { get; set; }

        public RegionLimits((double min, double max) frequencyRange, List<string> datarateRange)
        {
            this.FrequencyRange = frequencyRange;
            this.DatarateRange = datarateRange;
        }
    }
}
