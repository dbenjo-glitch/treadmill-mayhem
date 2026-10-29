using System.Collections;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// A low-poly spectator filming the race on a phone. Built from primitives at runtime so it costs no assets;
    /// swap <see cref="CrowdManager.spectatorModel"/> for a Tripo model to replace the look.
    /// Idles, tracks the action with head and phone, occasionally flashes, and cheers at eliminations.
    /// </summary>
    public class Spectator : MonoBehaviour
    {
        public Color shirt = Color.red;
        public Color pants = new Color(0.2f, 0.25f, 0.45f);
        public Color skin = new Color(0.95f, 0.78f, 0.62f);
        public Color hair = new Color(0.25f, 0.15f, 0.08f);
        public bool wearsCap;

        Transform torso, head, phoneArm, freeArm, phoneFlash;
        Light flashLight;
        float seed, cheerUntil, nextFlash, nextPose, phoneRaise = -95f, phoneRaiseTarget = -95f;
        Vector3 basePos;

        void Start()
        {
            seed = Random.value * 100f;
            basePos = transform.localPosition;
            if (transform.childCount == 0) Build();
            else CacheParts();
            nextFlash = Time.time + Random.Range(2f, 8f);
        }

        void Build()
        {
            var shirtMat = Effects.SharedMat(shirt);
            var pantsMat = Effects.SharedMat(pants);
            var skinMat = Effects.SharedMat(skin);

            foreach (float x in new[] { -0.12f, 0.12f })
                Part(PrimitiveType.Capsule, transform, "Leg", new Vector3(x, 0.42f, 0f), new Vector3(0.18f, 0.42f, 0.18f), pantsMat);

            torso = new GameObject("Torso").transform;
            torso.SetParent(transform, false);
            torso.localPosition = new Vector3(0f, 0.85f, 0f);
            Part(PrimitiveType.Capsule, torso, "Body", new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.38f, 0.32f), shirtMat);

            head = new GameObject("Head").transform;
            head.SetParent(torso, false);
            head.localPosition = new Vector3(0f, 0.82f, 0f);
            Part(PrimitiveType.Sphere, head, "Face", Vector3.zero, Vector3.one * 0.34f, skinMat);
            if (wearsCap)
            {
                Part(PrimitiveType.Sphere, head, "Cap", new Vector3(0f, 0.07f, 0f), new Vector3(0.36f, 0.22f, 0.36f), Effects.SharedMat(shirt * 0.7f + Color.white * 0.3f));
                Part(PrimitiveType.Cube, head, "Brim", new Vector3(0f, 0.06f, 0.17f), new Vector3(0.26f, 0.03f, 0.16f), Effects.SharedMat(shirt * 0.7f + Color.white * 0.3f));
            }
            else Part(PrimitiveType.Sphere, head, "Hair", new Vector3(0f, 0.06f, -0.03f), new Vector3(0.36f, 0.26f, 0.36f), Effects.SharedMat(hair));

            phoneArm = Arm(torso, new Vector3(0.3f, 0.58f, 0f), shirtMat, skinMat);
            freeArm = Arm(torso, new Vector3(-0.3f, 0.58f, 0f), shirtMat, skinMat);

            // Phone held up in the raised hand, screen facing the spectator, camera facing the treadmill.
            var phone = Part(PrimitiveType.Cube, phoneArm, "Phone", new Vector3(0f, -0.6f, 0.04f), new Vector3(0.14f, 0.03f, 0.24f), Effects.SharedMat(new Color(0.1f, 0.1f, 0.12f)));
            var screen = Part(PrimitiveType.Cube, phone.transform, "Screen", new Vector3(0f, 0.6f, 0f), new Vector3(0.85f, 0.2f, 0.85f), null);
            var screenMat = Effects.MakeMat(new Color(0.5f, 0.85f, 1f), 0.9f);
            screenMat.EnableKeyword("_EMISSION");
            screenMat.SetColor("_EmissionColor", new Color(0.4f, 0.7f, 1f));
            screen.GetComponent<Renderer>().sharedMaterial = screenMat;

            phoneFlash = Part(PrimitiveType.Sphere, phone.transform, "Flash", new Vector3(0.3f, -0.8f, 0.35f), Vector3.one * 0.6f, Effects.MakeMat(new Color(1f, 1f, 0.9f), 1f)).transform;
            phoneFlash.gameObject.SetActive(false);
            flashLight = phoneFlash.gameObject.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.range = 4f;
            flashLight.intensity = 3f;
            flashLight.shadows = LightShadows.None;
        }

        void CacheParts()
        {
            torso = transform.Find("Torso");
            head = torso ? torso.Find("Head") : null;
        }

        static GameObject Part(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static Transform Arm(Transform torso, Vector3 shoulder, Material sleeve, Material hand)
        {
            var pivot = new GameObject("ArmPivot").transform;
            pivot.SetParent(torso, false);
            pivot.localPosition = shoulder;
            Part(PrimitiveType.Capsule, pivot, "Sleeve", new Vector3(0f, -0.2f, 0f), new Vector3(0.14f, 0.22f, 0.14f), sleeve);
            Part(PrimitiveType.Sphere, pivot, "Hand", new Vector3(0f, -0.5f, 0f), Vector3.one * 0.12f, hand);
            return pivot;
        }

        public void Cheer(float delay) => StartCoroutine(CheerRoutine(delay));

        IEnumerator CheerRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            cheerUntil = Time.time + Random.Range(0.9f, 1.5f);
        }

        void Update()
        {
            if (torso == null) return;
            float t = Time.time + seed;
            bool cheering = Time.time < cheerUntil;

            // Idle sway and breathing; cheering adds little hops.
            float hop = cheering ? Mathf.Abs(Mathf.Sin(t * 12f)) * 0.25f : 0f;
            transform.localPosition = basePos + Vector3.up * hop;
            torso.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 3f + (cheering ? -8f : 4f), Mathf.Sin(t * 0.4f) * 6f, Mathf.Sin(t * 0.9f) * 2f);

            // Head follows the player.
            var player = GameManager.Instance ? GameManager.Instance.Player : null;
            if (head && player && !player.IsEliminated)
            {
                var look = Quaternion.LookRotation(player.transform.position - head.position);
                head.rotation = Quaternion.Slerp(head.rotation, look, Time.deltaTime * 2f);
            }

            // Phone arm: mostly held up filming, sometimes lifted higher for a better angle.
            if (Time.time >= nextPose)
            {
                nextPose = Time.time + Random.Range(2f, 6f);
                phoneRaiseTarget = Random.value < 0.3f ? -140f : -95f;
            }
            phoneRaise = Mathf.Lerp(phoneRaise, cheering ? -165f : phoneRaiseTarget, Time.deltaTime * 4f);
            if (phoneArm) phoneArm.localRotation = Quaternion.Euler(phoneRaise, 0f, -10f);
            if (freeArm) freeArm.localRotation = cheering
                ? Quaternion.Euler(-160f + Mathf.Sin(t * 14f) * 15f, 0f, 20f)
                : Quaternion.Euler(Mathf.Sin(t * 0.8f) * 5f, 0f, 8f);

            // Camera flash.
            if (phoneFlash && Time.time >= nextFlash)
            {
                nextFlash = Time.time + Random.Range(3f, 10f);
                StartCoroutine(Flash());
            }
        }

        IEnumerator Flash()
        {
            phoneFlash.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.08f);
            phoneFlash.gameObject.SetActive(false);
        }
    }
}
