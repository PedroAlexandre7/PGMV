using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float movementSpeed;
    [SerializeField] private GameObject tavern;
    private float currentVerticalAngle;
    private bool isMouseInitialized = false;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {

        if (GameController.viewMode != ViewMode.NORMAL)
            return;
        Move();
        Rotate();

        CheckCollisions();

        void Move()
        {
            Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            int speedMultiplier = (Input.GetKey(KeyCode.LeftShift) ? 2 : 1);
            transform.Translate(Time.deltaTime * movementSpeed * speedMultiplier * direction);
        }

        void Rotate()
        {
            float horizontalInput = Input.GetAxisRaw("Mouse X");
            float verticalInput = Input.GetAxisRaw("Mouse Y");
            if (!isMouseInitialized && (horizontalInput != 0 || verticalInput != 0))
            {
                isMouseInitialized = true;
                return;
            }
            float rotationDeltaTime = rotationSpeed * Time.deltaTime * 360;
            float yaw = horizontalInput * rotationDeltaTime;
            currentVerticalAngle -= verticalInput * rotationDeltaTime;
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, -90f, 90f);
            transform.localEulerAngles = new Vector3(currentVerticalAngle, transform.localEulerAngles.y + yaw, 0f);
        }
    }

    private void CheckCollisions()
    {
        Collider colider = GetComponent<Collider>();
        foreach (var item in tavern.GetComponents<Collider>())
        {
            if (item.bounds.Intersects(colider.bounds))
            {
                transform.position = startPosition;
                break;
            }
        }
    }
}