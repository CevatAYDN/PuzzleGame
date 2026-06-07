using VContainer;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Registers persistence layer services.
    /// Responsibilities: save encryption, storage, game save management.
    /// </summary>
    internal static class PersistenceInstallerModule
    {
        public static void Configure(IContainerBuilder builder)
        {
            // Save system
            builder.Register<ISaveCrypto, SaveCrypto>(Lifetime.Singleton);
            builder.Register<ISaveStorage, SaveStorage>(Lifetime.Singleton);
            builder.Register<ISaveManager, GameSaveManager>(Lifetime.Singleton);
        }
    }
}
