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
