using System;
using System.Collections.Generic;

[Serializable]
public sealed class ContentPackage<T>
{
    public string schema_version;
    public string bible_version;
    public string kind;
    public string generated_at;
    public string run_id;
    public List<T> items;
}

[Serializable]
public sealed class GuestRecord
{
    public string guest_id;
    public string name;
    public string title;
    public string bio;
    public string voice;
    public string personality;
    public List<string> preferred_needs;
    public Dictionary<string, IdealRangeRecord> ideal_ranges;
    public List<string> dietary;
}

[Serializable]
public sealed class IdealRangeRecord
{
    public int low;
    public int high;
}

[Serializable]
public sealed class LineRecord
{
    public string line_id;
    public string situation;
    public string subject;
    public string voice;
    public string text;
}

[Serializable]
public sealed class IngredientRecord
{
    public string ingredient_id;
    public string name;
    public string kind;
    public string description;
    public int base_price;
    public List<string> dietary_conflicts;
}

[Serializable]
public sealed class DishRecord
{
    public string dish_id;
    public string name;
    public string description;
    public List<string> need_tags;
    public List<string> ingredient_ids;
    public int base_price;
}
