public static class PbManager
{
    private static readonly float?[] _bests = new float?[3];

    public static float? GetBest(int stageIndex) => _bests[stageIndex];

    // Returns true when the submitted time is a new best (including first completion).
    public static bool Submit(int stageIndex, float time)
    {
        if (!_bests[stageIndex].HasValue || time < _bests[stageIndex].Value)
        {
            _bests[stageIndex] = time;
            return true;
        }
        return false;
    }
}
