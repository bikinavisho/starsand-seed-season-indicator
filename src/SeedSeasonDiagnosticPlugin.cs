using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using GameTime;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UI;
using XSandbox.Farm;

namespace StarsandIsland.SeedSeasonDiagnostic;

[BepInPlugin("com.starsandisland.seedseason-indicator", "Seasonal Seed Indicator", "0.1.0")]
public sealed class SeedSeasonDiagnosticPlugin : BasePlugin
{
    public override void Load()
    {
        AddComponent<SeasonalSeedIndicatorManager>();
        Log.LogInfo("Seasonal seed indicator loaded.");
    }
}

public sealed class SeasonalSeedIndicatorManager : MonoBehaviour
{
    private const string IndicatorName = "SeasonalSeedIndicator";
    private const float RefreshIntervalSeconds = 0.75f;

    private ESeason _lastObservedSeason = ESeason.Total;
    private float _nextRefreshSeconds;

    private void Update()
    {
        ESeason currentSeason;
        try
        {
            currentSeason = KGameTimeUtil.Now.GetSeason();
        }
        catch
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        if (currentSeason != _lastObservedSeason || currentTime >= _nextRefreshSeconds)
        {
            _lastObservedSeason = currentSeason;
            _nextRefreshSeconds = currentTime + RefreshIntervalSeconds;
            RefreshVisibleIndicators();
        }
    }

    private static void RefreshVisibleIndicators()
    {
        foreach (KUICell_Item cell in UnityEngine.Object.FindObjectsOfType<KUICell_Item>())
        {
            if (cell == null || !cell.gameObject.activeInHierarchy)
            {
                continue;
            }

            ApplyIndicator(cell);
        }

        foreach (KUIShopItemCard card in UnityEngine.Object.FindObjectsOfType<KUIShopItemCard>())
        {
            if (card == null || !card.gameObject.activeInHierarchy)
            {
                continue;
            }

            ApplyIndicator(card);
        }
    }

    private static void ApplyIndicator(KUICell_Item cell)
    {
        if (cell == null)
        {
            return;
        }

        ItemTemplate template = cell.Template;
        if (template == null && cell.Item != null)
        {
            template = cell.Item.Template;
        }

        ApplyIndicator(cell, template, cell.Preview != null ? cell.Preview.transform : cell.transform);
    }

    private static void ApplyIndicator(KUIShopItemCard card)
    {
        if (card == null)
        {
            return;
        }

        ItemTemplate template = GetShopCardTemplate(card);
        ApplyIndicator(card, template, card.Preview != null ? card.Preview.transform : card.transform);
    }

    private static void ApplyIndicator(Component owner, ItemTemplate template, Transform parent)
    {
        if (owner == null || parent == null)
        {
            return;
        }

        string symbol = GetSeasonSymbolForTemplate(template);
        TextMeshProUGUI indicator = GetOrCreateIndicator(parent);

        if (string.IsNullOrEmpty(symbol))
        {
            indicator.text = string.Empty;
            indicator.gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);
        indicator.text = symbol;
    }

    private static string GetSeasonSymbolForTemplate(ItemTemplate template)
    {
        CropTemplate cropTemplate = ResolveCropTemplate(template);
        if (cropTemplate == null || cropTemplate.SeasonConfigs == null)
        {
            return string.Empty;
        }

        ESeason currentSeason = KGameTimeUtil.Now.GetSeason();
        if (HasAllFourSeasons(cropTemplate.SeasonConfigs))
        {
            return string.Empty;
        }

        if (cropTemplate.SeasonConfigs.Contains(currentSeason))
        {
            return GetSeasonSymbol(currentSeason);
        }

        return string.Empty;
    }

    private static CropTemplate ResolveCropTemplate(ItemTemplate template)
    {
        if (template == null || template.Extensions == null)
        {
            return null;
        }

        foreach (Il2CppSystem.Type extensionType in template.Extensions.Keys)
        {
            if (extensionType == null
                || !string.Equals(extensionType.FullName, "XSandbox.Farm.FarmSeedItemExt", StringComparison.Ordinal))
            {
                continue;
            }

            ItemExtension extension = template.Extensions[extensionType];
            if (extension == null)
            {
                continue;
            }

            FarmSeedItemExt farmSeedExtension = ((Il2CppObjectBase)extension).TryCast<FarmSeedItemExt>();
            if (farmSeedExtension == null || farmSeedExtension.Template == null)
            {
                continue;
            }

            CropTemplate cropTemplate = farmSeedExtension.Template.Get();
            return cropTemplate;
        }

        return null;
    }

    private static ItemTemplate GetShopCardTemplate(KUIShopItemCard card)
    {
        if (card == null || card.ShopItem == null || card.ShopItem.Template == null)
        {
            return null;
        }

        if (card.ShopItem.Template.ItemTemplate == null)
        {
            return null;
        }

        return card.ShopItem.Template.ItemTemplate.Get();
    }

    private static bool HasAllFourSeasons(Il2CppSystem.Collections.Generic.List<ESeason> seasonConfigs)
    {
        if (seasonConfigs == null)
        {
            return false;
        }

        return seasonConfigs.Contains(ESeason.Spring)
            && seasonConfigs.Contains(ESeason.Summer)
            && seasonConfigs.Contains(ESeason.Autumn)
            && seasonConfigs.Contains(ESeason.Winter);
    }

    private static string GetSeasonSymbol(ESeason season)
    {
        switch (season)
        {
            case ESeason.Spring:
                return "🌸";
            case ESeason.Summer:
                return "☀️";
            case ESeason.Autumn:
                return "🍂";
            case ESeason.Winter:
                return "⛄";
            default:
                return string.Empty;
        }
    }

    private static TextMeshProUGUI GetOrCreateIndicator(Transform parent)
    {
        Transform existingIndicator = parent.Find(IndicatorName);
        if (existingIndicator != null)
        {
            return existingIndicator.GetComponent<TextMeshProUGUI>();
        }

        GameObject indicatorObject = new GameObject(IndicatorName);
        RectTransform rectTransform = indicatorObject.AddComponent<RectTransform>();
        indicatorObject.AddComponent<TextMeshProUGUI>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-8f, -8f);
        rectTransform.sizeDelta = new Vector2(28f, 28f);

        TextMeshProUGUI indicator = indicatorObject.GetComponent<TextMeshProUGUI>();
        indicator.raycastTarget = false;
        indicator.alignment = TextAlignmentOptions.Center;
        indicator.fontSize = 20f;
        indicator.text = string.Empty;
        indicator.enableWordWrapping = false;
        indicator.gameObject.SetActive(false);
        return indicator;
    }
}
