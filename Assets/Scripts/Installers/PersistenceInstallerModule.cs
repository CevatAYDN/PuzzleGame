using Nexus.Core;
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
        public static void Configure(IContextBuilder builder, NexusDI container)
        {
            // Save system
            builder.Bind<ISaveCrypto, SaveCrypto>();
            builder.Bind<ISaveStorage, SaveStorage>();
            builder.Bind<ISaveManager, GameSaveManager>();

            // Cloud save (wraps leaderboard + progress + shop + crypto + storage)
            builder.Bind<ICloudSaveService, CloudSaveService>();
        }
    }
}
