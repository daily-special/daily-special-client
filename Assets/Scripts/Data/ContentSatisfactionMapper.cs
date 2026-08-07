using System;
using System.Collections.Generic;
using System.Linq;
using DailySpecial.Domain;

public static class ContentSatisfactionMapper
{
    public static GuestPersona ToPersona(GuestRecord guest)
    {
        if (guest == null) throw new ArgumentException("손님 콘텐츠가 없다", nameof(guest));

        Dictionary<string, IdealRange> ranges = (guest.ideal_ranges ?? new Dictionary<string, IdealRangeRecord>())
            .ToDictionary(pair => pair.Key, pair => new IdealRange(pair.Value.low, pair.Value.high));
        return new GuestPersona(ranges, guest.dietary);
    }

    public static ServedDish ToServedDish(DishRecord dish, IEnumerable<IngredientRecord> ingredients,
        IDictionary<string, int> parameters)
    {
        if (dish == null) throw new ArgumentException("요리 콘텐츠가 없다", nameof(dish));
        if (ingredients == null) throw new ArgumentException("재료 콘텐츠가 없다", nameof(ingredients));

        Dictionary<string, IngredientRecord> byId = ingredients.ToDictionary(item => item.ingredient_id);
        HashSet<string> conflicts = new();
        foreach (string ingredientId in dish.ingredient_ids ?? new List<string>())
        {
            if (!byId.TryGetValue(ingredientId, out IngredientRecord ingredient))
            {
                throw new InvalidOperationException($"요리 '{dish.dish_id}'의 재료를 찾을 수 없다: {ingredientId}");
            }

            foreach (string conflict in ingredient.dietary_conflicts ?? new List<string>()) conflicts.Add(conflict);
        }

        return new ServedDish(dish.need_tags ?? new List<string>(), dish.base_price, parameters, conflicts);
    }
}
