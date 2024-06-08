using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed;
    public Rigidbody2D rb;
    private Animator animator;
    public GameObject cameraHolder;
    private int state;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (EventSystem.current.currentSelectedGameObject != null) return;

        Vector2 movement = Vector2.zero;

        bool isPressingA = Input.GetKey(KeyCode.A);
        bool isPressingD = Input.GetKey(KeyCode.D);

        // Manejo de movimiento
        if (isPressingA)
        {
            movement += Vector2.left;
            state = 2;
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            // Si se suelta la tecla A, establece el estado en 0
            state = 0;
        }

        if (isPressingD)
        {
            movement += Vector2.right;
            state = 3;
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            // Si se suelta la tecla D, establece el estado en 1
            state = 1;
        }

        if (Input.GetKey(KeyCode.W))
        {
            movement += Vector2.up;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement += Vector2.down;
        }

        animator.SetInteger("State", state);
        movement = movement.normalized;
        rb.velocity = new Vector2(movement.x * speed, movement.y * speed);
    }

    // Confirmación de cámara Online
    public override void OnNetworkSpawn()
    {
        cameraHolder.SetActive(IsOwner);
        base.OnNetworkSpawn();
    }
}
