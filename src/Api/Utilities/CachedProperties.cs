// -----------------------------------------------------------------------
// <copyright file="CachedProperties.cs" company="iopietro">
// Copyright (c) iopietro. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// -----------------------------------------------------------------------

namespace VoiceChatManager.Api.Utilities
{
    using System;
    using System.Reflection;
    using Dissonance;
    using Dissonance.Audio.Playback;
    using Dissonance.Integrations.MirrorIgnorance;
    using UnityEngine;

    /// <summary>
    /// A set of cached properties.
    /// </summary>
    public static class CachedProperties
    {
        private static MirrorIgnoranceCommsNetwork commsNetwork;
        private static DissonanceComms dissonanceComms;
        private static FieldInfo maximumVoiceChatDesync;

        /// <summary>
        /// Gets the cached mirror ignorance comms network.
        /// </summary>
        public static MirrorIgnoranceCommsNetwork CommsNetwork
        {
            get
            {
                if (commsNetwork == null)
                    commsNetwork = GameObject.FindObjectOfType<MirrorIgnoranceCommsNetwork>();

                return commsNetwork;
            }
        }

        /// <summary>
        /// Gets the cached dissonance comms.
        /// </summary>
        public static DissonanceComms DissonanceComms
        {
            get
            {
                if (dissonanceComms == null)
                    dissonanceComms = GameObject.FindObjectOfType<DissonanceComms>();

                return dissonanceComms;
            }
        }

        /// <summary>
        /// Gets or sets the maximum tolerable desync by the decoder pipeline.
        /// </summary>
        public static TimeSpan MaximumVoiceChatDesync
        {
            get => SamplePlaybackComponent.ResetDesync;
            set
            {
                if (maximumVoiceChatDesync == null)
                    maximumVoiceChatDesync = typeof(SamplePlaybackComponent).GetField(nameof(SamplePlaybackComponent.ResetDesync), BindingFlags.Static | BindingFlags.NonPublic);

                maximumVoiceChatDesync.SetValue(null, value);
            }
        }
    }
}
