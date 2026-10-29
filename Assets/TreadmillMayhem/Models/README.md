# Swapping in new models

Every racer is two layers:

- **Physics root**: Rigidbody, a simple Capsule (cans) or Sphere (balls/fruit) collider, `Racer`, and the
  stamina/acceleration systems. It never changes when you swap art.
- **Visual child** called `Model`: just the mesh you see.

## Replace a racer's model

1. Export from Tripo as `.glb` (or `.fbx`) and drop it in this folder.
2. Open `Assets/TreadmillMayhem/Config/Racers/<Racer>.asset`.
3. Drag the new model into **Visual Model**. Adjust **Visual Size** or **Visual Rotation** if it's the wrong
   size or facing. Cans should have their long axis along X, lying on their side.
4. Run **Tools ▸ Treadmill Mayhem ▸ Build Scene**. The collider is re-fitted to the new mesh automatically.

All tuning (mass, friction, bounce, acceleration, steering, stability, wobble) lives in the same asset and
applies live in Play mode. You don't need to rebuild for those.

## Other replaceable art

- **Spectators**: assign a model to `Crowd ▸ CrowdManager ▸ Spectator Model`. It's empty by default, which
  uses the built-in low-poly people.
- **Treadmill**: `treadmill.glb`. The belt physics box, walls and start button are fitted from its shape when
  the scene is built.
- **Audio**: assign clips on `Main Camera ▸ AudioFX` (music, crowd). Everything else falls back to
  synthesised placeholders.
