/// <summary>
/// Cache de strings para enteros pequeños. Evita el alloc de int.ToString() en
/// los HUD que se refrescan cada frame (reloj, contadores).
/// </summary>
public static class NumberStrings
{
    private const int MaxCached = 256;
    private static readonly string[] cache = BuildCache();

    private static string[] BuildCache()
    {
        string[] values = new string[MaxCached];
        for (int i = 0; i < MaxCached; i++)
        {
            values[i] = i.ToString();
        }
        return values;
    }

    public static string Get(int value)
    {
        if (value < 0) return "0";
        if (value >= MaxCached) return value.ToString();
        return cache[value];
    }
}
