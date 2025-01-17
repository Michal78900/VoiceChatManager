// -----------------------------------------------------------------------
// <copyright file="AudiosCommand.cs" company="iopietro">
// Copyright (c) iopietro. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// -----------------------------------------------------------------------

namespace VoiceChatManager.Commands.List
{
    using System;
    using System.IO;
    using Api.Audio.Playback;
    using Api.Extensions;
    using CommandSystem;
    using Exiled.Permissions.Extensions;
    using NorthwoodLib.Pools;

    /// <inheritdoc/>
    internal class AudiosCommand : ICommand
    {
        private AudiosCommand()
        {
        }

        /// <summary>
        /// Gets the command instance.
        /// </summary>
        public static AudiosCommand Instance { get; } = new AudiosCommand();

        /// <inheritdoc/>
        public string Command { get; } = "audios";

        /// <inheritdoc/>
        public string[] Aliases { get; } = { "audio", "a", "au" };

        /// <inheritdoc/>
        public string Description { get; } = "Gets the list of playing/paused/stopped audios.";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count != 0)
            {
                response = "voicechatmanager list audio";
                return false;
            }

            if (!sender.CheckPermission("voicechatmanager.list.audio"))
            {
                response = "Not enough permissions to run this command!\nRequired: voicechatmanager.list.audio";
                return false;
            }

            var message = StringBuilderPool.Shared.Rent().AppendLine().Append("[Audios list (").Append(StreamedMicrophone.List.Count).AppendLine(")]").AppendLine();

            if (StreamedMicrophone.List.Count > 0)
            {
                var i = 0;

                foreach (var streamedMicrophone in StreamedMicrophone.List)
                {
                    message.Append("[").Append(i++).Append(". ");

                    if (streamedMicrophone.Stream is FileStream fileStream)
                        message.Append(fileStream.Name);

                    message.AppendLine("]")
                        .Append("Status: ").AppendLine(streamedMicrophone.Status.ToString())
                        .Append("Channel name: ").AppendLine(streamedMicrophone.ChannelName.ToString())
                        .Append("Volume: ").Append(streamedMicrophone.Volume).Append('/').Append(VoiceChatManager.Instance.Config.VolumeLimit.ToString()).Append(" (").Append(streamedMicrophone.PercentageVolume).AppendLine("%)")
                        .Append("Duration: ").AppendLine(streamedMicrophone.Duration.ToString(VoiceChatManager.Instance.Config.DurationFormat))
                        .Append("Progression: ").Append(streamedMicrophone.Progression.ToString(VoiceChatManager.Instance.Config.DurationFormat)).Append(" (").Append(streamedMicrophone.PercentageProgression).AppendLine("%)")
                        .Append("Size: ").Append(streamedMicrophone.Size.FromBytesToMegaBytes()).AppendLine(" MB");

                    if (streamedMicrophone is IProximityStreamedMicrophone proximityStreamedMicrophone)
                        message.Append("Position: ").AppendLine(proximityStreamedMicrophone.Position.ToString());

                    if (streamedMicrophone is IPlayerProximityStreamedMicrophone playerProximityStreamedMicrophone)
                        message.Append("Player: ").AppendLine(playerProximityStreamedMicrophone.Player.ToString());

                    message.AppendLine();
                }
            }
            else
            {
                message.Append("There are no audios.");
            }

            response = StringBuilderPool.Shared.ToStringReturn(message);
            return true;
        }
    }
}
