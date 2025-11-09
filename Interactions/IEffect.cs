namespace Ostranauts.Bit.Interactions
{
    /// <summary>
    /// Interface for custom interaction effects that can be registered with LaunchControl
    /// </summary>
    public interface IEffect
    {
        /// <summary>
        /// Determines whether this effect needs to prepare/modify the interaction before ApplyEffects runs
        /// </summary>
        /// <param name="interaction">The interaction being processed</param>
        /// <returns>True if Prepare should be called, false otherwise</returns>
        bool ShouldPrepare(Interaction interaction);

        /// <summary>
        /// Prepares/modifies the interaction before ApplyEffects runs (e.g., clear objThem to prevent default behavior)
        /// </summary>
        /// <param name="interaction">The interaction to prepare</param>
        void Prepare(Interaction interaction);

        /// <summary>
        /// Determines whether this effect should execute for the given interaction (called after ApplyEffects)
        /// </summary>
        /// <param name="interaction">The interaction being processed</param>
        /// <returns>True if this effect should execute, false otherwise</returns>
        bool ShouldExecute(Interaction interaction);

        /// <summary>
        /// Executes the custom effect logic for the interaction (called after ApplyEffects)
        /// </summary>
        /// <param name="interaction">The interaction being processed</param>
        /// <returns>EffectResult indicating whether to continue processing other effects</returns>
        EffectResult Execute(Interaction interaction);
    }

    /// <summary>
    /// Result of an effect execution
    /// </summary>
    public class EffectResult
    {
        /// <summary>
        /// Whether processing should continue to the next effect
        /// </summary>
        public bool bShouldContinue { get; set; }

        /// <summary>
        /// Creates a new effect result
        /// </summary>
        /// <param name="shouldContinue">Whether processing should continue</param>
        public EffectResult(bool shouldContinue)
        {
            bShouldContinue = shouldContinue;
        }
    }
}

