using UnityEngine;
using UnityEngine.InputSystem;

namespace TreadmillMayhem
{
    /// <summary>
    /// Human input for a Racer. Reads keyboard (WASD/arrows, Shift = stamina boost, Space = Super Speed),
    /// gamepad (left stick, right trigger = boost, south button = Super Speed) and a drag-anywhere virtual
    /// joystick for mouse/touch. The Racer itself never knows whether a human or the AI is driving it.
    /// </summary>
    [RequireComponent(typeof(Racer))]
    public class PlayerController : MonoBehaviour
    {
        public float dragRadiusPixels = 110f;

        public static Vector2 DragOrigin { get; private set; }
        public static Vector2 DragCurrent { get; private set; }
        public static bool Dragging { get; private set; }

        Racer racer;

        void Awake() => racer = GetComponent<Racer>();

        void Update()
        {
            bool racing = GameManager.Instance != null && GameManager.Instance.IsRacing && !GameManager.Instance.Paused;
            if (racer.IsEliminated || !racing)
            {
                racer.MoveInput = Vector2.zero;
                racer.BoostHeld = false;
                Dragging = false;
                return;
            }

            Vector2 input = Vector2.zero;
            bool boost = false, super = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                boost |= kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                super |= kb.spaceKey.wasPressedThisFrame;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                input += pad.leftStick.ReadValue();
                boost |= pad.rightTrigger.isPressed;
                super |= pad.buttonSouth.wasPressedThisFrame;
            }

            if (TouchControls.Active)
            {
                input += TouchControls.Move;
                boost |= TouchControls.BoostHeld;
                super |= TouchControls.ConsumeSuper();
            }
            input += ReadDrag();

            // Normalised so diagonals are no faster than straight lines.
            racer.MoveInput = Vector2.ClampMagnitude(input, 1f);
            racer.BoostHeld = boost;
            if (super && racer.Super != null) racer.Super.TryActivate();
        }

        Vector2 ReadDrag()
        {
            bool pressed = false;
            Vector2 pos = Vector2.zero;

            // On touch devices the dual-thumb TouchControls own the screen; the drag stick is for mouse only.
            var touch = TouchControls.Active ? null : Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                pressed = true;
                pos = touch.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pressed = true;
                pos = Mouse.current.position.ReadValue();
            }

            if (!pressed) { Dragging = false; return Vector2.zero; }
            if (!Dragging) { Dragging = true; DragOrigin = pos; }
            DragCurrent = pos;
            return Vector2.ClampMagnitude((pos - DragOrigin) / dragRadiusPixels, 1f);
        }
    }
}
