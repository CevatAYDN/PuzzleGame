using System;

namespace PuzzleGame.Application.Interfaces
{
    public interface IAgeVerificationService
    {
        bool IsVerified { get; }
        bool IsUnder13 { get; }
        DateTime? BirthDate { get; }

        void Verify(DateTime birthDate);
        void ReVerify(DateTime birthDate);
        void Clear();
    }
}
