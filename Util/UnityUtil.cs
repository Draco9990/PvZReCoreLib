using Il2CppInterop.Runtime;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace PvZReCoreLib.Util;

public class UnityUtil
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static void LogDetails(GameObject obj, int indentLevel = 0)
    {
        string indent = new string(' ', indentLevel * 2);
        
        if (obj == null)
        {
            MelonLogger.Msg($"{indent}GameObject is null");
            return;
            
        }

        MelonLogger.Msg($"{indent}--- GameObject: {obj.name} ---");
        MelonLogger.Msg($"{indent}Type: {obj.GetIl2CppType().FullName}");

        Component[] components = obj.GetComponents<Component>();
        MelonLogger.Msg($"{indent}--- Components ({components.Length}) ---");
        foreach (Component comp in components)
        {
            if (comp == null)
            {
                MelonLogger.Msg("Missing Script Component");
                continue;
            }

            MelonLogger.Msg($"{indent}Component: {comp.GetIl2CppType().FullName}");
        }

        // Children
        MelonLogger.Msg($"{indent}--- Children ({obj.transform.childCount}) ---");
        foreach (var o in obj.transform)
        {
            var child = o.TryCast<Transform>();
            if (child is null)
            {
                continue;
            }
            
            MelonLogger.Msg($"{indent}Child: {child.name}");
            LogDetails(child.gameObject, indentLevel + 1);
        }
    }
    
    public static List<GameObject> FindDeepChildrenByName(Transform parent, string name)
    {
        var results = new List<GameObject>();
        FindChildrenRecursive(parent, name, results);
        return results;
    }

    private static void FindChildrenRecursive(Transform parent, string name, List<GameObject> results)
    {
        foreach (var o in parent)
        {
            var child = o.TryCast<Transform>();
            if (child is null)
            {
                continue;
            }

            if (child.name == name)
            {
                results.Add(child.gameObject);
            }
            FindChildrenRecursive(child, name, results);
        }
    }
    
    public static void InjectScrollbar(Transform parent, Transform gridChild, 
        float heightMult = 1f, float widthAdd = 0f)
    {
        if(parent.FindChild("CoreLib_GridScrollView") != null)
        {
            // Already injected
            return;
        }
        
        RectTransform grid = gridChild.Cast<RectTransform>();
        
        // Scroll view root
        GameObject injectedRoot;
        RectTransform injectedRootRectT;
        ScrollRect injectedRootScrollRect;
        {
            // Store original grid size before modifications
            Vector2 originalOffsetMin = grid.offsetMin;
            Vector2 originalOffsetMax = grid.offsetMax;
            var originalPivot = grid.pivot;
            var originalAnchorsMin = grid.anchorMin;
            var originalAnchorsMax = grid.anchorMax;
            var originalLocalPosition = grid.localPosition;
            var originalAnchoredPosition = grid.anchoredPosition;
            var originalSizeDelta = grid.sizeDelta;
        
            float height = grid.offsetMax.y - grid.offsetMin.y;
        
            // Create Scroll View root
            injectedRoot = new GameObject("CoreLib_GridScrollView", Il2CppType.Of<RectTransform>(), Il2CppType.Of<ScrollRect>());
            injectedRoot.transform.SetParent(parent, false);
            injectedRoot.transform.SetSiblingIndex(grid.GetSiblingIndex());

            injectedRootRectT = injectedRoot.GetComponent<RectTransform>().Cast<RectTransform>();
            injectedRootRectT.anchorMin = originalAnchorsMin;
            injectedRootRectT.anchorMax = originalAnchorsMax;
            injectedRootRectT.pivot = originalPivot;
            injectedRootRectT.offsetMin = originalOffsetMin;
            injectedRootRectT.offsetMax = originalOffsetMax;
            injectedRootRectT.localPosition = originalLocalPosition;
            injectedRootRectT.anchoredPosition = originalAnchoredPosition;
            injectedRootRectT.sizeDelta = originalSizeDelta;
            injectedRootRectT.sizeDelta += new Vector2(widthAdd, height * (heightMult - 1)); // Make room for scrollbar
            
            injectedRootScrollRect = injectedRoot.GetComponent<ScrollRect>();
        }
        
        // Viewport
        GameObject injectedViewport;
        RectTransform injectedViewportRectT;
        Mask injectedViewportMask;
        {
            injectedViewport = new GameObject("Viewport", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Mask>(), Il2CppType.Of<Image>());
            injectedViewport.transform.SetParent(injectedRoot.transform, false);

            injectedViewportRectT = injectedViewport.GetComponent<RectTransform>();
            injectedViewportRectT.anchorMin = new Vector2(0, 0);
            injectedViewportRectT.anchorMax = new Vector2(1, 1);
            injectedViewportRectT.offsetMin = Vector2.zero;
            injectedViewportRectT.offsetMax = Vector2.zero;

            injectedViewportMask = injectedViewport.GetComponent<Mask>();
            injectedViewportMask.showMaskGraphic = false;
        }
        
        // Configure ScrollRect
        {
            injectedRootScrollRect.viewport = injectedViewportRectT;
            injectedRootScrollRect.content = grid;
            injectedRootScrollRect.horizontal = false;
            injectedRootScrollRect.vertical = true;    
        }

        // Reset grid anchors/offsets and move under viewport
        {
            grid.SetParent(injectedViewport.transform, false);
            grid.anchorMin = new Vector2(0, 1); // Anchor to top-left
            grid.anchorMax = new Vector2(1, 1);
            grid.pivot = new Vector2(0.5f, 1);
            grid.anchoredPosition = Vector2.zero;
            grid.localPosition = Vector3.zero;
            grid.offsetMin = new Vector2(0, 0);
            grid.offsetMax = new Vector2(0, 0);
            grid.sizeDelta = new Vector2(0, 0);
        }

        // Scrollbar
        GameObject injectedScrollbar;
        RectTransform injectedScrollbarRectT;
        Image injectedScrollbarImage;
        Scrollbar injectedScrollbarComp;
        {
            injectedScrollbar = new GameObject("Scrollbar", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Image>(), Il2CppType.Of<Scrollbar>());
            injectedScrollbar.transform.SetParent(injectedRoot.transform, false);
            
            injectedScrollbarRectT = injectedScrollbar.GetComponent<RectTransform>();
            injectedScrollbarRectT.anchorMin = new Vector2(1, 0);
            injectedScrollbarRectT.anchorMax = new Vector2(1, 1);
            injectedScrollbarRectT.pivot = new Vector2(1, 0.5f);
            injectedScrollbarRectT.sizeDelta = new Vector2(20, 0);
            injectedScrollbarRectT.anchoredPosition = new Vector2(0, 0); // Position at right edge
            
            injectedScrollbarImage = injectedScrollbar.GetComponent<Image>();
            injectedScrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Dark semi-transparent background
            
            injectedScrollbarComp = injectedScrollbar.GetComponent<Scrollbar>();
        }

        // Scrollbar Handle
        GameObject injectedScrollbarHandle;
        RectTransform injectedScrollbarHandleRectT;
        Image injectedScrollbarHandleImage;
        {
            injectedScrollbarHandle = new GameObject("Handle", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Image>());
            injectedScrollbarHandle.transform.SetParent(injectedScrollbar.transform, false);
            
            injectedScrollbarHandleRectT = injectedScrollbarHandle.GetComponent<RectTransform>();
            injectedScrollbarHandleRectT.anchorMin = Vector2.zero;
            injectedScrollbarHandleRectT.anchorMax = Vector2.one;
            injectedScrollbarHandleRectT.offsetMin = Vector2.zero;
            injectedScrollbarHandleRectT.offsetMax = Vector2.zero;
            
            injectedScrollbarHandleImage = injectedScrollbarHandle.GetComponent<Image>();
            injectedScrollbarHandleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Lighter handle
            
            injectedScrollbarComp = injectedScrollbar.GetComponent<Scrollbar>();
            injectedScrollbarComp.handleRect = injectedScrollbarHandleRectT;
            injectedScrollbarComp.direction = Scrollbar.Direction.BottomToTop;
            
            injectedRootScrollRect.verticalScrollbar = injectedScrollbarComp;
            injectedRootScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }
        
        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
    }

    #endregion
}