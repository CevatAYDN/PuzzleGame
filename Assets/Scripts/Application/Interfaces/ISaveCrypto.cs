namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Pure C# contract for save-payload signing. Implementation lives in
    /// Infrastructure (HMAC-SHA256 + device-derived secret key).
    /// Split out of GameSaveManager (Sprint #18) so the orchestrator stays
    /// decoupled from any specific crypto algorithm.
    /// </summary>
    public interface ISaveCrypto
    {
        /// <summary>Secret key the implementation derives internally.</summary>
        string SecretKey { get; }

        /// <summary>Generate a fresh per-write salt (base64, 16 random bytes).</summary>
        string GenerateSalt();

        /// <summary>Compute hex HMAC-SHA256(salt + payload) using SecretKey.</summary>
        string Sign(string salt, string payload);

        /// <summary>Constant-time verify of an expected hex signature.</summary>
        bool Verify(string salt, string payload, string expectedHex);
    }
}
