using System;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// State machine contract. POCO, Unity bağımlılığı yok, test edilebilir.
    /// Geçiş kuralları CanTransitionTo() ile uygulanabilir.
    /// </summary>
    public interface IGameStateMachine
    {
        GameState Current { get; }
        GameState Previous { get; }

        /// <summary>True eğer mevcut state verilen ile eşitse.</summary>
        bool IsInState(GameState state);

        /// <summary>State değiştiğinde tetiklenir. (previous, current) argümanları.</summary>
        event Action<GameState, GameState> OnStateChanged;

        /// <summary>Geçiş yapar. İzin verilmiyse false döner.</summary>
        bool TransitionTo(GameState next);

        /// <summary>Bir önceki state'e döner (Pause→Playing gibi).</summary>
        bool RevertToPrevious();

        /// <summary>Geçiş kuralı kayıt eder (lifecycle). Null = her zaman izin ver.</summary>
        void RegisterTransitionRule(GameState from, GameState to, Func<bool> guard);

        /// <summary>Geçişe izin verilip verilmediğini kontrol eder (guard kontrolsüz). Default: true.</summary>
        bool CanTransitionTo(GameState next);
    }
}
