// IGameInput.cs — Core input abstraction interface.
// All input sources (keyboard, webcam pose, easy mode) implement this.
// PlayerController reads ONLY from this interface, never directly from any input source.

namespace EndlessRunner.Input
{
    /// <summary>
    /// Abstraction for game input. Implementations include KeyboardInput,
    /// PoseInput. The PlayerController consumes this
    /// interface without knowing the source.
    /// </summary>
    public interface IGameInput
    {
        /// <summary>
        /// Desired lane: 0 = left, 1 = center, 2 = right.
        /// </summary>
        int DesiredLane { get; }

        /// <summary>
        /// True when a jump has been requested but not yet consumed.
        /// </summary>
        bool JumpRequested { get; }

        /// <summary>
        /// True when a duck/roll has been requested but not yet consumed.
        /// </summary>
        bool DuckRequested { get; }

        /// <summary>
        /// Called by PlayerController after processing the jump.
        /// Resets the jump request flag.
        /// </summary>
        void ConsumeJump();

        /// <summary>
        /// Called by PlayerController after processing the duck.
        /// Resets the duck request flag.
        /// </summary>
        void ConsumeDuck();
    }
}
