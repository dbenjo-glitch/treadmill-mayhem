using System.Reflection;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Simulated human player for automated playtests. Takes over the player's racer with the NPC brain,
    /// tuned to a 0..1 skill (blended between the five AI difficulty profiles), and adds what only the
    /// player has: Super Speed timing. Low-skill bots also get a "distraction" wobble like a new player.
    /// </summary>
    public class PlaytestBot : MonoBehaviour
    {
        public float skill;

        Racer racer;
        AIController brain;
        float nextSuperCheck, distractedUntil, nextDistraction;
        Vector2 distraction;

        public static PlaytestBot Attach(Racer player, float skill, AIDifficultyProfile[] profiles)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.enabled = false;
            var bot = player.gameObject.AddComponent<PlaytestBot>();
            bot.skill = skill;
            bot.racer = player;
            bot.brain = player.GetComponent<AIController>();
            if (bot.brain == null) bot.brain = player.gameObject.AddComponent<AIController>();
            bot.brain.SetProfile(Blend(profiles, skill));
            bot.nextDistraction = Time.time + Random.Range(3f, 8f);
            return bot;
        }

        /// <summary>Linear blend across the difficulty ladder: 0 = Easy, 1 = Insane.</summary>
        static AIDifficultyProfile Blend(AIDifficultyProfile[] profiles, float skill)
        {
            var result = ScriptableObject.CreateInstance<AIDifficultyProfile>();
            if (profiles == null || profiles.Length == 0) return result;
            float t = Mathf.Clamp01(skill) * (profiles.Length - 1);
            int a = Mathf.FloorToInt(t), b = Mathf.Min(profiles.Length - 1, a + 1);
            float f = t - a;
            var pa = profiles[a] != null ? profiles[a] : result;
            var pb = profiles[b] != null ? profiles[b] : result;
            foreach (var field in typeof(AIDifficultyProfile).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (field.FieldType == typeof(float))
                    field.SetValue(result, Mathf.Lerp((float)field.GetValue(pa), (float)field.GetValue(pb), f));
            result.displayName = "BOT " + Mathf.RoundToInt(skill * 100f);
            return result;
        }

        void LateUpdate()
        {
            if (racer == null || racer.IsEliminated || GameManager.Instance == null || !GameManager.Instance.IsRacing) return;

            // New players get distracted: brief random steering, more often the lower the skill (not in the
            // first seconds, which would just measure the bot walking off the edge before the game starts).
            if (Time.time >= nextDistraction && GameManager.Instance.ElapsedTime > 4f)
            {
                float chance = Mathf.Lerp(0.6f, 0.02f, skill);
                if (Random.value < chance)
                {
                    distraction = Random.insideUnitCircle;
                    distractedUntil = Time.time + Mathf.Lerp(1.2f, 0.2f, skill);
                }
                nextDistraction = Time.time + Random.Range(3f, 8f);
            }
            if (Time.time < distractedUntil) racer.MoveInput = Vector2.ClampMagnitude(racer.MoveInput * 0.3f + distraction, 1f);

            // Super Speed: skilled players fire it promptly, beginners late or never.
            if (racer.Super != null && Time.time >= nextSuperCheck)
            {
                nextSuperCheck = Time.time + Mathf.Lerp(6f, 0.5f, skill);
                if (racer.Super.CanActivate && skill > 0.25f && Random.value < skill) racer.Super.TryActivate();
            }
        }
    }
}
