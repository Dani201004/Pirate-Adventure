using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;

    // Esto se ejecuta cuando el usuario toca o hace clic en el objeto
    private void OnMouseDown()
    {
        // Calculamos el offset para que el objeto se mantenga en la misma posici�n relativa cuando sea arrastrado
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z));

        isDragging = true;

        // Desactivar la colisi�n entre la llave y los cofres durante el drag
        Collider keyCollider = GetComponent<Collider>();  // Asumimos que usas un Collider 3D
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f); // Colisiones cercanas

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Cofre"))
            {
                // Desactivar la colisi�n entre la llave y el cofre mientras se arrastra
                Physics.IgnoreCollision(hitCollider, keyCollider, true);
            }
        }
    }

    // Esto se ejecuta mientras el rat�n o el toque est� presionado
    private void Update()
    {
        if (isDragging)
        {
            // Mover el objeto a la nueva posici�n
            Vector3 currentMousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z));
            transform.position = new Vector3(currentMousePos.x + offset.x, currentMousePos.y + offset.y, transform.position.z);
        }

        // Verifica si el usuario ha soltado el rat�n o el toque (terminar el arrastre)
        if (isDragging && Input.GetMouseButtonUp(0)) // WebGL (cuando se suelta el rat�n)
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

    // Esto se ejecuta cuando se suelta el rat�n o el toque
    private void OnDragEnd()
    {
        isDragging = false;

        // Restaurar la colisi�n entre la llave y los cofres cuando se suelta la llave
        Collider keyCollider = GetComponent<Collider>();  // Asumimos que usas un Collider 3D
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f); // Colisiones cercanas

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Cofre"))
            {
                // Restaurar la colisi�n entre la llave y el cofre despu�s de soltarla
                Physics.IgnoreCollision(hitCollider, keyCollider, false);
            }
        }

        // Al soltar el objeto, verificamos que tiene el componente Key
        Key key = GetComponent<Key>();
        if (key != null)
        {
            // Llamamos a la funci�n de verificaci�n de Key
            key.CheckForMatchingChest();
        }
    }
}