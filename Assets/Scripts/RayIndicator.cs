using UnityEngine;
using Oculus.Interaction;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

public class RayIndicator : MonoBehaviour
{
    public Transform rayOrigin;
    public LineRenderer line;
    public float maxDistance = 10f;
    public RaycastHit hit;
    public bool hasHit;

    public Ray ray;
    public Vector3 endPoint;
    // track previous state for right-hand trigger edge detection
    private bool prevRightTrigger = false;
    private bool prevRightGrip = false;
    private bool prevRightPrimary = false;
    private GameObject grabbedObject = null;
    private Rigidbody grabbedRb = null;
    
    [SerializeField]
    private GameObject menu;

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


        // Controller-only: use right-hand XR controller trigger (Meta/Oculus)
        bool clicked = false;
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool rightTriggerPressed = false;
        bool rightGripPressed = false;
        bool rightPrimaryPressed = false;
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out rightTriggerPressed))
        {
            // edge-detect: only trigger on press down this frame
            if (rightTriggerPressed && !prevRightTrigger)
            {
                clicked = true;
            }
        }

        // Primary / A button handling (toggle grab/release)
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed))
        {
            if (rightPrimaryPressed && !prevRightPrimary)
            {
                // On primary-button press edge
                if (grabbedObject == null)
                {
                    // Try to grab the currently pointed object
                    if (hasHit && hit.collider != null)
                    {
                        var target = hit.collider.gameObject;
                        // detach from any parent (e.g., agent hand)
                        target.transform.SetParent(null, true);
                        // attempt to use Rigidbody if present
                        var rb = target.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            // make kinematic while held to avoid physics interference
                            rb.isKinematic = true;
                            grabbedRb = rb;
                        }
                        // parent to the controller ray origin so it follows the controller
                        target.transform.SetParent(rayOrigin, true);
                        grabbedObject = target;
                    }
                }
                else
                {
                    // release currently grabbed object
                    grabbedObject.transform.SetParent(null, true);
                    if (grabbedRb != null)
                    {
                        grabbedRb.isKinematic = false;
                        grabbedRb = null;
                    }
                    grabbedObject = null;
                }
            }
        }

        // update previous primary state for next frame
        prevRightPrimary = rightPrimaryPressed;

        // update previous state for trigger next frame
        prevRightTrigger = rightTriggerPressed;

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

        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.gripButton, out rightGripPressed))
        {
            // edge-detect: only trigger on press down this frame
            Debug.Log("Grip state: " + rightGripPressed);
            if (rightGripPressed && !prevRightGrip)
            {
                Debug.Log("Menu open");
                openMenu();
                
            }
        }
        // update previous state for next frame
        prevRightGrip = rightGripPressed;


    }

    void openMenu() {
        if (menu != null) {
            menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        }
    }


}