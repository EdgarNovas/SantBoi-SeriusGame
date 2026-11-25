using UnityEngine;

public class RotationChecker : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float anguloArriba = 90f;
    [SerializeField] private float tolerancia = 20f;

    public string state { get; private set; }
    private bool finished;

    void Update()
    {
        float angle = transform.eulerAngles.z;
        float diferencia = Mathf.Abs(Mathf.DeltaAngle(anguloArriba, angle));

        if (diferencia <= tolerancia)
        {
            finished = true;
            state = "FINISHED";
        }
        else
        {
            finished = false;
            state = "NOT FINISHED";
        }

        Debug.Log(state + " | finished = " + finished);
    }

    public bool GetIsFinished()
    {
        return finished;
    }
}