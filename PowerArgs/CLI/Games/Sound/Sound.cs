using PowerArgs;
using PowerArgs.Cli;
using System;

namespace PowerArgs.Games
{
    /// <summary>
    /// The main API for playing sound in PowerArgs games. By default it is configured with a No op provider that will not play any sound.
    /// 
    /// If you want sound and are on Windows then you must make your project a ull .NET project that can reference WindowsBase, PresentationCore, and PresentationFramework.
    /// Then you can use the WindowsSoundProvider that implements ISoundProvider by creating an instance of WindowsSoundProvider.SoundProvider and assigning it to Sound.Provider.
    /// 
    /// I have not yet figured out how to make sound work in .NET Core.
    /// 
    /// </summary>
    public static class Sound
    {
        /// <summary>
        /// The current provider, by default a no op that does not play sound
        /// </summary>
        public static ISoundProvider Provider = new NoOpSoundProvider();

        /// <summary>
        /// Plays the sound associated with the given id immediately and once
        /// </summary>
        /// <param name="soundId">a sound id</param>
        public static Promise<Lifetime> Play(string soundId) => Provider.Play(soundId.ToLower());

        /// <summary>
        /// Plays the sound associated with the given id immidiately and in a loop
        /// </summary>
        /// <param name="soundId">a sound id</param>
        /// <returns>a promist to a disposable that can be used to stop the loop</returns>
        public static Promise<IDisposable> Loop(string soundId) => Provider.Loop(soundId.ToLower());
        
        /// <summary>
        /// Disposes the current provider and resets the provider to a no op provider
        /// </summary>
        public static void Dispose()
        {
            Provider.Dispose();
            Provider = new NoOpSoundProvider();
        }
    }

    /// <summary>
    /// The interface for playing sound in PowerArgs games
    /// </summary>
    public interface ISoundProvider : IDisposable
    {
        /// <summary>
        /// Plays the sound associated with the given id immediately and once
        /// </summary>
        /// <param name="soundId">a sound id</param>
        Promise<Lifetime> Play(string soundId);

        /// <summary>
        /// Plays the sound associated with the given id immidiately and in a loop
        /// </summary>
        /// <param name="soundId">a sound id</param>
        /// <returns>a promist to a disposable that can be used to stop the loop</returns>
        Promise<IDisposable> Loop(string soundId);
    }

    /// <summary>
    /// A sound provider that does not play sound
    /// </summary>
    public class NoOpSoundProvider : DummyDisposable, ISoundProvider
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="soundId">unused</param>
        public Promise<Lifetime> Play(string soundId)
        {
            var d = Deferred<Lifetime>.Create();
            var l = new Lifetime();
            l.Dispose();
            d.Resolve(l);
            return d.Promise;
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="soundId">unused</param>
        /// <returns>a promise that resolves immediately to a dummy disposable</returns>
        public Promise<IDisposable> Loop(string soundId)
        {
            var d = Deferred<IDisposable>.Create();
            d.Resolve(new DummyDisposable());
            return d.Promise;
        }
    }
}
