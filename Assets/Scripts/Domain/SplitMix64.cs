namespace DailySpecial.Domain
{

public sealed class SplitMix64
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
    private const ulong Mix1 = 0xBF58476D1CE4E5B9UL;
    private const ulong Mix2 = 0x94D049BB133111EBUL;
    private ulong state;

    public SplitMix64(long seed)
    {
        state = unchecked((ulong)seed);
    }

    public long NextLong()
    {
        ulong z = unchecked(state += GoldenGamma);
        z = unchecked((z ^ (z >> 30)) * Mix1);
        z = unchecked((z ^ (z >> 27)) * Mix2);
        return unchecked((long)(z ^ (z >> 31)));
    }

    public int NextInt(int boundExclusive)
    {
        if (boundExclusive <= 0)
        {
            throw new System.ArgumentException($"상한은 양수여야 한다: {boundExclusive}", nameof(boundExclusive));
        }

        // Java Math.floorMod(long, long)과 동일하다. ulong 나머지를 쓰면 고정 벡터가 갈라진다.
        return FloorMod(NextLong(), boundExclusive);
    }

    public static int FloorMod(long value, int boundExclusive)
    {
        if (boundExclusive <= 0)
        {
            throw new System.ArgumentException($"상한은 양수여야 한다: {boundExclusive}", nameof(boundExclusive));
        }

        return (int)(((value % boundExclusive) + boundExclusive) % boundExclusive);
    }
}
}
