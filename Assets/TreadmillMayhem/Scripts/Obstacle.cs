using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    public enum ObstacleKind { Block, Roller, Crate }

    /// <summary>Simple stylised props dropped onto the front of the belt that ride back through the pack.</summary>
    public class Obstacle : MonoBehaviour
    {
        public static readonly List<Obstacle> Active = new List<Obstacle>();
        /// <summary>Set once this obstacle touches the player (so it doesn't count as a dodge).</summary>
        public bool HitPlayer { get; private set; }
        Treadmill treadmill;

        /// <summary>A long candy-striped bar lying across part of the belt (Obstacle Mode "Rollers" wave).</summary>
        public static Obstacle SpawnRollerBar(Vector3 pos, float length)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Obstacle_RollerBar";
            Destroy(go.GetComponent<Collider>());
            var cap = go.AddComponent<CapsuleCollider>();
            cap.direction = 1; cap.radius = 0.5f; cap.height = 2f;
            go.transform.localScale = new Vector3(0.75f, length * 0.5f, 0.75f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            go.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(new Color(1f, 0.4f, 0.6f));
            int stripes = Mathf.Max(2, Mathf.RoundToInt(length / 0.8f));
            for (int i = 0; i < stripes; i++)
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(band.GetComponent<Collider>());
                band.transform.SetParent(go.transform, false);
                band.transform.localPosition = new Vector3(0f, -1f + (i + 0.5f) * 2f / stripes, 0f);
                band.transform.localScale = new Vector3(1.03f, 0.25f / stripes * 2f, 1.03f);
                band.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(Color.white);
            }
            go.transform.position = pos;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Keep it lying across the belt so it sweeps a lane rather than spinning out.
            rb.constraints = RigidbodyConstraints.FreezeRotationY;
            return go.AddComponent<Obstacle>();
        }

        public static Obstacle Spawn(ObstacleKind kind, Vector3 pos)
        {
            GameObject go;
            Rigidbody rb;
            switch (kind)
            {
                case ObstacleKind.Roller:
                {
                    // Short cylinder lying across the belt; it rolls with the belt and tumbles racers.
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = "Obstacle_Roller";
                    Destroy(go.GetComponent<Collider>());
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.direction = 1; cap.radius = 0.5f; cap.height = 2f;
                    go.transform.localScale = new Vector3(0.7f, 0.55f, 0.7f);
                    go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    go.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(new Color(0.55f, 0.3f, 0.95f));
                    var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Destroy(band.GetComponent<Collider>());
                    band.transform.SetParent(go.transform, false);
                    band.transform.localScale = new Vector3(1.03f, 0.15f, 1.03f);
                    band.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(Color.white);
                    rb = go.AddComponent<Rigidbody>();
                    rb.mass = 2.5f;
                    break;
                }
                case ObstacleKind.Crate:
                {
                    // Lightweight crate: big but easy to shove.
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Obstacle_Crate";
                    go.transform.localScale = Vector3.one * 0.85f;
                    go.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(new Color(0.95f, 0.72f, 0.35f));
                    foreach (var y in new[] { -0.3f, 0.3f })
                    {
                        var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(slat.GetComponent<Collider>());
                        slat.transform.SetParent(go.transform, false);
                        slat.transform.localPosition = new Vector3(0f, y, 0f);
                        slat.transform.localScale = new Vector3(1.02f, 0.12f, 1.02f);
                        slat.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(new Color(0.75f, 0.5f, 0.25f));
                    }
                    rb = go.AddComponent<Rigidbody>();
                    rb.mass = 1.2f;
                    break;
                }
                default:
                {
                    // Small dense block.
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Obstacle_Block";
                    go.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
                    go.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(new Color(1f, 0.45f, 0.35f));
                    rb = go.AddComponent<Rigidbody>();
                    rb.mass = 3.5f;
                    break;
                }
            }
            go.transform.position = pos;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return go.AddComponent<Obstacle>();
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);
        void Start() => treadmill = FindAnyObjectByType<Treadmill>();

        void Update()
        {
            if (treadmill && treadmill.IsOffTreadmill(transform.position)) Destroy(gameObject);
        }

        void OnCollisionEnter(Collision c)
        {
            var racer = c.rigidbody ? c.rigidbody.GetComponent<Racer>() : null;
            if (racer == null) return;
            if (racer.isPlayer) HitPlayer = true;
            if (c.relativeVelocity.magnitude < 2f) return;
            var dir = racer.Body.position - transform.position;
            dir.y = 0f;
            // Obstacle Mode is about dodging, so getting clipped hurts more there.
            float k = Session.IsObstacle ? 1.6f : 1f;
            racer.TakeKnock(dir.normalized * 2.5f * k + Vector3.up * 1f);
            Effects.Burst(c.GetContact(0).point, Color.white, 8);
            AudioFX.Play(Sfx.Bump, 0.5f, 0.8f);
            racer.Squash(0.2f);
            if (racer.isPlayer) CameraRig.Shake(0.15f);
        }
    }
}
