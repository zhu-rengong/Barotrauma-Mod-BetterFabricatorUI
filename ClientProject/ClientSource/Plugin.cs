using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using static HarmonyLib.Code;

namespace BetterFabricatorUI
{
    public partial class Plugin : IAssemblyPlugin
    {
        private static Dictionary<Fabricator, GUIDropDown> cachedFabricatorContentPackageFilter = new();

        [HarmonyPatch(declaringType: typeof(Fabricator))]
        [HarmonyPatch(methodName: nameof(Fabricator.CreateGUI))]
        class Patch_Fabricator_CreateGUI
        {
            [HarmonyPostfix]
            static void MakeFabricatorBetterAgain(Fabricator __instance)
            {
                var fabricator = __instance;

                // Finding anonymous gui components
                var paddedItemFrame = fabricator.itemList.Parent as GUILayoutGroup;
                var itemListFrame = paddedItemFrame.Parent as GUILayoutGroup;
                var topFrame = itemListFrame.Parent as GUIFrame;
                var mainFrame = topFrame.Parent as GUILayoutGroup;
                var innerArea = mainFrame.Parent as GUILayoutGroup;
                var paddedFrame = innerArea.Parent as GUILayoutGroup;

                // Expand the height of the gui for digging a modding area
                float expandedSizeForModdingFrame = 0.05f;
                Vector2 originalGuiFrameSize = fabricator.GuiFrame.RectTransform.RelativeSize;
                fabricator.GuiFrame.RectTransform.RelativeSize += new Vector2(0.0f, expandedSizeForModdingFrame);
                if (fabricator.AlternativeLayout is not null) { fabricator.AlternativeLayout.RelativeSize += new Vector2(0.0f, expandedSizeForModdingFrame); }
                if (fabricator.DefaultLayout is not null) { fabricator.DefaultLayout.RelativeSize += new Vector2(0.0f, expandedSizeForModdingFrame); }

                // Keep the original gui component size unchanged
                mainFrame.Children.ForEach(child => child.RectTransform.RelativeSize *= originalGuiFrameSize / fabricator.GuiFrame.RectTransform.RelativeSize);

                var moddingFrame = new GUIFrame(
                    new RectTransform(
                        new Vector2(1.0f, 1.0f - (originalGuiFrameSize.Y / fabricator.GuiFrame.RectTransform.RelativeSize.Y)),
                        mainFrame.RectTransform
                    )
                )
                { AutoDraw = false };

                HashSet<ContentPackage> contentPackages = new();
                fabricator.fabricationRecipes.Values.ForEach(recipe =>
                {
                    if (recipe.TargetItem.ContentPackage is ContentPackage contentPackage)
                    {
                        contentPackages.Add(contentPackage);
                    }
                });

                // Used to filter recipes based on content packages
                var contentPackageFilter = new GUIDropDown(
                    new RectTransform(
                        Vector2.One,
                        moddingFrame.RectTransform,
                        anchor: Anchor.CenterRight
                    ),
                    elementCount: Math.Clamp(contentPackages.Count, 4, 10),
                    selectMultiple: true,
                    dropAbove: true,
                    textAlignment: Alignment.CenterLeft
                );
                contentPackageFilter.ListBox.Padding = new Vector4(10.0f, 15.0f, 10.0f, 15.0f);

                contentPackages.ForEach(contentPackage => contentPackageFilter.AddItem(RichString.Rich(contentPackage.Name).SanitizedValue, contentPackage));

                contentPackageFilter.ListBox.Content.Children.ForEach(child =>
                {
                    var tickBox = child.GetChild<GUITickBox>();
                    var contentPackage = tickBox.UserData as ContentPackage;
                    if (ContentPackageManager.LocalPackages.Contains(contentPackage))
                    {
                        tickBox.text.OverrideTextColor(GUIStyle.TextColorBright);
                    }
                    else if (ContentPackageManager.WorkshopPackages.Contains(contentPackage))
                    {
                        tickBox.text.OverrideTextColor(Color.MediumPurple);
                    }
                    else
                    {
                        tickBox.text.OverrideTextColor(GUIStyle.Green);
                    }

                    // Collapse content package selector when clicking RMB on any option
                    tickBox.OnSecondaryClicked = (GUIComponent component, object userData) =>
                    {
                        contentPackageFilter.Dropped = false;
                        return true;
                    };

                    // Modify layout settings to prevent gui from being reset to origin
                    tickBox.ContentWidth = tickBox.Rect.Width;
                    tickBox.RectTransform.ScaleChanged += () => { tickBox.ContentWidth = tickBox.Rect.Width; };
                    tickBox.RectTransform.SizeChanged += () => { tickBox.ContentWidth = tickBox.Rect.Width; };

                    tickBox.HoverColor = new Color(50, 50, 50, 100);
                });

                SetSelectorText();
                contentPackageFilter.AfterSelected += (_, _) => SetSelectorText();

                bool SetSelectorText()
                {
                    if (contentPackageFilter.SelectedIndexMultiple.Count() == 0)
                    {
                        contentPackageFilter.Text = TextManager.Get("workshopmenutab.installedmods");
                        contentPackageFilter.ButtonTextColor = GUIStyle.TextColorDim;
                    }
                    else
                    {
                        List<LocalizedString> texts = new List<LocalizedString>();
                        foreach (GUIComponent child in contentPackageFilter.ListBox.Content.Children)
                        {
                            var tickBox = child.GetChild<GUITickBox>();
                            if (tickBox is { Selected: true })
                            {
                                var contentPackage = tickBox.UserData as ContentPackage;
                                if (ContentPackageManager.LocalPackages.Contains(contentPackage))
                                {
                                    texts.Add($"‖color:gui.textcolorbright‖{tickBox.Text}‖color:end‖");
                                }
                                else if (ContentPackageManager.WorkshopPackages.Contains(contentPackage))
                                {
                                    texts.Add($"‖color:147,112,219,255‖{tickBox.Text}‖color:end‖");
                                }
                                else
                                {
                                    texts.Add($"‖color:gui.green‖{tickBox.Text}‖color:end‖");
                                }
                            }
                        }
                        contentPackageFilter.button.GetChild<GUITextBlock>().Text = RichString.Rich(
                            "‖color:gui.yellow‖[‖color:end‖" + LocalizedString.Join(" ‖color:gui.yellow‖][‖color:end‖ ", texts) + "‖color:gui.yellow‖]‖color:end‖");
                        contentPackageFilter.ButtonTextColor = GUIStyle.TextColorNormal;
                    }

                    return true;
                }

                fabricator.itemCategoryButtons.ForEach(button =>
                {
                    button.SelectedColor = Color.Yellow;
                    button.HoverColor = Color.LightYellow;
                });

                var categoryButtonAll = fabricator.itemCategoryButtons.FirstOrDefault(button => button.UserData is null);
                contentPackageFilter.AfterSelected += (_, _) => FilterByContentPacakge();

                bool FilterByContentPacakge()
                {
                    var selectedContentPackages = contentPackageFilter.SelectedDataMultiple.Cast<ContentPackage>().ToHashSet();
                    fabricator.itemCategoryButtons.ForEach(button =>
                    {
                        if (selectedContentPackages.Count == 0)
                        {
                            fabricator.itemCategoryButtons.ForEach(button => button.Enabled = true);
                        }
                        else
                        {
                            MapEntityCategory filteredCategories = new();
                            fabricator.fabricationRecipes.Values.ForEach(recipe =>
                            {
                                if (recipe?.TargetItem is ItemPrefab ti
                                    && ti.ContentPackage is ContentPackage contentPackage
                                    && selectedContentPackages.Contains(contentPackage))
                                {
                                    filteredCategories |= ti.Category;
                                }
                            });

                            fabricator.itemCategoryButtons.ForEach(button =>
                            {
                                var category = (MapEntityCategory?)button.UserData;
                                if (category.HasValue)
                                {
                                    button.Enabled = filteredCategories.HasFlag(category);
                                    if (!button.Enabled && button.Selected)
                                    {
                                        button.Selected = false;
                                        if (categoryButtonAll is not null)
                                        {
                                            categoryButtonAll.OnClicked(categoryButtonAll, categoryButtonAll.UserData);
                                        }
                                    }
                                }
                            });
                        }
                    });

                    fabricator.FilterEntities(fabricator.selectedItemCategory, fabricator.itemFilterBox?.Text ?? string.Empty);

                    return true;
                }

                cachedFabricatorContentPackageFilter[fabricator] = contentPackageFilter;
            }
        }

