using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Move;
    public Vector2 Look;
    public NetworkButtons Buttons;

    public const int BUTTON_JUMP = 0;
    public const int BUTTON_PUSH = 1; // на будущее (ЛКМ)
}
