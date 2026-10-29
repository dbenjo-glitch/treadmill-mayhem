using System.Collections;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Gameplay: fixed three-quarter view of the whole belt that leans gently toward the player, with shake.
    /// Menu: a close-up on the console start button. Intro: a fly-out from the button, over the arena,
    /// into a focus shot on the player, then down into the gameplay view.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public Vector3 lookPoint;
        public float followAmount = 0.25f;
        public float smooth = 4f;

        [Header("Lens")]
        public float baseFov = 50f;
        public float fovPerSpeed = 0.8f;
        public float menuFov = 38f;

        [Header("Intro fly-out")]
        public Vector3 revealPosition = new Vector3(13f, 15f, -24f);
        public float introDuration = 3.4f;

        public bool InMenu { get; private set; }
        public bool PlayingIntro { get; private set; }

        static float shake;
        Vector3 basePos;
        Vector3 offset;
        Camera cam;
        Treadmill treadmill;
        StartButton button;
        Vector3 menuPos, menuLook;

        public static void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void Awake()
        {
            basePos = transform.position;
            cam = GetComponent<Camera>();
        }

        void Start() => treadmill = FindAnyObjectByType<Treadmill>();

        /// <summary>Parks the camera on a close-up of the start button.</summary>
        public void EnterMenu(StartButton startButton)
        {
            button = startButton;
            InMenu = true;
            menuLook = button.transform.position + button.Normal * 0.15f;
            // A three-quarter view from the belt side: shows the button's height, the console and the handlebars.
            var dir = (button.Normal * 0.6f - Vector3.forward * 0.8f).normalized;
            menuPos = menuLook + dir * 4.8f;
            transform.position = menuPos;
            transform.LookAt(menuLook);
            cam.fieldOfView = menuFov;
        }

        public void PlayIntro(System.Action onDone) => StartCoroutine(IntroRoutine(onDone));

        IEnumerator IntroRoutine(System.Action onDone)
        {
            InMenu = false;
            PlayingIntro = true;
            AudioFX.Play(Sfx.Whoosh, 0.9f, 0.8f);

            var focusTarget = target ? target.position : lookPoint;
            var tunaPos = focusTarget + new Vector3(0.8f, 1.8f, -3.6f);
            var center = treadmill ? treadmill.transform.position : lookPoint;

            // Catmull-Rom through: button close-up → wide reveal over the arena → tuna focus → gameplay view.
            var pts = new[] { menuPos, menuPos, revealPosition, tunaPos, basePos, basePos };
            var looks = new[] { menuLook, menuLook, center, focusTarget, lookPoint, lookPoint };
            var fovs = new[] { menuFov, menuFov, 58f, 40f, baseFov, baseFov };
            // How much of the run each leg takes: the reveal lingers, the tuna focus holds briefly.
            var legs = new[] { 0.4f, 0.3f, 0.3f };

            for (int leg = 0; leg < 3; leg++)
            {
                float dur = introDuration * legs[leg];
                // Cap the step so a frame hitch can't skip the shot.
                for (float t = 0f; t < 1f; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / dur)
                {
                    float e = Mathf.SmoothStep(0f, 1f, t);
                    transform.position = CatmullRom(pts[leg], pts[leg + 1], pts[leg + 2], pts[leg + 3], e);
                    var look = Vector3.Lerp(looks[leg + 1], looks[leg + 2], Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.3f)));
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
                    cam.fieldOfView = AspectFov(Mathf.Lerp(fovs[leg + 1], fovs[leg + 2], e));
                    yield return null;
                }
                if (leg == 1)
                {
                    // Beat on the tuna before dropping into the game view.
                    var racer = target ? target.GetComponent<Racer>() : null;
                    if (racer) racer.Squash(0.3f);
                    yield return new WaitForSecondsRealtime(0.35f);
                }
            }

            transform.position = basePos;
            transform.LookAt(lookPoint);
            offset = Vector3.zero;
            PlayingIntro = false;
            onDone?.Invoke();
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        void LateUpdate()
        {
            if (PlayingIntro) return;
            if (InMenu)
            {
                // Tiny idle sway so the menu shot feels alive.
                float t = Time.unscaledTime;
                transform.position = menuPos + new Vector3(Mathf.Sin(t * 0.6f) * 0.08f, Mathf.Sin(t * 0.8f) * 0.05f, 0f) + Random.insideUnitSphere * shake * 0.2f;
                transform.LookAt(menuLook);
                shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.5f);
                return;
            }

            if (followingFall && target != null)
            {
                // Cinematic beat on elimination: swing in and watch the player tumble off.
                var want = target.position + new Vector3(2.5f, 4f, -7f);
                transform.position = Vector3.Lerp(transform.position, want, Time.unscaledDeltaTime * 2.5f);
                var rot = Quaternion.LookRotation(target.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.unscaledDeltaTime * 4f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 42f, Time.unscaledDeltaTime * 2f);
                return;
            }

            var desired = Vector3.zero;
            float danger = 0f;
            if (target != null)
            {
                var d = target.position - lookPoint;
                desired = new Vector3(d.x, 0f, d.z) * followAmount;
                // Dynamic zoom: lean in when the player is close to the rear edge.
                if (treadmill) danger = Mathf.InverseLerp(treadmill.RearZ + treadmill.beltLength * 0.3f, treadmill.RearZ, target.position.z);
            }
            offset = Vector3.Lerp(offset, desired, Time.unscaledDeltaTime * smooth);
            dangerZoom = Mathf.Lerp(dangerZoom, danger, Time.unscaledDeltaTime * 2f);

            var playerRacer = target ? target.GetComponent<Racer>() : null;
            bool superOn = playerRacer && playerRacer.Super && playerRacer.Super.IsActive;
            float superShake = superOn ? 0.04f : 0f;

            var toTarget = target ? (target.position - basePos) : Vector3.zero;
            transform.position = basePos + offset + toTarget * 0.18f * dangerZoom + Random.insideUnitSphere * (shake * 0.5f + superShake);
            transform.LookAt(lookPoint + offset * 0.8f + (target ? (target.position - lookPoint) * 0.3f * dangerZoom : Vector3.zero));
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.5f);

            // Widen the lens a touch as the belt speeds up, a lot during Super Speed, and tighten when in danger.
            if (cam && treadmill)
            {
                float fov = baseFov + treadmill.speed * fovPerSpeed - dangerZoom * 4f + (superOn ? 10f : 0f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, AspectFov(fov), Time.unscaledDeltaTime * 2.5f);
            }
        }

        bool followingFall;
        float dangerZoom;

        /// <summary>Switches to the elimination shot that follows the target as it falls.</summary>
        public void FollowFall() => followingFall = true;

        /// <summary>
        /// FOVs are tuned for 16:9. Narrower screens (iPad 4:3) widen the vertical FOV so the treadmill keeps
        /// the same horizontal framing; wider phones keep the tuned value.
        /// </summary>
        float AspectFov(float fov16x9)
        {
            const float tuned = 16f / 9f;
            float aspect = cam.aspect;
            if (aspect >= tuned || aspect <= 0f) return fov16x9;
            float halfH = Mathf.Atan(Mathf.Tan(fov16x9 * 0.5f * Mathf.Deg2Rad) * tuned);
            return Mathf.Min(80f, 2f * Mathf.Atan(Mathf.Tan(halfH) / aspect) * Mathf.Rad2Deg);
        }
    }
}
