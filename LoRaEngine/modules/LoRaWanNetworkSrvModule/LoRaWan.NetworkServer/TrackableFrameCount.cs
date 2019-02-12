// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    /// <summary>
    /// Defines a trackable, thread-safe frame count, which keep tracks of last saved value
    /// </summary>
    public class TrackableFrameCount
    {
        readonly object sync;

        public int Value { get; private set; }

        public int LastSavedValue { get; private set; }

        public TrackableFrameCount()
        {
            this.sync = new object();
        }

        public TrackableFrameCount(int value)
        {
            this.sync = new object();
            this.Value = value;
            this.LastSavedValue = value;
        }

        public void Set(int newValue)
        {
            lock (this.sync)
            {
                this.Value = newValue;
            }
        }

        public int Increment(int value)
        {
            lock (this.sync)
            {
                this.Value += value;
                return this.Value;
            }
        }

        /// <summary>
        /// Accept currently pending changes, setting the <see cref="LastSavedValue"/> to <see cref="Value"/>
        /// </summary>
        public void AcceptChanges()
        {
            lock (this.sync)
            {
                this.LastSavedValue = this.Value;
            }
        }

        /// <summary>
        /// Gets if there are changes to be saved
        /// </summary>
        public bool HasChanges()
        {
            lock (this.sync)
            {
                return this.Value != this.LastSavedValue;
            }
        }
    }
}