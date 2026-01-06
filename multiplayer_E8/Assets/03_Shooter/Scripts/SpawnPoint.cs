using UnityEngine;

namespace Starter.Shooter
{
	/// <summary>
	/// Represents a spawning point in the environment.
	/// </summary>
	public class SpawnPoint : MonoBehaviour
	{
		public float Radius = 1f;

        private void Awake()
        {
            // Ocultar cualquier "mesh" (Cubo, Esfera, etc.) que se haya usado para visualizar en el editor.
            // Esto cumple tu petición: "ponerle un cubo y que se desactive al iniciar".
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
        }

		private void OnDrawGizmos()
		{
            // Guardar matriz original
            Matrix4x4 originalMatrix = Gizmos.matrix;
            // Usar la rotación/posición del objeto
            Gizmos.matrix = transform.localToWorldMatrix;

            // 1. DIBUJAR CUBO DE 1x1x1 (Como pediste)
            // Color azul semitransparente para ver la orientación
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f); 
            // Cubo de 1m x 1m x 1m, levantado 0.5m para que apoye en el suelo
            Gizmos.DrawCube(Vector3.up * 0.5f, Vector3.one); 
            
            // Cubo 'alambre' para ver mejor los bordes
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(Vector3.up * 0.5f, Vector3.one);

            // 2. FLECHA DE DIRECCIÓN (FRENTE)
            // Línea amarilla gruesa indicando hacia dónde mirará el jugador
            Gizmos.color = Color.yellow;
            Vector3 start = Vector3.up * 0.5f; // Desde el centro del cubo
            Vector3 end = start + Vector3.forward * 2f; // 2 metros hacia adelante
            Gizmos.DrawLine(start, end);
            
            // Punta de la flecha
            Vector3 arrowTip = end;
            Vector3 right = arrowTip + (Vector3.back + Vector3.left) * 0.4f;
            Vector3 left = arrowTip + (Vector3.back + Vector3.right) * 0.4f;
            Gizmos.DrawLine(arrowTip, right);
            Gizmos.DrawLine(arrowTip, left);
            
            // Restaurar matriz
            Gizmos.matrix = originalMatrix;
            
            // Esfera de referencia para seleccionarlo fácil
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, Radius);
		}
	}
}
