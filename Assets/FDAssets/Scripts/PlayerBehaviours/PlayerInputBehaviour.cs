using UnityEngine;
using UnityEngine.InputSystem;

[Tooltip("Handles Player Input.  This is a singleton because there is only one local player.")]
public class PlayerInputBehaviour : MonoBehaviour
{
    /// <summary>
    /// The singleton reference to the player behaviour
    /// </summary>
    public static PlayerInputBehaviour Instance { get; private set; }

    /// <summary>
    /// We allow jump value to have a slight buffer in case it was pressed early
    /// </summary>
    public static float jumpValue;

    /// <summary>
    /// Affects the player movement
    /// </summary>
    public static Vector2 moveValue;

    /// <summary>
    /// Affects camera rotation
    /// </summary>
    public static Vector2 lookValue;

    /// <summary>
    /// A slight buffer to allow the player to still jump if they press jump a little early.
    /// </summary>
    const float JUMP_BUFFER = 8f / 60f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        Instance = this;
    }
    private void OnDestroy()
    {
        Instance = null;
    }

    #region UNITY_INPUT_MESSAGES
    public void OnMove(InputValue value)
    {
        moveValue = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookValue = value.Get<Vector2>();
    }
    public void OnJump(InputValue value)
    {
        if (jumpValue <= 0 && value.isPressed)
        {
            jumpValue = JUMP_BUFFER;
        }
    }
    #endregion

    /// <summary>
    /// Reduces the jump value over time.
    /// </summary>
    private void Update()
    {
        if (jumpValue > 0)
            jumpValue -= Time.deltaTime;
    }
}