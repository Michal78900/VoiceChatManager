// -----------------------------------------------------------------------
// <copyright file="ServerHandler.cs" company="iopietro">
// Copyright (c) iopietro. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// -----------------------------------------------------------------------

namespace VoiceChatManager.Events
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Audio.Capture;
    using Api.Audio.Playback;
    using Api.Extensions;
    using Dissonance;
    using Dissonance.Audio.Playback;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs;
    using Mirror;
    using NAudio.Wave;
    using UnityEngine;
    using Xabe.FFmpeg;
    using static VoiceChatManager;
    using Log = Exiled.API.Features.Log;

    /// <summary>
    /// Handles server-related events.
    /// </summary>
    internal sealed class ServerHandler
    {
        /// <summary>
        /// Gets the actual round name.
        /// </summary>
        public string RoundName { get; private set; }

        /// <summary>
        /// Gets the round paths queue.
        /// </summary>
        internal ConcurrentQueue<string> RoundPaths { get; private set; } = new ConcurrentQueue<string>();

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.OnReloadedConfigs"/>
        public void OnReloadedConfigs()
        {
            if (Directory.Exists(Instance.Config.FFmpegDirectoryPath))
            {
                FFmpeg.SetExecutablesPath(Instance.Config.FFmpegDirectoryPath);
            }
            else if (Instance.Config.Converter.IsEnabled)
            {
                Log.Warn($"Audio converter cannot be enabled, FFmpeg wasn't found at \"{Instance.Config.FFmpegDirectoryPath}\"");

                Instance.Config.Converter.IsEnabled = false;
            }

            Instance.Gdpr.Load();

            if (Instance.Config.Converter.IsEnabled)
            {
                if (Instance.ConverterCancellationTokenSource == null)
                {
                    Instance.ConverterCancellationTokenSource = new CancellationTokenSource();
                    Instance.Converter = new AudioConverter(
                                new WaveFormat(Instance.Config.Converter.SampleRate, Instance.Config.Converter.Channels),
                                Instance.Config.Converter.FileFormat,
                                Instance.Config.Converter.Speed,
                                Instance.Config.Converter.Bitrate,
                                Instance.Config.Converter.ShouldDeleteAfterConversion,
                                Instance.Config.Converter.Preset,
                                Instance.Config.Converter.ConcurrentLimit,
                                Instance.Config.Converter.Interval);

                    Task.Run(() => Instance.Converter.StartAsync(Instance.ConverterCancellationTokenSource.Token)).ConfigureAwait(false);
                }
            }
            else
            {
                Instance.ConverterCancellationTokenSource?.Cancel();
                Instance.ConverterCancellationTokenSource?.Dispose();
                Instance.ConverterCancellationTokenSource = null;

                Instance.Converter?.Queue.Clear();
                Instance.Converter = null;
            }

            if (Instance.Config.Recorder.IsEnabled)
            {
                RoundPaths.Clear();

                if (Directory.Exists(Instance.Config.Recorder.RootDirectoryPath))
                {
                    foreach (var directory in new DirectoryInfo(Instance.Config.Recorder.RootDirectoryPath).GetDirectories().OrderBy(info => info.CreationTime))
                        RoundPaths.Enqueue(directory.FullName);
                }

                if (Instance.CaptureCancellationTokenSource == null)
                {
                    Instance.CaptureCancellationTokenSource = new CancellationTokenSource();
                    Instance.Capture = new VoiceChatCapture(
                        new WaveFormat(Instance.Config.Recorder.SampleRate, 1),
                        Instance.Config.Recorder.ReadBufferSize,
                        Instance.Config.Recorder.ReadInterval);

                    Task.Run(() => Instance.Capture.StartAsync(Instance.CaptureCancellationTokenSource.Token)).ConfigureAwait(false);
                }

                foreach (var player in Player.List)
                {
                    if (!Instance.Gdpr.IsCompliant ||
                        (Instance.Gdpr.IsCompliant && (Instance.Gdpr.CanBeVoiceRecordedPlayerUserIds?.Contains(player.UserId) ?? false)))
                    {
                        player.SessionVariables["canBeVoiceRecorded"] = true;

                        var voiceChatRecorder = new VoiceChatRecorder(
                            player,
                            new WaveFormat(Instance.Config.Recorder.SampleRate, 1),
                            Path.Combine(Instance.Config.Recorder.RootDirectoryPath, RoundName),
                            Instance.Config.Recorder.DateTimeFormat,
                            Instance.Config.Recorder.MinimumBytesToWrite,
                            Instance.Converter);

                        if (!player.TryGet(out SamplePlaybackComponent playbackComponent)
                            || (!Instance.Capture?.Recorders.TryAdd(playbackComponent, voiceChatRecorder) ?? true))
                        {
                            Log.Debug($"Failed to add {player} ({player.UserId}) to the list of voice recorded players!", Instance.Config.IsDebugEnabled);
                            continue;
                        }

                        playbackComponent.MultiplyBySource = false;
                    }
                    else
                    {
                        IVoiceChatRecorder voiceChatRecorder = null;

                        if (!player.TryGet(out SamplePlaybackComponent playbackComponent)
                            || (!Instance.Capture?.Recorders.TryRemove(playbackComponent, out voiceChatRecorder) ?? true))
                        {
                            Log.Debug($"Failed to remove {player} ({player.UserId}) from the list of voice recorded players!", Instance.Config.IsDebugEnabled);
                            continue;
                        }

                        voiceChatRecorder?.Dispose();

                        player.SessionVariables.Remove("canBeVoiceRecorded");
                    }
                }
            }
            else
            {
                RoundPaths.Clear();

                Instance.CaptureCancellationTokenSource?.Cancel();
                Instance.CaptureCancellationTokenSource?.Dispose();
                Instance.CaptureCancellationTokenSource = null;

                Instance.Capture?.Dispose();
                Instance.Capture = null;
            }
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.OnWaitingForPlayers"/>
        public void OnWaitingForPlayers()
        {
            InitHost();

            // It doesn't get invoked by Exiled
            if (Exiled.Events.Events.Instance.Config.ShouldReloadConfigsAtRoundRestart)
                OnReloadedConfigs();

            RoundName = $"Round {DateTime.Now.ToString(Instance.Config.Recorder.DateTimeFormat)}";

            if (Instance.Config.Recorder.IsEnabled && Instance.Config.Recorder.KeepLastNumberOfRounds > 0)
            {
                Task.Run(() =>
                {
                    while (RoundPaths.Count >= Instance.Config.Recorder.KeepLastNumberOfRounds && RoundPaths.TryDequeue(out var path) && Directory.Exists(path))
                        Directory.Delete(path, true);

                    RoundPaths.Enqueue(Path.Combine(Instance.Config.Recorder.RootDirectoryPath, RoundName));
                }).ConfigureAwait(false);
            }

            Instance.Config.PlayOnEvent.WaitingForPlayers.Play();
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.OnRestartingRound"/>
        public void OnRestartingRound()
        {
            foreach (var streamedMicrophone in StreamedMicrophone.List)
                streamedMicrophone.Dispose();

            StreamedMicrophone.List.Clear();

            Instance.Capture?.Clear();
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.OnRoundStarted"/>
        public void OnRoundStarted() => Instance.Config.PlayOnEvent.RoundStarted.Play();

        /// <inheritdoc cref="Exiled.Events.Handlers.Map.OnAnnouncingNtfEntrance(AnnouncingNtfEntranceEventArgs)"/>
        public void OnAnnouncingNtfEntrance(AnnouncingNtfEntranceEventArgs ev)
        {
            if (!string.IsNullOrEmpty(Instance.Config.PlayOnEvent.NtfEntrance.Name))
            {
                ev.IsAllowed = false;
                Instance.Config.PlayOnEvent.NtfEntrance.Play();
            }
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.OnRespawningTeam(RespawningTeamEventArgs)"/>
        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            if (ev.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency)
            {
                Instance.Config.PlayOnEvent.CiEntrance.Play();
            }
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Warhead.OnStarting(StartingEventArgs)"/>
        public void OnWarheadStarting(StartingEventArgs ev) => Instance.Config.PlayOnEvent.WarheadStart.Play();

        /// <inheritdoc cref="Exiled.Events.Handlers.Warhead.OnStopping(StoppingEventArgs)"/>
        public void OnWarheadStopping(StoppingEventArgs ev) => Instance.Config.PlayOnEvent.WarheadCancel.Play();

        /// <inheritdoc cref="Exiled.Events.Handlers.Warhead.OnDetonated"/>
        public void OnWarheadDetonated() => Instance.Config.PlayOnEvent.WarheadDetonated.Play();

        /// <inheritdoc cref="Exiled.Events.Handlers.Map.OnDecontaminating(DecontaminatingEventArgs)"/>
        public void OnDecontaminating(DecontaminatingEventArgs ev) => Instance.Config.PlayOnEvent.DecontaminationStart.Play();

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.OnRoundEnded(RoundEndedEventArgs)"/>
        public void OnRoundEnded(RoundEndedEventArgs ev) => Instance.Config.PlayOnEvent.RoundEnded.Play();

        /// <summary>
        /// Inits the host, to play positional audio.
        /// </summary>
        private void InitHost()
        {
            Server.Host.GameObject.transform.localScale = Vector3.zero;
            Server.Host.IsGodModeEnabled = true;

            NetworkServer.Spawn(Server.Host.GameObject);

            Server.Host.ReferenceHub.characterClassManager.NetworkCurClass = RoleType.ClassD;
            Server.Host.ReferenceHub.characterClassManager.ApplyProperties();

            Server.Host.GameObject.AddComponent<VoiceReceiptTrigger>().RoomName = "SCP";
        }
    }
}
