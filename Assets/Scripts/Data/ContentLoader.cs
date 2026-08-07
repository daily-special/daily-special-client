using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public static class ContentLoader
{
    private const int SupportedSchemaMajor = 1;

    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public static ContentPackage<GuestRecord> LoadGuests()
    {
        TextAsset source = Resources.Load<TextAsset>("guests");
        if (source == null)
        {
            throw new InvalidOperationException("Resources에 guests.json이 없습니다.");
        }

        ContentPackage<GuestRecord> package = JsonConvert.DeserializeObject<ContentPackage<GuestRecord>>(
            source.text,
            Settings);

        if (package == null)
        {
            throw new InvalidOperationException("guests.json을 읽지 못했습니다.");
        }

        ValidatePackage(package, "guests");
        return package;
    }

    public static ContentPackage<LineRecord> LoadLines()
    {
        TextAsset source = Resources.Load<TextAsset>("lines");
        if (source == null)
        {
            throw new InvalidOperationException("Resources에 lines.json이 없습니다.");
        }

        ContentPackage<LineRecord> package = JsonConvert.DeserializeObject<ContentPackage<LineRecord>>(
            source.text,
            Settings);

        if (package == null)
        {
            throw new InvalidOperationException("lines.json을 읽지 못했습니다.");
        }

        ValidatePackage(package, "lines");
        return package;
    }

    public static ContentPackage<IngredientRecord> LoadIngredients()
    {
        return LoadPackage<IngredientRecord>("ingredients", "ingredients");
    }

    public static ContentPackage<DishRecord> LoadDishes()
    {
        return LoadPackage<DishRecord>("dishes", "dishes");
    }

    private static ContentPackage<T> LoadPackage<T>(string resourceName, string expectedKind)
    {
        TextAsset source = Resources.Load<TextAsset>(resourceName);
        if (source == null)
        {
            throw new InvalidOperationException($"Resources에 {resourceName}.json이 없습니다.");
        }

        ContentPackage<T> package = JsonConvert.DeserializeObject<ContentPackage<T>>(source.text, Settings);
        if (package == null)
        {
            throw new InvalidOperationException($"{resourceName}.json을 읽지 못했습니다.");
        }

        ValidatePackage(package, expectedKind);
        return package;
    }

    private static void ValidatePackage<T>(ContentPackage<T> package, string expectedKind)
    {
        if (package.kind != expectedKind)
        {
            throw new InvalidOperationException($"콘텐츠 종류가 {expectedKind}이 아닙니다: {package.kind}");
        }

        if (string.IsNullOrWhiteSpace(package.schema_version))
        {
            throw new InvalidOperationException("schema_version이 비어 있습니다.");
        }

        string[] version = package.schema_version.Split('.');
        if (!int.TryParse(version[0], out int major) || major != SupportedSchemaMajor)
        {
            throw new InvalidOperationException(
                $"지원하지 않는 schema_version입니다: {package.schema_version}");
        }

        if (package.items == null || package.items.Count == 0)
        {
            throw new InvalidOperationException("손님 콘텐츠가 비어 있습니다.");
        }
    }
}