        [HarmonyPatch(declaringType: typeof(Fabricator))]
        [HarmonyPatch(methodName: nameof(Fabricator.FilterEntities))]
        class Patch_Fabricator_FilterEntities
        {
            [HarmonyPostfix]
            static void FurtherFilterEntities(Fabricator __instance, MapEntityCategory? category, string filter)
            {
                var fabricator = __instance;

                if (!cachedFabricatorContentPackageFilter.TryGetValue(fabricator, out GUIDropDown contentPackageSelector)) { return; }
                var selectedContentPackages = contentPackageSelector.SelectedDataMultiple.Cast<ContentPackage>().ToHashSet();
                if (selectedContentPackages.Count == 0) { return; }

                foreach (GUIComponent child in fabricator.itemList.Content.Children)
                {
                    if (!child.Visible) { continue; }
                    if (child.UserData is FabricationRecipe recipe)
                    {
                        if (recipe?.TargetItem is ItemPrefab ti
                            && ti.ContentPackage is ContentPackage contentPackage
                            && !selectedContentPackages.Contains(contentPackage))
                        {
                            child.Visible = false;
                        }
                    }
                }

                fabricator.HideEmptyItemListCategories();
            }
        }

        [HarmonyPatch(declaringType: typeof(Entity))]
        [HarmonyPatch(methodName: nameof(Entity.RemoveAll))]
        class Patch_Entity_RemoveAll
        {
            [HarmonyPostfix]
            static void Uncache()
            {
                LuaCsLogger.LogMessage($"[{nameof(BetterFabricatorUI)}] Uncache {cachedFabricatorContentPackageFilter.Count} fabricator(s)");
                cachedFabricatorContentPackageFilter.Clear();
            }
        }
    }
}
