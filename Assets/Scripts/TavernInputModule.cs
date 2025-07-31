using UnityEngine;
using UnityEngine.EventSystems;

public class TavernInputModule : StandaloneInputModule
{

    protected override MouseState GetMousePointerEventData(int id)
    {
        UnlockCursor();
        MouseState mouseState = base.GetMousePointerEventData(id);
        UpdateCursorState();
        return mouseState;
    }

    protected override void ProcessMove(PointerEventData pointerEvent)
    {
        UnlockCursor();
        base.ProcessMove(pointerEvent);
        UpdateCursorState();
    }

    protected override void ProcessDrag(PointerEventData pointerEvent)
    {
        UnlockCursor();
        base.ProcessDrag(pointerEvent);
        UpdateCursorState();
    }

    private void UnlockCursor() => Cursor.lockState = CursorLockMode.None;

    private void UpdateCursorState()
    {
        Cursor.lockState = GameController.viewMode == ViewMode.TOP ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = GameController.viewMode == ViewMode.TOP;
    }
}
