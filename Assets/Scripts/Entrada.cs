using UnityEngine;
using UnityEngine.InputSystem;

// Lectura de controles: teclado/mouse y gamepad (Xbox / PlayStation).
public static class Entrada
{
    private static float ultimoCambioHorizontal;

    static Gamepad Pad => Gamepad.current;
    static Keyboard Kb => Keyboard.current;
    static Mouse Ms => Mouse.current;

    public static Vector2 Mover()
    {
        Vector2 v = Vector2.zero;
        if (Kb != null)
        {
            if (Kb.aKey.isPressed || Kb.leftArrowKey.isPressed) v.x -= 1f;
            if (Kb.dKey.isPressed || Kb.rightArrowKey.isPressed) v.x += 1f;
            if (Kb.wKey.isPressed || Kb.upArrowKey.isPressed) v.y += 1f;
            if (Kb.sKey.isPressed || Kb.downArrowKey.isPressed) v.y -= 1f;
        }
        if (Pad != null)
        {
            Vector2 stick = Pad.leftStick.ReadValue();
            if (stick.magnitude > 0.2f) v += stick;
            v += Pad.dpad.ReadValue();
        }
        return Vector2.ClampMagnitude(v, 1f);
    }

    public static bool Saltar() =>
        (Kb != null && Kb.spaceKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    public static bool Correr() =>
        (Kb != null && (Kb.leftShiftKey.isPressed || Kb.rightShiftKey.isPressed)) ||
        (Pad != null && (Pad.rightTrigger.ReadValue() > 0.4f || Pad.leftStickButton.isPressed || Pad.buttonEast.isPressed));

    public static bool Lanzar() =>
        (Kb != null && (Kb.eKey.wasPressedThisFrame || Kb.fKey.wasPressedThisFrame)) ||
        (Ms != null && Ms.leftButton.wasPressedThisFrame) ||
        (Pad != null && (Pad.buttonWest.wasPressedThisFrame || Pad.rightShoulder.wasPressedThisFrame));

    public static bool Pausa() =>
        (Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.pKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.startButton.wasPressedThisFrame);

    public static bool Cancelar() =>
        (Kb != null && Kb.escapeKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonEast.wasPressedThisFrame);

    public static bool Saltear() =>
        (Kb != null && (Kb.spaceKey.wasPressedThisFrame || Kb.enterKey.wasPressedThisFrame || Kb.escapeKey.wasPressedThisFrame)) ||
        (Ms != null && Ms.leftButton.wasPressedThisFrame) ||
        (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame));

    // -1 izquierda, +1 derecha (con repetición controlada para el stick)
    public static int CambioHorizontal()
    {
        if (Kb != null)
        {
            if (Kb.aKey.wasPressedThisFrame || Kb.leftArrowKey.wasPressedThisFrame) return -1;
            if (Kb.dKey.wasPressedThisFrame || Kb.rightArrowKey.wasPressedThisFrame) return 1;
        }
        if (Pad != null)
        {
            if (Pad.dpad.left.wasPressedThisFrame || Pad.leftShoulder.wasPressedThisFrame) return -1;
            if (Pad.dpad.right.wasPressedThisFrame || Pad.rightShoulder.wasPressedThisFrame) return 1;
            float x = Pad.leftStick.ReadValue().x;
            if (Mathf.Abs(x) > 0.6f && Time.unscaledTime - ultimoCambioHorizontal > 0.35f)
            {
                ultimoCambioHorizontal = Time.unscaledTime;
                return x > 0f ? 1 : -1;
            }
        }
        return 0;
    }

    public static bool HayGamepad => Pad != null;
}
