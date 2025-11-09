using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Ostranauts.Bit.Commands
{
    /// <summary>
    /// Implements the dumpui command which waits for a mouse click then dumps UI hierarchy
    /// </summary>
    public class DumpUICommand : MonoBehaviour
    {
        private bool _waitingForClick = false;
        private EventSystem _eventSystem;

        private void Start()
        {
            // Register the dumpui command
            LaunchControl.Instance.Commands.RegisterCommand("dumpui", OnDumpUICommand);
            LaunchControlPlugin.Logger.LogInfo("DumpUI command registered");
        }

        private void OnDumpUICommand(string input)
        {
            LaunchControlPlugin.Logger.LogInfo("DumpUI: Click on any UI element to dump its hierarchy...");
            _waitingForClick = true;
        }

        private void Update()
        {
            if (_waitingForClick && Input.GetMouseButtonDown(0))
            {
                _waitingForClick = false;
                ProcessClick();
            }
        }

        private void ProcessClick()
        {
            try
            {
                LaunchControlPlugin.Logger.LogInfo("DumpUI: Processing click...");

                // Get the EventSystem
                if (_eventSystem == null)
                {
                    _eventSystem = EventSystem.current;
                }

                if (_eventSystem == null)
                {
                    LaunchControlPlugin.Logger.LogError("DumpUI: EventSystem not found!");
                    return;
                }

                // Create pointer event data
                PointerEventData pointerData = new PointerEventData(_eventSystem);
                pointerData.position = Input.mousePosition;

                // Raycast to find UI elements
                List<RaycastResult> results = new List<RaycastResult>();
                _eventSystem.RaycastAll(pointerData, results);

                if (results.Count == 0)
                {
                    LaunchControlPlugin.Logger.LogWarning("DumpUI: No UI elements found at click position");
                    return;
                }

                // Get the first (top-most) UI element
                GameObject clickedObject = results[0].gameObject;
                LaunchControlPlugin.Logger.LogInfo($"DumpUI: Found UI element: {clickedObject.name}");

                // Find the root canvas
                Canvas rootCanvas = FindRootCanvas(clickedObject);
                if (rootCanvas == null)
                {
                    LaunchControlPlugin.Logger.LogWarning("DumpUI: Could not find root Canvas, dumping from clicked object");
                    DumpHierarchy(clickedObject, 0);
                }
                else
                {
                    LaunchControlPlugin.Logger.LogInfo($"DumpUI: Root Canvas: {rootCanvas.name}");
                    LaunchControlPlugin.Logger.LogInfo("==================== UI Hierarchy Dump ====================");
                    DumpHierarchy(rootCanvas.gameObject, 0);
                    LaunchControlPlugin.Logger.LogInfo("==================== End of Dump ====================");
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"DumpUI Error: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }

        private Canvas FindRootCanvas(GameObject obj)
        {
            Transform current = obj.transform;
            Canvas lastCanvas = null;

            while (current != null)
            {
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null)
                {
                    lastCanvas = canvas;
                }
                current = current.parent;
            }

            return lastCanvas;
        }

        public static void DumpHierarchy(GameObject obj, int depth)
        {
            if (obj == null)
            {
                return;
            }

            // Create indentation
            StringBuilder indent = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                indent.Append("  ");
            }

            string indentStr = indent.ToString();

            // Basic object info
            string activeStr = obj.activeSelf ? "active" : "inactive";
            string layerStr = LayerMask.LayerToName(obj.layer);
            
            // Check for prefab information
            string prefabInfo = GetPrefabInfo(obj);
            
            string objectInfo = $"{indentStr}[{obj.name}] (Layer: {layerStr}, {activeStr}{prefabInfo})";
            
            LaunchControlPlugin.Logger.LogInfo(objectInfo);

            // Get all components
            Component[] components = obj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    continue;
                }

                string componentInfo = DumpComponent(components[i], indentStr);
                if (!string.IsNullOrEmpty(componentInfo))
                {
                    LaunchControlPlugin.Logger.LogInfo(componentInfo);
                }
            }

            // Recursively dump children
            Transform transform = obj.transform;
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    DumpHierarchy(child.gameObject, depth + 1);
                }
            }
        }

        private static string DumpComponent(Component component, string indent)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(indent);
            sb.Append("  - ");
            sb.Append(component.GetType().Name);

            // Add specific component details
            if (component is RectTransform)
            {
                RectTransform rt = component as RectTransform;
                sb.Append($" [AnchoredPos: {rt.anchoredPosition}, Size: {rt.sizeDelta}, Pivot: {rt.pivot}, Anchors: Min{rt.anchorMin}/Max{rt.anchorMax}]");
            }
            else if (component is Text)
            {
                Text text = component as Text;
                string textContent = text.text;
                if (textContent.Length > 50)
                {
                    textContent = textContent.Substring(0, 50) + "...";
                }
                sb.Append($" [Text: \"{textContent}\"]");
            }
            else if (component is TextMeshProUGUI)
            {
                TextMeshProUGUI tmp = component as TextMeshProUGUI;
                string textContent = tmp.text;
                if (textContent.Length > 50)
                {
                    textContent = textContent.Substring(0, 50) + "...";
                }
                sb.Append($" [Text: \"{textContent}\"]");
            }
            else if (component is Image)
            {
                Image img = component as Image;
                string spriteName = img.sprite != null ? img.sprite.name : "null";
                sb.Append($" [Sprite: {spriteName}, Color: {img.color}, Type: {img.type}]");
            }
            else if (component is Button)
            {
                Button btn = component as Button;
                var targetGraphic = btn.targetGraphic != null ? ResolveGraphicName(btn.targetGraphic) : "null";
                sb.Append($" [Interactable: {btn.interactable}, TargetGraphic: {targetGraphic}]");
            }
            else if (component is CanvasGroup)
            {
                CanvasGroup cg = component as CanvasGroup;
                sb.Append($" [Alpha: {cg.alpha}, Interactable: {cg.interactable}, BlocksRaycasts: {cg.blocksRaycasts}]");
            }
            else if (component is Canvas)
            {
                Canvas canvas = component as Canvas;
                sb.Append($" [RenderMode: {canvas.renderMode}, SortOrder: {canvas.sortingOrder}]");
            }
            else if (component is Scrollbar)
            {
                Scrollbar scrollbar = component as Scrollbar;
                string handleRect = scrollbar.handleRect != null ? ResolveComponentName(scrollbar.handleRect) : "null";
                sb.Append($" [Direction: {scrollbar.direction}, Value: {scrollbar.value:F2}, Size: {scrollbar.size:F2}, Steps: {scrollbar.numberOfSteps}, HandleRect: {handleRect}]");
            }
            else if (component is ScrollRect)
            {
                ScrollRect scrollRect = component as ScrollRect;
                string content = scrollRect.content != null ? ResolveComponentName(scrollRect.content) : "null";
                string viewport = scrollRect.viewport != null ? ResolveComponentName(scrollRect.viewport) : "null";
                string hScrollbar = scrollRect.horizontalScrollbar != null ? ResolveComponentName(scrollRect.horizontalScrollbar) : "null";
                string vScrollbar = scrollRect.verticalScrollbar != null ? ResolveComponentName(scrollRect.verticalScrollbar) : "null";
                sb.Append($" [H: {scrollRect.horizontal}, V: {scrollRect.vertical}, MovementType: {scrollRect.movementType}, Inertia: {scrollRect.inertia}");
                sb.Append($", ScrollSensitivity: {scrollRect.scrollSensitivity:F2}, DecelerationRate: {scrollRect.decelerationRate:F2}, Elasticity: {scrollRect.elasticity:F2}");
                sb.Append($", Content: {content}, Viewport: {viewport}");
                if (scrollRect.horizontal && hScrollbar != "null")
                    sb.Append($", HScrollbar: {hScrollbar}");
                if (scrollRect.vertical && vScrollbar != "null")
                    sb.Append($", VScrollbar: {vScrollbar}");
                sb.Append("]");
            }
            else if (component is Toggle)
            {
                Toggle toggle = component as Toggle;
                string targetGraphic = toggle.targetGraphic != null ? ResolveGraphicName(toggle.targetGraphic) : "null";
                string graphic = toggle.graphic != null ? ResolveGraphicName(toggle.graphic) : "null";
                sb.Append($" [IsOn: {toggle.isOn}, Interactable: {toggle.interactable}, TargetGraphic: {targetGraphic}, Graphic: {graphic}]");
            }
            else if (component is Slider)
            {
                Slider slider = component as Slider;
                string fillRect = slider.fillRect != null ? ResolveComponentName(slider.fillRect) : "null";
                string handleRect = slider.handleRect != null ? ResolveComponentName(slider.handleRect) : "null";
                sb.Append($" [Direction: {slider.direction}, Value: {slider.value:F2}, Min: {slider.minValue:F2}, Max: {slider.maxValue:F2}, WholeNumbers: {slider.wholeNumbers}");
                sb.Append($", FillRect: {fillRect}, HandleRect: {handleRect}]");
            }
            else if (component is Dropdown)
            {
                Dropdown dropdown = component as Dropdown;
                string template = dropdown.template != null ? ResolveComponentName(dropdown.template) : "null";
                sb.Append($" [Value: {dropdown.value}, Options: {dropdown.options.Count}, Template: {template}]");
            }
            else if (component is InputField)
            {
                InputField inputField = component as InputField;
                string textComponent = inputField.textComponent != null ? ResolveComponentName(inputField.textComponent) : "null";
                string placeholder = inputField.placeholder != null ? ResolveGraphicName(inputField.placeholder) : "null";
                sb.Append($" [Text: \"{TruncateString(inputField.text, 30)}\", ContentType: {inputField.contentType}, TextComponent: {textComponent}, Placeholder: {placeholder}]");
            }
            else if (component is TMP_InputField)
            {
                TMP_InputField inputField = component as TMP_InputField;
                string textComponent = inputField.textComponent != null ? ResolveComponentName(inputField.textComponent) : "null";
                string placeholder = inputField.placeholder != null ? ResolveGraphicName(inputField.placeholder) : "null";
                sb.Append($" [Text: \"{TruncateString(inputField.text, 30)}\", ContentType: {inputField.contentType}, TextComponent: {textComponent}, Placeholder: {placeholder}]");
            }
            else if (component is HorizontalLayoutGroup)
            {
                HorizontalLayoutGroup layout = component as HorizontalLayoutGroup;
                sb.Append($" [Spacing: {layout.spacing}, ChildAlign: {layout.childAlignment}, ControlChildSize: W{layout.childControlWidth}/H{layout.childControlHeight}]");
            }
            else if (component is VerticalLayoutGroup)
            {
                VerticalLayoutGroup layout = component as VerticalLayoutGroup;
                sb.Append($" [Spacing: {layout.spacing}, ChildAlign: {layout.childAlignment}, ControlChildSize: W{layout.childControlWidth}/H{layout.childControlHeight}]");
            }
            else if (component is GridLayoutGroup)
            {
                GridLayoutGroup layout = component as GridLayoutGroup;
                sb.Append($" [CellSize: {layout.cellSize}, Spacing: {layout.spacing}, StartCorner: {layout.startCorner}, StartAxis: {layout.startAxis}, Constraint: {layout.constraint}]");
            }
            else if (component is ContentSizeFitter)
            {
                ContentSizeFitter fitter = component as ContentSizeFitter;
                sb.Append($" [HorizontalFit: {fitter.horizontalFit}, VerticalFit: {fitter.verticalFit}]");
            }
            else if (component is LayoutElement)
            {
                LayoutElement element = component as LayoutElement;
                sb.Append($" [MinW: {element.minWidth}, MinH: {element.minHeight}, PreferredW: {element.preferredWidth}, PreferredH: {element.preferredHeight}, FlexibleW: {element.flexibleWidth}, FlexibleH: {element.flexibleHeight}]");
            }
            else if (component is Mask)
            {
                Mask mask = component as Mask;
                sb.Append($" [ShowMaskGraphic: {mask.showMaskGraphic}]");
            }

            return sb.ToString();
        }

        private static string ResolveComponentName(Component comp)
        {
            if (comp == null) return "null";
            return $"{comp.gameObject.name}";
        }

        private static string ResolveGraphicName(Graphic graphic)
        {
            if (graphic == null) return "null";
            return $"{graphic.gameObject.name}({graphic.GetType().Name})";
        }

        private static string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength) + "...";
        }

        private static string GetPrefabInfo(GameObject obj)
        {
            try
            {
                // Check if name contains "(Clone)" which Unity adds to instantiated prefabs
                if (obj.name.Contains("(Clone)"))
                {
                    string prefabName = obj.name.Replace("(Clone)", "").Trim();
                    return $", Prefab: {prefabName}";
                }
                
                // Additional check: objects instantiated from prefabs might have different patterns
                // Some Unity versions use different naming conventions
                return "";
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogWarning($"Error getting prefab info: {ex.Message}");
                return "";
            }
        }
    }
}

