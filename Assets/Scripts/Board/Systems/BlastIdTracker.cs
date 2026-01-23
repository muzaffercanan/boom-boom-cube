public static class BlastIdTracker
{
    private static int _currentBlastId = 0;

    public static int CurrentBlastId => _currentBlastId;

    public static void NextBlast()
    {
        _currentBlastId++;
    }
}
