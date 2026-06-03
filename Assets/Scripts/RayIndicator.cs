using UnityEngine;
using UnityEngine.InputSystem;
using Oculus.Interaction;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RayIndicator : MonoBehaviour
{
    public Transform rayOrigin;
    public LineRenderer line;
    public float maxDistance = 10f;
    public RaycastHit hit;
    public bool hasHit;

    public Ray ray;
    public Vector3 endPoint;
    
    void Update()
    {
        // Variable for new ray (actual ray itself)
        ray = new Ray(rayOrigin.position, rayOrigin.forward);

        // Variable for the endpoint of the ray (where it lands)
        endPoint = rayOrigin.position + rayOrigin.forward * maxDistance;


        hasHit = Physics.Raycast(ray, out hit, maxDistance);
        if (hasHit)
        {
            endPoint = hit.point;
        }
   

        //Adjusts visible line to match ray
        line.SetPosition(0, rayOrigin.position);
        line.SetPosition(1, endPoint);


        // Detect primary click (supports new Input System mouse, keyboard/gamepad Up, and legacy input)
        bool clicked = false;

        // New Input System: mouse
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
        }

        // New Input System: keyboard Up arrow
        if (!clicked)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.upArrowKey.wasPressedThisFrame)
                clicked = true;
        }

        // New Input System: gamepad dpad Up
        if (!clicked)
        {
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && gp.dpad.up.wasPressedThisFrame)
                clicked = true;
        }

        // Legacy input fallback: mouse left or UpArrow key
        if (!clicked)
        {
            clicked = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        if (clicked)
        {
            // Ensure EventSystem exists
            if (EventSystem.current == null)
            {
                var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var ped = new PointerEventData(EventSystem.current);

            bool dispatched = false;

            // First: if physics hit, try ExecuteEvents up the hierarchy (works if UI has collider or IPointerClickHandler on gameobject)
            if (hasHit && hit.collider != null)
            {
                ExecuteEvents.ExecuteHierarchy(hit.collider.gameObject, ped, ExecuteEvents.pointerClickHandler);
                dispatched = true;
            }

            // Second: try GraphicRaycaster-based UI raycast (for Canvas UI elements)
            if (!dispatched)
            {
                // Need a camera to convert world point to screen point
                var cam = Camera.main;
                if (cam != null)
                {
                    // Convert the ray endpoint (or hit point) to screen position
                    Vector3 screenPos = cam.WorldToScreenPoint(endPoint);
                    ped.position = new Vector2(screenPos.x, screenPos.y);

                    // Raycast all GraphicRaycasters in scene
                    GraphicRaycaster[] raycasters = GameObject.FindObjectsOfType<GraphicRaycaster>();
                    var results = new System.Collections.Generic.List<RaycastResult>();
                    foreach (var gr in raycasters)
                    {
                        // Only raycast canvases that are enabled
                        if (gr == null || !gr.gameObject.activeInHierarchy) continue;
                        results.Clear();
                        gr.Raycast(ped, results);
                        if (results.Count > 0)
                        {
                            // Execute click on the first result
                            ExecuteEvents.ExecuteHierarchy(results[0].gameObject, ped, ExecuteEvents.pointerClickHandler);
                            dispatched = true;
                            break;
                        }
                    }
                }
            }
        }

    }


}