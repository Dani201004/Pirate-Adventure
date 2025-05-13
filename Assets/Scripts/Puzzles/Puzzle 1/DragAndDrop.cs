using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;

    private Key keyComponent;

    // Esto se ejecuta cuando el usuario toca o hace clic en el objeto
    private void OnMouseDown()
    {
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z));
        isDragging = true;

        keyComponent = GetComponent<Key>();
        if (keyComponent != null)
        {
            keyComponent.Select(); // Activar brillo al seleccionar
        }

        Collider keyCollider = GetComponent<Collider>();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Cofre"))
            {
                Physics.IgnoreCollision(hitCollider, keyCollider, true);
            }
        }
    }

    // Esto se ejecuta mientras el ratón o el toque está presionado
    private void Update()
    {
        if (isDragging)
        {
            // Mover el objeto a la nueva posición
            Vector3 currentMousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z));
            transform.position = new Vector3(currentMousePos.x + offset.x, currentMousePos.y + offset.y, transform.position.z);
        }

        // Verifica si el usuario ha soltado el ratón o el toque (terminar el arrastre)
        if (isDragging && Input.GetMouseButtonUp(0)) // WebGL (cuando se suelta el ratón)
        {
            OnDragEnd();
        }
        else if (isDragging && Application.isMobilePlatform && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended) // Android (cuando se suelta el toque)
            {
                OnDragEnd();
            }
        }
    }

    // Esto se ejecuta cuando se suelta el ratón o el toque
    private void OnDragEnd()
    {
        if (MenuController.IsPaused)
        {
            return;
        }

        isDragging = false;

        if (keyComponent != null)
        {
            keyComponent.Deselect(); // Desactivar brillo al soltar
        }

        Collider keyCollider = GetComponent<Collider>();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Cofre"))
            {
                Physics.IgnoreCollision(hitCollider, keyCollider, false);
            }
        }

        Key key = GetComponent<Key>();
        if (key != null)
        {
            key.CheckForMatchingChest();
        }
    }
}