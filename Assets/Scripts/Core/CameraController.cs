using UnityEngine;

namespace DnDTactics.Core
{
    // Tactical camera: pan (WASD/arrows + screen-edge), zoom (scroll, orthographic size),
    // and recenter on a target (F). Keeps the fixed iso ANGLE — only translates + zooms,
    // so grid/click mapping (which depends on angle) is unaffected.
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Pan")]
        public float panSpeed = 18f;        // world units/sec at default zoom
        public bool edgeScroll = true;
        public float edgeThicknessPx = 12f; // how close to the edge triggers edge-scroll

        [Header("Zoom (orthographic size)")]
        public float zoomSpeed = 8f;
        public float minOrthoSize = 4f;
        public float maxOrthoSize = 22f;

        [Header("Recenter")]
        public KeyCode recenterKey = KeyCode.F;

        private Camera cam;
        private Vector3 flatForward, flatRight; // ground-plane pan axes from the camera's yaw

        // Optional target to recenter on (set by exploration/combat to the selected character).
        public Transform recenterTarget;

        void Awake()
        {
            cam = GetComponent<Camera>();
            // Flatten the camera's forward/right onto the XZ plane for screen-aligned panning.
            flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            if (cam.orthographic)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minOrthoSize, maxOrthoSize);
        }

        void Update()
        {
            HandlePan();
            HandleZoom();
            if (Input.GetKeyDown(recenterKey)) Recenter();
        }

        void HandlePan()
        {
            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;

            if (edgeScroll)
            {
                Vector3 mp = Input.mousePosition;
                if (mp.x >= 0 && mp.x <= Screen.width && mp.y >= 0 && mp.y <= Screen.height)
                {
                    if (mp.x <= edgeThicknessPx) h -= 1f;
                    else if (mp.x >= Screen.width - edgeThicknessPx) h += 1f;
                    if (mp.y <= edgeThicknessPx) v -= 1f;
                    else if (mp.y >= Screen.height - edgeThicknessPx) v += 1f;
                }
            }

            if (h == 0f && v == 0f) return;

            // Scale pan speed with zoom so it feels consistent (slower when zoomed in).
            float zoomFactor = cam.orthographicSize / maxOrthoSize;
            float speed = panSpeed * Mathf.Lerp(0.4f, 1f, zoomFactor);

            Vector3 move = (flatRight * h + flatForward * v).normalized * speed * Time.deltaTime;
            transform.position += move;
        }

        void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;
            if (!cam.orthographic) return;
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
        }

        // Recenter the camera over a target (keeps height/angle; shifts XZ so target is centered).
        public void Recenter()
        {
            if (recenterTarget == null) return;
            // Move the camera so its view centers on the target. Because the camera is tilted,
            // we shift along the flattened axes by the XZ delta to the target.
            Vector3 t = recenterTarget.position;
            // Find where the camera currently "looks at" on the ground (y = target's y), then
            // shift by the difference. Simpler: offset camera by the XZ delta of target vs. the
            // camera's ground-projected focus.
            Vector3 focus = GroundFocus(t.y);
            Vector3 delta = new Vector3(t.x - focus.x, 0f, t.z - focus.z);
            transform.position += delta;
        }

        // Where the camera's center ray hits the horizontal plane at height y.
        Vector3 GroundFocus(float y)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float t = (y - ray.origin.y) / ray.direction.y;
            return ray.origin + ray.direction * t;
        }
    }
}