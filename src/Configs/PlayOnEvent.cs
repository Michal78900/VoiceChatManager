namespace VoiceChatManager.Configs
{
    using System.ComponentModel;
    using Api.Utilities;

    /// <summary>
    /// <see cref="PlayOnEvent"/> related configs.
    /// </summary>
    public sealed class PlayOnEvent
    {
        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the server waits for players:")]
        public AudioFile WaitingForPlayers { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the round has started:")]
        public AudioFile RoundStarted { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the NTF are spawned:")]
        public AudioFile NtfEntrance { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the CI are spawned: (will be heard by ALL players not only ClassD and Chaos!")]
        public AudioFile CiEntrance { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the Warhead is started: (the default Alpha Warhead cassie WILL still play!)")]
        public AudioFile WarheadStart { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the Warhead is cancaled: (the default Alpha Warhead cassie WILL still play!)")]
        public AudioFile WarheadCancel { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the Warhead is detonated:")]
        public AudioFile WarheadDetonated { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the Decontamination proccess has started: (the default decontamination cassie WILL still play!)")]
        public AudioFile DecontaminationStart { get; private set; } = new AudioFile();

        /// <summary>
        /// Gets .
        /// </summary>
        [Description("Called when the round has ended:")]
        public AudioFile RoundEnded { get; private set; } = new AudioFile();
    }
}