using UnityEngine;

public static class Elements
{
    public enum elements {Null, Fire, Water, Earth};

    public static bool WeakTo(elements atkElement, elements defElement)
    {
        // Placeholder weakness logic; you can expand this with actual element interactions, etc.
        if (atkElement == elements.Null || defElement == elements.Null) return false;

        if (atkElement == elements.Fire && defElement == elements.Earth) return true;
        if (atkElement == elements.Water && defElement == elements.Fire) return true;
        if (atkElement == elements.Earth && defElement == elements.Water) return true;

        return false;
    }
}

