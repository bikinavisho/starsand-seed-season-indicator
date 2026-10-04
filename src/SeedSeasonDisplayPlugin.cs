using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using GameTime;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Inventory;
using UnityEngine;
using UnityEngine.UI;
using UI;
using XSandbox.Farm;

namespace StarsandIsland.SeedSeasonDisplay;

[BepInPlugin("com.starsandisland.seedseason-indicator", "Seed Season Display", "1.0.0")]
public sealed class SeedSeasonDisplayPlugin : BasePlugin
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
    private static Sprite _springSprite;
    private static Sprite _summerSprite;
    private static Sprite _fallSprite;
    private static Sprite _winterSprite;

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

        Sprite sprite = GetSeasonSpriteForTemplate(template);
        Image indicator = GetOrCreateIndicator(parent);

        if (sprite == null)
        {
            indicator.sprite = null;
            indicator.gameObject.SetActive(false);
            return;
        }

        indicator.sprite = sprite;
        indicator.gameObject.SetActive(true);
    }

    private static Sprite GetSeasonSpriteForTemplate(ItemTemplate template)
    {
        CropTemplate cropTemplate = ResolveCropTemplate(template);
        if (cropTemplate == null || cropTemplate.SeasonConfigs == null)
        {
            return null;
        }

        ESeason currentSeason = KGameTimeUtil.Now.GetSeason();
        if (HasAllFourSeasons(cropTemplate.SeasonConfigs))
        {
            return null;
        }

        if (cropTemplate.SeasonConfigs.Contains(currentSeason))
        {
            return GetSeasonSprite(currentSeason);
        }

        return null;
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

    private static Sprite GetSeasonSprite(ESeason season)
    {
        switch (season)
        {
            case ESeason.Spring:
                return _springSprite ?? (_springSprite = LoadSeasonSprite("spring.png"));
            case ESeason.Summer:
                return _summerSprite ?? (_summerSprite = LoadSeasonSprite("summer.png"));
            case ESeason.Autumn:
                return _fallSprite ?? (_fallSprite = LoadSeasonSprite("fall.png"));
            case ESeason.Winter:
                return _winterSprite ?? (_winterSprite = LoadSeasonSprite("winter.png"));
            default:
                return null;
        }
    }

    private static Sprite LoadSeasonSprite(string fileName)
    {
        string resourceName = "StarsandIsland.SeedSeasonDisplay.Assets." + fileName;
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("Embedded seasonal icon resource not found: " + resourceName);
            }

            using (MemoryStream buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                byte[] imageData = buffer.ToArray();
                Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                if (!ImageConversion.LoadImage(texture, imageData, true))
                {
                    UnityEngine.Object.Destroy(texture);
                    throw new InvalidOperationException("Unable to decode embedded seasonal icon resource: " + resourceName);
                }

                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 64f, 64f),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }
    }

    private static Image GetOrCreateIndicator(Transform parent)
    {
        Transform existingIndicator = parent.Find(IndicatorName);
        if (existingIndicator != null)
        {
            Image existingImage = existingIndicator.GetComponent<Image>();
            if (existingImage != null)
            {
                return existingImage;
            }

            Component oldTextIndicator = existingIndicator.GetComponent("TextMeshProUGUI");
            if (oldTextIndicator != null)
            {
                UnityEngine.Object.Destroy(oldTextIndicator);
            }

            return existingIndicator.gameObject.AddComponent<Image>();
        }

        GameObject indicatorObject = new GameObject(IndicatorName);
        RectTransform rectTransform = indicatorObject.AddComponent<RectTransform>();
        Image indicator = indicatorObject.AddComponent<Image>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-8f, -8f);
        rectTransform.sizeDelta = new Vector2(28f, 28f);

        indicator.raycastTarget = false;
        indicator.preserveAspect = true;
        indicator.gameObject.SetActive(false);
        return indicator;
    }
}
