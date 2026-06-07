using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeFeatureFlagService : IFeatureFlagService
    {
        public bool GetBool(string key, bool defaultValue) => defaultValue;
        public string GetString(string key, string defaultValue) => defaultValue;
        public int GetInt(string key, int defaultValue) => defaultValue;
    }
}