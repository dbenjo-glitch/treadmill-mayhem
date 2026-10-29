using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Fires a burst of confetti whenever a racer is eliminated or a level is cleared.</summary>
    public class ConfettiCannon : MonoBehaviour
    {
        public ParticleSystem burst;

        void OnEnable() { Racer.Eliminated += OnEliminated; GameManager.Celebrate += Fire; }
        void OnDisable() { Racer.Eliminated -= OnEliminated; GameManager.Celebrate -= Fire; }

        void OnEliminated(Racer r) => Fire();

        public void Fire()
        {
            if (burst == null || !isActiveAndEnabled) return;
            burst.Emit(60);
            AudioFX.Play(Sfx.Whoosh, 0.25f, 1.6f);
        }
    }
}
